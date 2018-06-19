using Alba.CsConsoleFormat;
using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Factories;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using CryptonightProfitSwitcher.Miners;
using CryptonightProfitSwitcher.ProfitPoviders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptonightProfitSwitcher.ProfitSwitchingStrategies;

namespace CryptonightProfitSwitcher
{
    class Program
    {
        const int VERSION = 6;

        static IMiner _currentMiner = null;
        static Mineable _currentMineable = null;
        static int _watchdogOvershots = 0;
        static bool _requestQuit = false;
        static bool _newVersionAvailable;
        static CancellationTokenSource _mainResetCts = null;
        static CancellationTokenSource _watchdogCts = null;
        static CancellationTokenSource _keyPressesCts = null;
        static CancellationTokenSource _profitSwitcherTaskCts = null;
        static Task _profitSwitcherTask;
        static DateTimeOffset _lastProfitSwitch = DateTimeOffset.MinValue;

        static void Main(string[] args)
        {
            while (!_requestQuit)
            {
                _mainResetCts = new CancellationTokenSource();
                //Welcome
                ResetConsole();

                //Get app root
                var appFolderPath = Helpers.GetApplicationRoot();
                var appFolder = new DirectoryInfo(appFolderPath);

                //Initalize coins
                var coinsFolder = appFolder.GetDirectories("Coins").First();
                var coins = new List<Coin>();
                foreach (var coinFile in coinsFolder.GetFiles().Where(f => f.Extension == ".json"))
                {
                    var coinJson = File.ReadAllText(coinFile.FullName);
                    var coin = JsonConvert.DeserializeObject<Coin>(coinJson);
                    coins.Add(coin);
                    Console.WriteLine("Initalized coin: " + coin.DisplayName);
                }

                //Initalize Nicehash algorithms
                var nicehashAlgorithmsFolder = appFolder.GetDirectories("NicehashAlgorithms").First();
                var nicehashAlgorithms = new List<NicehashAlgorithm>();
                foreach (var nicehashAlgorithmFile in nicehashAlgorithmsFolder.GetFiles().Where(f => f.Extension == ".json"))
                {
                    var nicehashAlgorithmJson = File.ReadAllText(nicehashAlgorithmFile.FullName);
                    var nicehashAlgorithm = JsonConvert.DeserializeObject<NicehashAlgorithm>(nicehashAlgorithmJson);
                    nicehashAlgorithms.Add(nicehashAlgorithm);
                    Console.WriteLine("Initalized Nicehash-Algorithm: " + nicehashAlgorithm.DisplayName);
                }

                //Initalize settings
                var settingsFile = appFolder.GetFiles("Settings.json").First();
                var settingsJson = File.ReadAllText(settingsFile.FullName);
                var settings = JsonConvert.DeserializeObject<Settings>(settingsJson);
                Console.WriteLine("Initalized settings.");

                //Start profit switching algorithm
                _profitSwitcherTaskCts?.Cancel();
                _profitSwitcherTaskCts = new CancellationTokenSource();
                _profitSwitcherTask = ProfitSwitcherTask(appFolderPath, appFolder, settings, coins, nicehashAlgorithms, _profitSwitcherTaskCts.Token);

                //Start key presses task
                _keyPressesCts?.Cancel();
                _keyPressesCts = new CancellationTokenSource();
                var keyPressesTask = KeypressesTask(appFolderPath, appFolder, settings, coins, nicehashAlgorithms, _keyPressesCts.Token);

                //Check for updates
                try
                {
                    var versionText = Helpers.GetJsonFromUrl("https://raw.githubusercontent.com/cryptoprofitswitcher/CryptonightProfitSwitcher/master/version.txt", settings, appFolder, CancellationToken.None);
                    int remoteVersion = Int32.Parse(versionText);
                    if (remoteVersion > VERSION)
                    {
                        _newVersionAvailable = true;
                        Console.WriteLine("New update available!");
                    }
                    else
                    {
                        Console.WriteLine("Your version is up to date.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't check for updates: " + ex.Message);
                }
                //Wait until reset
                _mainResetCts.Token.WaitHandle.WaitOne();
                Console.WriteLine("Reset app");
            }
        }

        static Task ProfitSwitcherTask(string appFolderPath, DirectoryInfo appRootFolder, Settings settings, List<Coin> coins, List<NicehashAlgorithm> nicehashAlgorithms, CancellationToken token)
        {
            return Task.Run(() =>
            {
                //Get pool profit data
                while (!token.IsCancellationRequested)
                {
                    var statusCts = new CancellationTokenSource();
                    var watchdogCts = new CancellationTokenSource();

                    try
                    {
                        ResetConsole();

                        // Get pool mined coins profitability
                        var profitProviders = Helpers.GetPoolProfitProviders(settings, null);
                        foreach (var coin in coins.Where(c => !String.IsNullOrEmpty(c.OverridePoolProfitProviders)))
                        {
                            var overrrideProfitProvidersSplitted = coin.OverridePoolProfitProviders.Split(",");
                            foreach (var profitProviderString in overrrideProfitProvidersSplitted)
                            {
                                ProfitProvider profitProvider;
                                if (Enum.TryParse(profitProviderString, out profitProvider))
                                {
                                    if (!profitProviders.Contains(profitProvider))
                                    {
                                        profitProviders.Add(profitProvider);
                                    }
                                }
                            }
                        }
                        var poolProfitsDictionaryUnordered = new Dictionary<ProfitProvider, Dictionary<string, Profit>>();
                        var profitProviderTasks = new List<Task>();
                        foreach (var profitProvider in profitProviders)
                        {
                            if (!poolProfitsDictionaryUnordered.ContainsKey(profitProvider))
                            {
                                profitProviderTasks.Add(Task.Run(()=>
                                {
                                    IPoolProfitProvider profitProviderClass = PoolProfitProviderFactory.GetPoolProfitProvider(profitProvider);
                                    var poolProfits = profitProviderClass.GetProfits(appRootFolder, settings, coins, token);
                                    poolProfitsDictionaryUnordered[profitProvider] = poolProfits;
                                }, token));
                            }
                        }
#if DEBUG
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
#endif
                        Task.WhenAll(profitProviderTasks).Wait(token);
#if DEBUG
                        sw.Stop();
                        Console.WriteLine($"Fetching profit data took {(int)sw.Elapsed.TotalSeconds} seconds.");
#endif
                        
                        //Reorder because of async
                        var poolProfitsDictionary = new Dictionary<ProfitProvider, Dictionary<string, Profit>>();
                        foreach (var profitProvider in profitProviders)
                        {
                            if (poolProfitsDictionaryUnordered.ContainsKey(profitProvider))
                            {
                                poolProfitsDictionary[profitProvider] = poolProfitsDictionaryUnordered[profitProvider];
                            }
                        }

                        IProfitSwitchingStrategy profitSwitchingStrategy = ProfitSwitchingStrategyFactory.GetProfitSwitchingStrategy(settings.ProfitSwitchingStrategy);

                        // Get best pool mined coin
                        MineableReward bestPoolminedCoin = profitSwitchingStrategy.GetBestPoolminedCoin(coins, poolProfitsDictionary, settings);
                        if (bestPoolminedCoin?.Mineable != null)
                        {
                            Console.WriteLine("Got best pool mined coin: " + bestPoolminedCoin.Mineable.DisplayName);
                        }
                        else
                        {
                            Console.WriteLine("Couldn't determine best pool mined coin.");
                        }

                        //Get Nicehash Profit
                        var nicehashProfitsDictionary = NicehashApi.GetProfits(appRootFolder, settings, nicehashAlgorithms, token);

                        //Get best nicehash algorithm
                        MineableReward bestNicehashAlgorithm = profitSwitchingStrategy.GetBestNicehashAlgorithm(nicehashAlgorithms, nicehashProfitsDictionary, settings);
                        if (bestNicehashAlgorithm?.Mineable != null)
                        {
                            Console.WriteLine("Got best nicehash algorithm: " + bestNicehashAlgorithm.Mineable.DisplayName);
                        }
                        else
                        {
                            Console.WriteLine("Couldn't determine best nicehash algorithm.");
                        }

                        //Print table
                        PrintProfitTable(coins, poolProfitsDictionary, nicehashAlgorithms, nicehashProfitsDictionary, settings);

                        //Mine using best algorithm/coin
                        Console.WriteLine();
                        MineableReward bestOverallMineable = profitSwitchingStrategy.GetBestMineable(bestPoolminedCoin, bestNicehashAlgorithm);
                        if (bestOverallMineable?.Mineable != null)
                        {
                            Console.WriteLine($"Determined best mining method: {bestOverallMineable.Mineable.DisplayName}");
                            if (_currentMineable == null || _currentMineable.Id != bestOverallMineable.Mineable.Id)
                            {
                                StartMiner(bestOverallMineable.Mineable, settings, appFolderPath, appRootFolder);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Couldn't determine best mining method.");
                        }

                        var statusUpdaterTask = StatusUpdaterTask(DateTimeOffset.Now.AddSeconds(settings.ProfitCheckInterval), settings, appRootFolder, coins, poolProfitsDictionary, nicehashAlgorithms, nicehashProfitsDictionary, statusCts.Token);

                        if (settings.ProfitCheckInterval > 0)
                        {
                            Task.Delay(TimeSpan.FromSeconds(settings.ProfitCheckInterval), token).Wait();
                        }
                        else
                        {
                            token.WaitHandle.WaitOne();
                        }

                        statusCts.Cancel();
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine("Cancelled profit task.");
                        statusCts.Cancel();
                        return;
                    }
                    catch (AggregateException)
                    {
                        Console.WriteLine("Cancelled profit task.");
                        statusCts.Cancel();
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Profit switcher task failed: " + ex.Message);
                        statusCts.Cancel();
                    }
                }
            }, token);
        }

        static Task StatusUpdaterTask(DateTimeOffset estReset, Settings settings, DirectoryInfo appRootFolder, List<Coin> coins, Dictionary<ProfitProvider, Dictionary<string, Profit>> poolProfitsDictionary, List<NicehashAlgorithm> nicehashAlgorithms, Dictionary<int, Profit> nicehashProfitsDictionary, CancellationToken token)
        {
            return Task.Run(() =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        ResetConsole();

                        Console.Write(" Press '");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("p");
                        Console.ResetColor();
                        Console.WriteLine("' to refresh profits immediately.");

                        Console.Write(" Press '");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("r");
                        Console.ResetColor();
                        Console.WriteLine("' to reset the app and run the reset script if it is set.");

                        Console.Write(" Press '");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("s");
                        Console.ResetColor();
                        Console.WriteLine("' to reload the app without reset.");

                        Console.Write(" Press '");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("q");
                        Console.ResetColor();
                        Console.WriteLine("' to quit.");

                        PrintProfitTable(coins, poolProfitsDictionary, nicehashAlgorithms, nicehashProfitsDictionary, settings);

                        WriteInCyan(" CURRENT STATUS: ");

                        if (_currentMineable != null && _currentMiner != null)
                        {
                            var currentHashrate = _currentMiner.GetCurrentHashrate(settings, appRootFolder);
                            var roundedHashRate = Math.Round(currentHashrate, 2, MidpointRounding.AwayFromZero);
                            var estimatedProfit = _currentMineable is Coin ? Helpers.GetPoolProfitForCoin((Coin)_currentMineable, poolProfitsDictionary, settings) : nicehashProfitsDictionary[((NicehashAlgorithm)_currentMineable).ApiId];
                            double currentUsdReward = 0;
                            double currentCoinReward = 0;

                            if (estimatedProfit.UsdRewardLive > 0)
                            {
                                currentUsdReward = (estimatedProfit.UsdRewardLive / _currentMineable.GetExpectedHashrate(settings)) * currentHashrate;
                            }
                            else if (estimatedProfit.UsdRewardDay > 0)
                            {
                                currentUsdReward = (estimatedProfit.UsdRewardDay / _currentMineable.GetExpectedHashrate(settings)) * currentHashrate;
                            }

                            if (estimatedProfit.CoinRewardLive > 0)
                            {
                                currentCoinReward = (estimatedProfit.CoinRewardLive / _currentMineable.GetExpectedHashrate(settings)) * currentHashrate;
                            }
                            else if (estimatedProfit.CoinRewardDay > 0)
                            {
                                currentCoinReward = (estimatedProfit.CoinRewardDay / _currentMineable.GetExpectedHashrate(settings)) * currentHashrate;
                            }

                            if (_currentMineable is Coin currentCoin)
                            {
                                Console.Write(" Mining: '");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(_currentMineable.DisplayName);
                                Console.ResetColor();
                                Console.Write("' at ");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(roundedHashRate + "H/s");
                                Console.ResetColor();
                                if (currentUsdReward > 0 || currentCoinReward > 0)
                                {
                                    Console.Write(" (");
                                    if (currentUsdReward > 0)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.Write(Helpers.ToCurrency(currentUsdReward, "$"));
                                        Console.ResetColor();
                                    }
                                    if (currentCoinReward > 0)
                                    {
                                        if (currentUsdReward > 0)
                                        {
                                            Console.Write(" / ");
                                        }
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.Write($"{Math.Round(currentCoinReward, 4)} {currentCoin.TickerSymbol}");
                                        Console.ResetColor();
                                    }

                                    Console.WriteLine(" per day)");
                                }
                            }
                            else
                            {
                                Console.Write(" Hashing: '");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(_currentMineable.DisplayName);
                                Console.ResetColor();
                                Console.Write("' on NiceHash at ");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(roundedHashRate + "H/s");
                                Console.ResetColor();
                                if (currentUsdReward > 0)
                                {
                                    Console.Write(" (");
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.Write(Helpers.ToCurrency(currentUsdReward, "$"));
                                    Console.ResetColor();
                                    Console.WriteLine(" per day)");
                                }
                            }

                            var differenceToLastProfitSwitch = DateTimeOffset.Now.Subtract(_lastProfitSwitch).TotalSeconds;
                            if (differenceToLastProfitSwitch < settings.ProfitSwitchCooldown)
                            {
                                Console.Write(" Cooldown: Must wait ");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(Math.Round(settings.ProfitSwitchCooldown - differenceToLastProfitSwitch, 0, MidpointRounding.AwayFromZero));
                                Console.ResetColor();
                                Console.WriteLine(" seconds before switching.");
                            }
                            else
                            {
                                Console.WriteLine(" Cooldown: Ready to switch.");
                            }

                            if (settings.EnableWatchdog)
                            {
                                Console.Write(" Watchdog: ");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(_watchdogOvershots);
                                Console.ResetColor();
                                Console.Write(" consecutive overshot(s) out of ");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(settings.WatchdogAllowedOversteps);
                                Console.ResetColor();
                                Console.WriteLine(" allowed.");
                            }
                            else
                            {
                                Console.WriteLine(" Watchdog: Not activated.");
                            }

                        }
                        else
                        {
                            Console.WriteLine(" Not mining.");
                        }

                        Console.WriteLine();
                        if (settings.ProfitCheckInterval > 0)
                        {
                            var remainingSeconds = estReset.Subtract(DateTimeOffset.Now).TotalSeconds;
                            Console.Write(" Will refresh automatically in ");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write(Math.Round(remainingSeconds, 0, MidpointRounding.AwayFromZero));
                            Console.ResetColor();
                            Console.WriteLine(" seconds.");
                        }
                        else
                        {
                            Console.WriteLine(" Won't refresh automatically.");
                        }
                        Task.Delay(TimeSpan.FromSeconds(settings.DisplayUpdateInterval), token).Wait();
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine(" Cancelled status task.");
                    return;
                }
                catch (AggregateException)
                {
                    Console.WriteLine(" Cancelled status task.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" Status task failed: " + ex.Message);
                }
            }, token);
        }

        static Task WatchdogTask(Settings settings, string appFolderPath, DirectoryInfo appRootFolder, CancellationToken token)
        {
            return Task.Run(() =>
            {
                try
                {
                    Task.Delay(TimeSpan.FromSeconds(settings.WatchdogDelay), token);
                    while (!token.IsCancellationRequested)
                    {
                        if (_currentMineable != null && _currentMiner != null)
                        {
                            double estimatedHashrate = _currentMineable.GetExpectedHashrate(settings);
                            double actualHashrate = _currentMiner.GetCurrentHashrate(settings, appRootFolder);
                            double differenceRatio = actualHashrate / estimatedHashrate;

                            if (differenceRatio < settings.WatchdogCriticalThreshold)
                            {
                                _watchdogOvershots++;
                            }
                            else
                            {
                                _watchdogOvershots = 0;
                            }

                            if (_watchdogOvershots > settings.WatchdogAllowedOversteps)
                            {
                                Console.WriteLine("Watchdog: Too many overshots -> Requesting reset");
                                ResetApp(settings, appFolderPath, true);
                            }
                        }
                        Task.Delay(TimeSpan.FromSeconds(settings.WatchdogInterval), token).Wait();
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Cancelled watchdog task.");
                    return;
                }
                catch (AggregateException)
                {
                    Console.WriteLine("Cancelled watchdog task.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Watchdog task failed: " + ex.Message);
                }
            }, token);
        }

        static Task KeypressesTask(string appFolderPath, DirectoryInfo appFolder, Settings settings, List<Coin> coins, List<NicehashAlgorithm> nicehashAlgorithms, CancellationToken token)
        {
            return Task.Run(() =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var readKey = Console.ReadKey(true);

                        if (!token.IsCancellationRequested)
                        {
                            if (readKey.Key == ConsoleKey.Q)
                            {
                                //Quit app
                                StopMiner();
                                Environment.Exit(0);
                                _requestQuit = true;
                                return;
                            }
                            else if (readKey.Key == ConsoleKey.P)
                            {
                                //Restart profit task
                                _profitSwitcherTaskCts.Cancel();
                                _profitSwitcherTask.Wait();
                                _profitSwitcherTaskCts = new CancellationTokenSource();
                                _profitSwitcherTask = ProfitSwitcherTask(appFolderPath, appFolder, settings, coins, nicehashAlgorithms, _profitSwitcherTaskCts.Token);
                            }
                            else if (readKey.Key == ConsoleKey.S)
                            {
                                //Relaunch app without reset script
                                ResetApp(settings, appFolderPath, false);
                            }
                            else if (readKey.Key == ConsoleKey.R)
                            {
                                //Reset app
                                ResetApp(settings, appFolderPath, true);
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Cancelled key presses task.");
                    //ResetApp(settings,appFolderPath);
                    return;
                }
                catch (AggregateException)
                {
                    Console.WriteLine("Cancelled key presses task.");
                    //ResetApp(settings, appFolderPath);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Key presses task failed: " + ex.Message);
                }
            }, token);
        }

        static void ResetConsole()
        {
            Console.CursorVisible = false;
            Console.Clear();
            string updateText = _newVersionAvailable ? " (NEW UPDATE AVAILABLE!)" : String.Empty;
            WriteInCyan($" CRYPTONIGHT PROFIT SWITCHER | VERSION: {Helpers.GetApplicationVersion()}{updateText}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" https://github.com/cryptoprofitswitcher/CryptonightProfitSwitcher");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void WriteInCyan(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        static void PrintProfitTable(List<Coin> coins, Dictionary<ProfitProvider, Dictionary<string, Profit>> poolProfitsDictionary, List<NicehashAlgorithm> nicehashAlgorithms, Dictionary<int, Profit> nicehashProfitsDictionary, Settings settings)
        {
            Console.WriteLine();

            WriteInCyan(" POOL MINED COINS SUMMARY: ");

            var headerThickness = new LineThickness(LineWidth.Double, LineWidth.Single);
            var poolGrid = new Grid
            {
                Color = ConsoleColor.White,
                Margin = new Thickness(1, 0, 0, 0),
                Columns = { GridLength.Auto },
                Children =
                {
                        new Cell("Name") { Stroke = headerThickness, Color = ConsoleColor.Magenta, Padding = new Thickness(2,0,10,0) },
                },
            };

            foreach (var profits in poolProfitsDictionary)
            {
                poolGrid.Columns.Add(GridLength.Auto);
                poolGrid.Children.Add(new Cell(profits.Key.ToString()) { Stroke = headerThickness, Color = ConsoleColor.Magenta, Padding = new Thickness(2, 0, 2, 0) });
            }

            foreach (var coin in coins)
            {
                poolGrid.Children.Add(new Cell(coin.DisplayName) { Color = coin.IsEnabled() ? ConsoleColor.Yellow : ConsoleColor.DarkGray, Padding = new Thickness(2, 0, 10, 0) });
                foreach (var profits in poolProfitsDictionary)
                {
                    var profit = profits.Value.GetValueOrDefault(coin.TickerSymbol, new Profit());
                    poolGrid.Children.Add(profit.UsdRewardLive <= 0
                        ? new Cell("No data") { Color = ConsoleColor.DarkGray, Align = Align.Center, Padding = new Thickness(2, 0, 2, 0) }
                        : new Cell(profit.ToString()) { Color = coin.IsEnabled() ? ConsoleColor.Gray : ConsoleColor.DarkGray, Align = Align.Center, Padding = new Thickness(2, 0, 2, 0) });
                }
            }
            var poolDoc = new Document(poolGrid);
            ConsoleRenderer.RenderDocument(poolDoc);
            Console.WriteLine();
            WriteInCyan(" NICEHASH ALGORITHMS SUMMARY: ");

            int firstColumnWidth = 0;
            int otherColumnWidth = 0;
            for (int i = 0; i < poolGrid.Columns.Count; i++)
            {
                if (i == 0)
                {
                    firstColumnWidth = poolGrid.Columns[i].ActualWidth;
                }
                else
                {
                    otherColumnWidth += poolGrid.Columns[i].ActualWidth;
                }
            }
            var nicehashGrid = new Grid
            {
                Color = ConsoleColor.White,
                MinWidth = poolGrid.ActualWidth,
                Margin = new Thickness(1, 0, 0, 0),
                Columns = { new GridLength(firstColumnWidth, GridUnit.Char), new GridLength(otherColumnWidth, GridUnit.Char) },
                Children =
                {
                        new Cell("Name") { Stroke = headerThickness, Color = ConsoleColor.Magenta , Padding = new Thickness(2, 0, 2, 0)},
                        new Cell("NiceHash API") { Stroke = headerThickness, Color = ConsoleColor.Magenta, Padding = new Thickness(2, 0, 2, 0) }
                }
            };

            foreach (var nicehashAlgorithm in nicehashAlgorithms)
            {
                nicehashGrid.Children.Add(new Cell(nicehashAlgorithm.DisplayName) { Color = nicehashAlgorithm.IsEnabled() ? ConsoleColor.Yellow : ConsoleColor.DarkGray, Padding = new Thickness(2, 0, 2, 0) });
                var profit = nicehashProfitsDictionary.GetValueOrDefault(nicehashAlgorithm.ApiId, new Profit());
                nicehashGrid.Children.Add(profit.UsdRewardLive <= 0
                    ? new Cell("No data") { Color = ConsoleColor.DarkGray, Align = Align.Center, Padding = new Thickness(2, 0, 2, 0) }
                    : new Cell(profit.ToString()) { Color = nicehashAlgorithm.IsEnabled() ? ConsoleColor.Gray : ConsoleColor.DarkGray, Align = Align.Center, Padding = new Thickness(2, 0, 2, 0) });
            }
            var nicehashDoc = new Document(nicehashGrid);
            ConsoleRenderer.RenderDocument(nicehashDoc);

            Console.WriteLine();
        }

        static void ResetApp(Settings settings, string appFolderPath, bool runResetScript)
        {
            _profitSwitcherTaskCts.Cancel();
            _profitSwitcherTask.Wait();
            StopMiner();
            Thread.Sleep(settings.MinerStartDelay);
            if (runResetScript)
            {
                ExecuteScript(settings.ResetScript, appFolderPath);
            }
            _keyPressesCts?.Cancel();
            _mainResetCts?.Cancel();
            _lastProfitSwitch = DateTimeOffset.MinValue;
        }

        static void StartMiner(Mineable mineable, Settings settings, string appRoot, DirectoryInfo appRootFolder)
        {
            var differenceToLastProfitSwitch = DateTimeOffset.Now.Subtract(_lastProfitSwitch).TotalSeconds;
            if (differenceToLastProfitSwitch > settings.ProfitSwitchCooldown)
            {
                StopMiner();

                _currentMineable = mineable;

                ExecuteScript(mineable.PrepareScript, appRoot);

                _currentMiner = MinerFactory.GetMiner(mineable.Miner);
                _lastProfitSwitch = DateTimeOffset.Now;
                _currentMiner.StartMiner(mineable, settings, appRoot, appRootFolder);

                if (settings.EnableWatchdog)
                {
                    _watchdogCts = new CancellationTokenSource();
                    var watchdogTask = WatchdogTask(settings, appRoot, appRootFolder, _watchdogCts.Token);
                }
            }
            else
            {
                Console.WriteLine($"Didn't switched to {mineable.DisplayName}! Waiting {settings.ProfitSwitchCooldown} seconds to cooldown.");
            }
        }

        static void StopMiner()
        {
            _watchdogCts?.Cancel();
            _currentMiner?.StopMiner();
            _currentMiner = null;
            _currentMineable = null;
            _watchdogOvershots = 0;
        }

        static void ExecuteScript(string scriptPath, string appFolderPath)
        {
            try
            {
                if (!String.IsNullOrEmpty(scriptPath))
                {
                    //Execute reset script
                    var fileInfo = new FileInfo(Helpers.ResolveToFullPath(scriptPath, appFolderPath));
                    switch (fileInfo.Extension)
                    {
                        case ".bat":
                        case ".BAT":
                        case ".cmd":
                        case ".CMD":
                            {
                                // Run batch in Windows
                                var resetProcess = new Process();
                                resetProcess.StartInfo.FileName = "cmd.exe";
                                resetProcess.StartInfo.Arguments = $"/c {Helpers.ResolveToArgumentPath(scriptPath, appFolderPath)}";
                                resetProcess.StartInfo.UseShellExecute = true;
                                resetProcess.StartInfo.CreateNoWindow = false;
                                resetProcess.StartInfo.RedirectStandardOutput = false;
                                resetProcess.Start();
                                resetProcess.WaitForExit();
                                break;
                            }
                        case ".sh":
                            {
                                // Run sh script in Linux
                                var resetProcess = new Process();
                                resetProcess.StartInfo.FileName = "sh";
                                resetProcess.StartInfo.Arguments = Helpers.ResolveToArgumentPath(scriptPath, appFolderPath);
                                resetProcess.StartInfo.UseShellExecute = true;
                                resetProcess.StartInfo.CreateNoWindow = false;
                                resetProcess.StartInfo.RedirectStandardOutput = false;
                                resetProcess.Start();
                                resetProcess.WaitForExit();
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't execute script: " + scriptPath);
                Console.WriteLine(ex.Message);
            }
        }
    }
}
