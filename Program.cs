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
using Serilog;

namespace CryptonightProfitSwitcher
{
    internal static class Program
    {
        private const int VERSION = 11;

        private static IMiner _currentMiner;
        private static Mineable _currentMineable;
        private static int _watchdogOvershots;
        private static bool _requestQuit;
        private static bool _manualSelection;
        private static bool? _manualWatchdogEnabled;
        private static Mineable _manualSelectedMineable;
        private static bool _newVersionAvailable;
        private static CancellationTokenSource _mainResetCts;
        private static CancellationTokenSource _watchdogCts;
        private static CancellationTokenSource _keyPressesCts;
        private static CancellationTokenSource _profitSwitcherTaskCts;
        private static Task _profitSwitcherTask;
        private static DateTimeOffset _lastProfitSwitch = DateTimeOffset.MinValue;

        private static void Main(string[] args)
        {
            //Get app root
            var appFolderPath = Helpers.GetApplicationRoot();
            var appFolder = new DirectoryInfo(appFolderPath);

            //Initalize logging file
            string logPath = Path.Combine(appFolderPath, "current_log.txt");
            try
            {
                var logFile = new FileInfo(logPath);
                if (logFile.Exists)
                {
                    string oldlogPath = Path.Combine(appFolderPath, "previous_log.txt");
                    if (File.Exists(oldlogPath))
                    {
                        File.Delete(oldlogPath);
                    }
                    logFile.MoveTo(oldlogPath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't initalize log file: " + e.Message);
            }

            while (!_requestQuit)
            {
                _mainResetCts = new CancellationTokenSource();
                //Welcome
                ResetConsole();

                //Initalize coins
                Console.WriteLine("Initalize coins");

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
                Console.WriteLine("Initalize Nicehash algorithms");

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
                Console.WriteLine("Initalize settings");

                var settingsFile = appFolder.GetFiles("Settings.json").First();
                var settingsJson = File.ReadAllText(settingsFile.FullName);
                var settings = JsonConvert.DeserializeObject<Settings>(settingsJson);
                Console.WriteLine("Initalized settings.");

                //Initalize logging
                if (settings.EnableLogging)
                {
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .WriteTo.File(logPath)
                        .CreateLogger();
                }
                else
                {
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();
                }

                //Start profit switching algorithm
                Log.Information("Start profit switching algorithm");

                _profitSwitcherTaskCts?.Cancel();
                _profitSwitcherTaskCts = new CancellationTokenSource();
                _profitSwitcherTask = ProfitSwitcherTaskAsync(appFolderPath, appFolder, settings, coins, nicehashAlgorithms, _profitSwitcherTaskCts.Token);

                //Start key presses task
                Log.Information("Start key presses task");

                _keyPressesCts?.Cancel();
                _keyPressesCts = new CancellationTokenSource();
                var keyPressesTask = KeypressesTaskAsync(appFolderPath, appFolder, settings, coins, nicehashAlgorithms, _keyPressesCts.Token);

                //Check for updates
                try
                {
                    Log.Information("Check for updates");

                    var versionText = Helpers.GetJsonFromUrl("https://raw.githubusercontent.com/cryptoprofitswitcher/CryptonightProfitSwitcher/master/version.txt", settings, appFolder, CancellationToken.None);
                    int remoteVersion = Int32.Parse(versionText);
                    if (remoteVersion > VERSION)
                    {
                        _newVersionAvailable = true;
                        Log.Information("New update available!");
                    }
                    else
                    {
                        Log.Information("Your version is up to date.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Information("Couldn't check for updates: " + ex.Message);
                }
                //Wait until reset
                _mainResetCts.Token.WaitHandle.WaitOne();
                Log.Information("Reset app");
            }
        }

        private static Task ProfitSwitcherTaskAsync(string appFolderPath, DirectoryInfo appRootFolder, Settings settings, List<Coin> coins, List<NicehashAlgorithm> nicehashAlgorithms, CancellationToken token)
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

                        var timeoutCts = new CancellationTokenSource(60000);
                        var childCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);

                        var profitProviders = Helpers.GetPoolProfitProviders(settings, null);
                        foreach (var coin in coins.Where(c => !String.IsNullOrEmpty(c.OverridePoolProfitProviders)))
                        {
                            foreach (var profitProviderString in coin.OverridePoolProfitProviders.Split(","))
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
                                    poolProfitsDictionaryUnordered[profitProvider] = profitProviderClass.GetProfits(appRootFolder, settings, coins, childCts.Token);
                                }, childCts.Token));
                            }
                        }
#if DEBUG
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
#endif
                        Task.WhenAll(profitProviderTasks).Wait(childCts.Token);
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

                        MineableReward currentReward = null;

                        // Get best pool mined coin
                        MineableReward bestPoolminedCoin = null;
                        if (!token.IsCancellationRequested)
                        {
                            var result = profitSwitchingStrategy.GetBestPoolminedCoin(coins,_currentMineable, poolProfitsDictionary, settings);
                            bestPoolminedCoin = result?.Result;
                            if (currentReward == null) currentReward = result?.Current;
                            if (bestPoolminedCoin?.Mineable != null)
                            {
                                Console.WriteLine("Got best pool mined coin: " + bestPoolminedCoin.Mineable.DisplayName);
                            }
                            else
                            {
                                Console.WriteLine("Couldn't determine best pool mined coin.");
                            }
                        }

                        //Get Nicehash Profit
                        Dictionary<int, Profit> nicehashProfitsDictionary = null;
                        if (!token.IsCancellationRequested)
                        {
                            nicehashProfitsDictionary = NicehashApi.GetProfits(appRootFolder, settings, nicehashAlgorithms, childCts.Token);
                        }

                        //Get best nicehash algorithm
                        MineableReward bestNicehashAlgorithm = null;
                        if (!token.IsCancellationRequested && nicehashProfitsDictionary != null)
                        {
                            var result = profitSwitchingStrategy.GetBestNicehashAlgorithm(nicehashAlgorithms,_currentMineable, nicehashProfitsDictionary, settings);
                            bestNicehashAlgorithm = result?.Result;
                            if (currentReward == null) currentReward = result?.Current;
                            if (bestNicehashAlgorithm?.Mineable != null)
                            {
                                Console.WriteLine("Got best nicehash algorithm: " + bestNicehashAlgorithm.Mineable.DisplayName);
                            }
                            else
                            {
                                Console.WriteLine("Couldn't determine best nicehash algorithm.");
                            }
                        }

                        //Sort profit table
                        if (settings.ProfitSorting != SortingMode.None)
                        {
                            var coinProfitComparer = new CoinProfitComparer(settings.ProfitSorting, poolProfitsDictionary);
                            coins.Sort(coinProfitComparer);
                            var nicehashProfitComparer = new NicehashProfitComparer(settings.ProfitSorting, nicehashProfitsDictionary);
                            nicehashAlgorithms.Sort(nicehashProfitComparer);
                        }

                        //Print table
                        if (!token.IsCancellationRequested && nicehashProfitsDictionary != null)
                        {
                            PrintProfitTable(coins, poolProfitsDictionary, nicehashAlgorithms, nicehashProfitsDictionary, settings);
                        }

                        //Mine using best algorithm/coin
                        if (!token.IsCancellationRequested)
                        {
                            Console.WriteLine();
                            Mineable bestOverallMineable = null;
                            if (_manualSelection && _manualSelectedMineable != null)
                            {
                                bestOverallMineable = _manualSelectedMineable;
                            }
                            else
                            {
                                bestOverallMineable = profitSwitchingStrategy.GetBestMineable(bestPoolminedCoin, bestNicehashAlgorithm, currentReward, settings)?.Mineable;
                            }
                            if (bestOverallMineable != null)
                            {
                                Console.WriteLine($"Determined best mining method: {bestOverallMineable.DisplayName}");
                                if (_currentMineable == null || _currentMineable.Id != bestOverallMineable.Id)
                                {
                                    StartMiner(bestOverallMineable, settings, appFolderPath, appRootFolder);
                                }
                            }
                            else
                            {
                                Log.Information("Couldn't determine best mining method.");
                            }
                        }

                        var statusUpdaterTask = StatusUpdaterTaskAsync(DateTimeOffset.Now.AddSeconds(settings.ProfitCheckInterval), settings, appRootFolder, coins, poolProfitsDictionary, nicehashAlgorithms, nicehashProfitsDictionary, statusCts.Token);

                        if (!token.IsCancellationRequested)
                        {
                            if (settings.ProfitCheckInterval > 0)
                            {
                                Task.Delay(TimeSpan.FromSeconds(settings.ProfitCheckInterval), token).Wait(token);
                            }
                            else
                            {
                                token.WaitHandle.WaitOne();
                            }
                        }

                        statusCts.Cancel();
                    }
                    catch (TaskCanceledException)
                    {
                        Log.Information("Cancelled profit task.");
                        statusCts.Cancel();
                        
                    }
                    catch (AggregateException)
                    {
                        Log.Information("Cancelled profit task 2.");
                        statusCts.Cancel();
                        
                    }
                    catch (OperationCanceledException)
                    {
                        Log.Information("Cancelled profit task 3.");
                        statusCts.Cancel();
                        
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Profit switcher task failed: " + ex);
                        statusCts.Cancel();
                    }
                }
            }, token);
        }

        private static Task StatusUpdaterTaskAsync(DateTimeOffset estReset, Settings settings, DirectoryInfo appRootFolder, List<Coin> coins, Dictionary<ProfitProvider, Dictionary<string, Profit>> poolProfitsDictionary, List<NicehashAlgorithm> nicehashAlgorithms, Dictionary<int, Profit> nicehashProfitsDictionary, CancellationToken token)
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

                        if(_manualSelection)
                        {
                            Console.Write(" Press '");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("m");
                            Console.ResetColor();
                            Console.WriteLine("' to disable manual selection.");
                        }
                        else
                        {
                            Console.Write(" Press '");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("m");
                            Console.ResetColor();
                            Console.WriteLine("' to select a coin / NiceHash algorithm to mine manually.");
                        }

                        if ((_manualWatchdogEnabled.HasValue && _manualWatchdogEnabled.Value) || (!_manualWatchdogEnabled.HasValue && settings.EnableWatchdog))
                        {
                            Console.Write(" Press '");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("w");
                            Console.ResetColor();
                            Console.WriteLine("' to disable watchdog.");
                        }
                        else
                        {
                            Console.Write(" Press '");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("w");
                            Console.ResetColor();
                            Console.WriteLine("' to enable watchdog.");
                        }

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
                                Console.Write(" Mining:     ");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(_currentMineable.DisplayName);
                                Console.ResetColor();
                                Console.Write(" at ");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(roundedHashRate + "H/s");
                                Console.ResetColor();
                                if (currentUsdReward > 0 || currentCoinReward > 0)
                                {
                                    Console.Write(" (");
                                    if (currentUsdReward > 0)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.Write(currentUsdReward.ToCurrency("$"));
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
                                Console.Write(" Hashing:    ");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(_currentMineable.DisplayName);
                                Console.ResetColor();
                                Console.Write(" on NiceHash at ");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(roundedHashRate + "H/s");
                                Console.ResetColor();
                                if (currentUsdReward > 0)
                                {
                                    Console.Write(" (");
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.Write(currentUsdReward.ToCurrency("$"));
                                    Console.ResetColor();
                                    Console.WriteLine(" per day)");
                                }
                            }

                            var differenceToLastProfitSwitch = DateTimeOffset.Now.Subtract(_lastProfitSwitch).TotalSeconds;
                            if (differenceToLastProfitSwitch < settings.ProfitSwitchCooldown)
                            {
                                Console.Write(" Cooldown:   Must wait ");
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(Math.Round(settings.ProfitSwitchCooldown - differenceToLastProfitSwitch, 0, MidpointRounding.AwayFromZero));
                                Console.ResetColor();
                                Console.WriteLine(" seconds before switching.");
                            }
                            else
                            {
                                Console.WriteLine(" Cooldown:   Ready to switch.");
                            }

                            if (_manualSelection)
                            {
                                if (_manualSelectedMineable != null)
                                {
                                    Console.WriteLine($" Switching:  Manual -> {_manualSelectedMineable.DisplayName}");
                                }
                                else
                                {
                                    Console.WriteLine(" Switching:  Manual -> Press the corresponding key to select a specific coin / NiceHash algorithm");
                                }
                            }
                            else
                            {
                                Console.WriteLine($" Switching:  {settings.ProfitSwitchingStrategy.ToString()}");
                            }

                            if ((_manualWatchdogEnabled.HasValue && _manualWatchdogEnabled.Value) || (!_manualWatchdogEnabled.HasValue && settings.EnableWatchdog))
                            {
                                Console.Write(" Watchdog:   ");
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
                                Console.WriteLine(" Watchdog:   Not activated.");
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
                        Task.Delay(TimeSpan.FromSeconds(settings.DisplayUpdateInterval), token).Wait(token);
                    }
                }
                catch (TaskCanceledException)
                {
                    Log.Information(" Cancelled status task.");
                    return;
                }
                catch (AggregateException)
                {
                    Log.Information(" Cancelled status task 2.");
                    return;
                }
                catch (OperationCanceledException)
                {
                    Log.Information("Cancelled status task 3.");
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(" Status task failed: " + ex);
                }
            }, token);
        }

        private static Task WatchdogTaskAsync(Settings settings, string appFolderPath, DirectoryInfo appRootFolder, CancellationToken token)
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

                            if (!token.IsCancellationRequested)
                            {
                                if (differenceRatio < settings.WatchdogCriticalThreshold)
                                {
                                    _watchdogOvershots++;
                                }
                                else
                                {
                                    _watchdogOvershots = 0;
                                }
                            }

                            if (_watchdogOvershots > settings.WatchdogAllowedOversteps && !token.IsCancellationRequested)
                            {
                                Log.Information("Watchdog: Too many overshots -> Requesting reset");
                                ResetApp(settings, appFolderPath, true);
                            }
                        }
                        Task.Delay(TimeSpan.FromSeconds(settings.WatchdogInterval), token).Wait(token);
                    }
                }
                catch (TaskCanceledException)
                {
                    Log.Information("Cancelled watchdog task.");
                    return;
                }
                catch (AggregateException)
                {
                    Log.Information("Cancelled watchdog task 2.");
                    return;
                }
                catch (OperationCanceledException)
                {
                    Log.Information("Cancelled watchdog task 3.");
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error("Watchdog task failed: " + ex);
                }
            }, token);
        }

        private static Task KeypressesTaskAsync(string appFolderPath, DirectoryInfo appFolder, Settings settings, List<Coin> coins, List<NicehashAlgorithm> nicehashAlgorithms, CancellationToken token)
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
                                RestartProfitTask(appFolderPath,appFolder,settings,coins,nicehashAlgorithms);
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
                            else if (readKey.Key == ConsoleKey.M)
                            {
                                //Toggle manual mode
                                _manualSelection = !_manualSelection;
                                _lastProfitSwitch = DateTimeOffset.MinValue;
                                RestartProfitTask(appFolderPath, appFolder, settings, coins, nicehashAlgorithms);
                            }
                            else if (readKey.Key == ConsoleKey.W)
                            {
                                //Toggle watchdog mode
                                if (!_manualWatchdogEnabled.HasValue)
                                {
                                    _manualWatchdogEnabled = !settings.EnableWatchdog;
                                }
                                else
                                {
                                    _manualWatchdogEnabled = !_manualWatchdogEnabled.Value;
                                }

                                if (_manualWatchdogEnabled.Value)
                                {
                                    _watchdogCts?.Cancel();
                                    _watchdogCts = new CancellationTokenSource();
                                    var watchdogTask = WatchdogTaskAsync(settings, appFolderPath, appFolder, _watchdogCts.Token);
                                }
                                else
                                {
                                    _watchdogCts?.Cancel();
                                }
                               
                            }
                            else
                            {
                                //Manual selection
                                int keyIndex = Helpers.ManualSelectionDictionary.Keys.ToList().IndexOf(readKey.Key);
                                if (keyIndex >= 0)
                                {
                                    if (keyIndex < coins.Count)
                                    {
                                        if (_manualSelectedMineable != coins[keyIndex])
                                        {
                                            _manualSelectedMineable = coins[keyIndex];
                                            _lastProfitSwitch = DateTimeOffset.MinValue;
                                            RestartProfitTask(appFolderPath, appFolder, settings, coins, nicehashAlgorithms);
                                        }
                                    }
                                    else
                                    {
                                        int nicehashIndex = keyIndex - coins.Count;
                                        if (nicehashIndex < nicehashAlgorithms.Count)
                                        {
                                            if (_manualSelectedMineable != nicehashAlgorithms[nicehashIndex])
                                            {
                                                _manualSelectedMineable = nicehashAlgorithms[nicehashIndex];
                                                _lastProfitSwitch = DateTimeOffset.MinValue;
                                                RestartProfitTask(appFolderPath, appFolder, settings, coins, nicehashAlgorithms);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    Log.Information("Cancelled key presses task.");
                    //ResetApp(settings,appFolderPath);
                    return;
                }
                catch (AggregateException)
                {
                    Log.Information("Cancelled key presses task 2.");
                    //ResetApp(settings, appFolderPath);
                    return;
                }
                catch (OperationCanceledException)
                {
                    Log.Information("Cancelled key presses task 3.");
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error("Key presses task failed: " + ex);
                }
            }, token);
        }

        private static void RestartProfitTask(string appFolderPath, DirectoryInfo appFolder, Settings settings, List<Coin> coins, List<NicehashAlgorithm> nicehashAlgorithms)
        {
            _profitSwitcherTaskCts.Cancel();
            _profitSwitcherTask.Wait(10000);
            _profitSwitcherTaskCts = new CancellationTokenSource();
            _profitSwitcherTask = ProfitSwitcherTaskAsync(appFolderPath, appFolder, settings, coins, nicehashAlgorithms, _profitSwitcherTaskCts.Token);
        }

        private static void ResetConsole()
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

        private static void WriteInCyan(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        private static void PrintProfitTable(List<Coin> coins, Dictionary<ProfitProvider, Dictionary<string, Profit>> poolProfitsDictionary, List<NicehashAlgorithm> nicehashAlgorithms, Dictionary<int, Profit> nicehashProfitsDictionary, Settings settings)
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

            int mineableIndex = -1;
            foreach (var coin in coins)
            {
                mineableIndex++;
                string displayName = _manualSelection ? $"[{Helpers.ManualSelectionDictionary.ElementAt(mineableIndex).Value}] {coin.DisplayName}" : coin.DisplayName;
                poolGrid.Children.Add(new Cell(displayName) { Color = coin.IsEnabled() ? ConsoleColor.Yellow : ConsoleColor.DarkGray, Padding = new Thickness(2, 0, 10, 0) });
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
                mineableIndex++;
                string displayName = _manualSelection ? $"[{Helpers.ManualSelectionDictionary.ElementAt(mineableIndex).Value}] {nicehashAlgorithm.DisplayName}" : nicehashAlgorithm.DisplayName;
                nicehashGrid.Children.Add(new Cell(displayName) { Color = nicehashAlgorithm.IsEnabled() ? ConsoleColor.Yellow : ConsoleColor.DarkGray, Padding = new Thickness(2, 0, 2, 0) });
                var profit = nicehashProfitsDictionary.GetValueOrDefault(nicehashAlgorithm.ApiId, new Profit());
                nicehashGrid.Children.Add(profit.UsdRewardLive <= 0
                    ? new Cell("No data") { Color = ConsoleColor.DarkGray, Align = Align.Center, Padding = new Thickness(2, 0, 2, 0) }
                    : new Cell(profit.ToString()) { Color = nicehashAlgorithm.IsEnabled() ? ConsoleColor.Gray : ConsoleColor.DarkGray, Align = Align.Center, Padding = new Thickness(2, 0, 2, 0) });
            }
            var nicehashDoc = new Document(nicehashGrid);
            ConsoleRenderer.RenderDocument(nicehashDoc);

            Console.WriteLine();
        }

        private static void ResetApp(Settings settings, string appFolderPath, bool runResetScript)
        {
            Log.Information(runResetScript ? "Resetting app with reset script!" : "Resetting app without reset script!");
            _profitSwitcherTaskCts.Cancel();
            _profitSwitcherTask.Wait(10000);
            StopMiner();
            Thread.Sleep(TimeSpan.FromSeconds(settings.MinerStartDelay));
            if (runResetScript)
            {
                ExecuteScript(settings.ResetScript, appFolderPath);
            }
            _keyPressesCts?.Cancel();
            _mainResetCts?.Cancel();
            _lastProfitSwitch = DateTimeOffset.MinValue;
        }

        private static void StartMiner(Mineable mineable, Settings settings, string appRoot, DirectoryInfo appRootFolder)
        {
            var differenceToLastProfitSwitch = DateTimeOffset.Now.Subtract(_lastProfitSwitch).TotalSeconds;
            if (differenceToLastProfitSwitch > settings.ProfitSwitchCooldown)
            {
                StopMiner();

                _currentMineable = mineable;

                ExecuteScript(mineable.PrepareScript, appRoot);

                _currentMiner = MinerFactory.GetMiner(mineable.Miner);
                _lastProfitSwitch = DateTimeOffset.Now;

                try
                {
                    Log.Information($"Starting {_currentMiner.Name} with {mineable.DisplayName}.");
                    _currentMiner.StartMiner(mineable, settings, appRoot, appRootFolder);
                }
                catch (Exception ex)
                {
                    Log.Warning("Couldn't start miner: " + ex);
                    ResetApp(settings, appRoot, false);
                }

                if(_manualWatchdogEnabled.HasValue)
                {
                    if (_manualWatchdogEnabled.Value)
                    {
                        _watchdogCts?.Cancel();
                        _watchdogCts = new CancellationTokenSource();
                        var watchdogTask = WatchdogTaskAsync(settings, appRoot, appRootFolder, _watchdogCts.Token);
                    }
                }
                else
                {
                    if (settings.EnableWatchdog)
                    {
                        _watchdogCts?.Cancel();
                        _watchdogCts = new CancellationTokenSource();
                        var watchdogTask = WatchdogTaskAsync(settings, appRoot, appRootFolder, _watchdogCts.Token);
                    }
                }
                
            }
            else
            {
                Console.WriteLine($"Didn't switched to {mineable.DisplayName}! Waiting {settings.ProfitSwitchCooldown} seconds to cooldown.");
            }
        }

        private static void StopMiner()
        {
            if (_currentMiner != null)
            {
                Log.Information($"Stopping {_currentMiner.Name}.");
            }
            _watchdogCts?.Cancel();
            _currentMiner?.StopMiner();
            _currentMiner = null;
            _currentMineable = null;
            _watchdogOvershots = 0;
        }

        private static void ExecuteScript(string scriptPath, string appFolderPath)
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
                                resetProcess.StartInfo.FileName = "x-terminal-emulator";
                                resetProcess.StartInfo.Arguments = $"-e \"'{Helpers.ResolveToFullPath(scriptPath, appFolderPath)}'\"";
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
                Log.Error("Couldn't execute script: " + scriptPath);
                Log.Error("Exception: " + ex);
            }
        }
    }
}
