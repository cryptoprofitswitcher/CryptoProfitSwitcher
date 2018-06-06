using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Miners;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptonightProfitSwitcher
{
    class Program
    {
        const int VERSION = 3;

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
                    var versionText = Helpers.GetJsonFromUrl("https://raw.githubusercontent.com/cryptoprofitswitcher/CryptonightProfitSwitcher/master/version.txt", settings, appFolder);
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
                        var poolProfitsDictionary = new Dictionary<string, double>();
                        var profitsJson = Helpers.GetJsonFromUrl("https://minecryptonight.net/api/rewards?hr=1000", settings, appRootFolder);
                        dynamic profits = JsonConvert.DeserializeObject<ExpandoObject>(profitsJson, new ExpandoObjectConverter());
                        long baseHashrate = profits.hash_rate;
                        double cnHeavyFactor = Helpers.GetProperty<double>(profits, "cryptonight-heavy_factor");
                        double cnLiteFactor = Helpers.GetProperty<double>(profits, "cryptonight-lite_factor");
                        double cnBittubeFactor = Helpers.GetProperty<double>(profits, "cryptonight-lite-tube_factor");
                        foreach (dynamic reward in profits.rewards)
                        {
                            string tickerSymbol = reward.ticker_symbol;
                            string algorithm = reward.algorithm;
                            double rewardUsd = 0;
                            switch (settings.ProfitTimeframe)
                            {
                                case ProfitTimeframe.Live:
                                    rewardUsd = reward.reward_24h.usd;
                                    break;
                                case ProfitTimeframe.OneHour:
                                case ProfitTimeframe.ThreeHours:
                                case ProfitTimeframe.Day:
                                case ProfitTimeframe.Week:
                                    throw new NotImplementedException("Currently only ProfitTimeframe.Live is supported.");
                            }
                            //Adjust profits based on user defined hashrate
                            Coin matchedCoin = coins.FirstOrDefault(c => c.TickerSymbol == tickerSymbol);

                            if (matchedCoin != null)
                            {
                                switch (algorithm)
                                {
                                    case "cryptonight-v1":
                                        rewardUsd = (rewardUsd / 1000) * matchedCoin.GetExpectedHashrate(settings);
                                        break;
                                    case "cryptonight-heavy":
                                        rewardUsd = (rewardUsd / (1000 * cnHeavyFactor)) * matchedCoin.GetExpectedHashrate(settings);
                                        break;
                                    case "cryptonight-lite-v1":
                                        rewardUsd = (rewardUsd / (1000 * cnLiteFactor)) * matchedCoin.GetExpectedHashrate(settings);
                                        break;
                                    case "cryptonight-lite-tube":
                                        rewardUsd = (rewardUsd / (1000 * cnBittubeFactor)) * matchedCoin.GetExpectedHashrate(settings);
                                        break;
                                }
                                poolProfitsDictionary[tickerSymbol] = rewardUsd;
                                Console.WriteLine("Got profit data for : " + tickerSymbol);
                            }
                        }

                        // Get best pool mined coin
                        Coin bestPoolminedCoin = null;
                        double bestPoolminedCoinProfit = 0;
                        foreach (var coin in coins)
                        {
                            double profit = poolProfitsDictionary.GetValueOrDefault(coin.TickerSymbol, 0);
                            if (bestPoolminedCoin == null || profit > bestPoolminedCoinProfit)
                            {
                                bestPoolminedCoinProfit = profit;
                                bestPoolminedCoin = coin;
                            }
                        }

                        if (bestPoolminedCoin != null)
                        {
                            Console.WriteLine("Got best pool mined coin: " + bestPoolminedCoin.DisplayName);
                        }
                        else
                        {
                            Console.WriteLine("Couldn't determine best pool mined coin.");
                        }

                        //Get BTC quote
                        var btcJson = Helpers.GetJsonFromUrl("https://api.coinmarketcap.com/v2/ticker/1/?convert=USD", settings, appRootFolder);
                        dynamic btc = JObject.Parse(btcJson);
                        double btcUsdPrice = btc.data.quotes.USD.price;
                        Console.WriteLine("Got BTC exchange rate: " + btcUsdPrice);

                        //Get Nicehash Profit
                       var nicehashProfitsDictionary = new Dictionary<int, double>();
                        var nicehashProfitsJson = Helpers.GetJsonFromUrl("https://api.nicehash.com/api?method=stats.global.current", settings, appRootFolder);
                        dynamic nicehashProfits = JObject.Parse(nicehashProfitsJson);
                        var result = nicehashProfits.result;

                        foreach (dynamic stat in result.stats)
                        {
                            int algo = stat.algo;
                            double price = stat.price;

                            var matchedAlgorithm = nicehashAlgorithms.FirstOrDefault(na => na.ApiId == algo);
                            if (matchedAlgorithm != null)
                            {
                                double btcReward = 0;
                                btcReward = (price / 1000000) * matchedAlgorithm.GetExpectedHashrate(settings);
                                var usdReward = btcReward * btcUsdPrice * settings.NicehashPreferFactor;
                                nicehashProfitsDictionary[matchedAlgorithm.ApiId] = usdReward;
                                Console.WriteLine("Got profit data for Nicehash: " + matchedAlgorithm.DisplayName);
                            }
                        }

                        //Get best nicehash algorithm
                        NicehashAlgorithm bestNicehashAlgorithm = null;
                        double bestNicehashAlgorithmProfit = 0;
                        foreach (var nicehashAlgorithm in nicehashAlgorithms)
                        {
                            double profit = nicehashProfitsDictionary.GetValueOrDefault(nicehashAlgorithm.ApiId, 0);
                            if (bestNicehashAlgorithm == null || profit > bestNicehashAlgorithmProfit)
                            {
                                bestNicehashAlgorithmProfit = profit;
                                bestNicehashAlgorithm = nicehashAlgorithm;
                            }
                        }

                        if (bestNicehashAlgorithm != null)
                        {
                            Console.WriteLine("Got best nicehash algorithm: " + bestNicehashAlgorithm.DisplayName);
                        }
                        else
                        {
                            Console.WriteLine("Couldn't determine berst nicehash algorithm.");
                        }

                        //Print table
                        PrintProfitTable(coins, poolProfitsDictionary, nicehashAlgorithms, nicehashProfitsDictionary);

                        //Mine using best algorithm/coin
                        Console.WriteLine();
                        if (bestPoolminedCoin != null && bestPoolminedCoinProfit > (bestNicehashAlgorithmProfit * settings.NicehashPreferFactor))
                        {
                            Console.WriteLine($"Determined best mining method: Mine {bestPoolminedCoin.DisplayName} in a pool at {Helpers.ToCurrency(bestPoolminedCoinProfit, "$")} per day.");
                            if (_currentMineable == null || _currentMineable.Id != bestPoolminedCoin.Id)
                            {
                                StartMiner(bestPoolminedCoin, settings, appFolderPath, appRootFolder);
                            }
                        }
                        else if (bestNicehashAlgorithm != null)
                        {
                            Console.WriteLine($"Determined best mining method: Provide hash power for {bestNicehashAlgorithm.DisplayName} on NiceHash at {Helpers.ToCurrency(bestNicehashAlgorithmProfit, "$")} per day.");
                            if (_currentMineable == null || _currentMineable.Id != bestNicehashAlgorithm.Id)
                            {
                                StartMiner(bestNicehashAlgorithm, settings, appFolderPath, appRootFolder);
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
                    catch(Exception ex)
                    {
                        Console.WriteLine("Profit switcher task failed: " + ex.Message);
                        statusCts.Cancel();
                    }
                }
            }, token);
        }

        static Task StatusUpdaterTask(DateTimeOffset estReset, Settings settings, DirectoryInfo appRootFolder, List<Coin> coins, Dictionary<string, double> poolProfitsDictionary, List<NicehashAlgorithm> nicehashAlgorithms, Dictionary<int, double> nicehashProfitsDictionary, CancellationToken token)
        {
            return Task.Run(() =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        ResetConsole();
                        Console.WriteLine("Press 'p' to refresh profits immediately.");
                        Console.WriteLine("Press 'r' to reset the app and run the reset script if set.");
                        Console.WriteLine("Press 'q' to quit.");

                        PrintProfitTable(coins, poolProfitsDictionary, nicehashAlgorithms, nicehashProfitsDictionary);

                        WriteInCyan("CURRENT STATUS: ");

                        if (_currentMineable != null && _currentMiner != null)
                        {
                            var currentHashrate = _currentMiner.GetCurrentHashrate(settings, appRootFolder);
                            var estimatedReward = _currentMineable is Coin ? poolProfitsDictionary[((Coin)_currentMineable).TickerSymbol] : nicehashProfitsDictionary[((NicehashAlgorithm)_currentMineable).ApiId];
                            double currentReward = (estimatedReward / _currentMineable.GetExpectedHashrate(settings)) * currentHashrate;

                            if (_currentMineable is NicehashAlgorithm)
                            {
                                Console.WriteLine($"Providing hashpower on NiceHash: '{_currentMineable.DisplayName}' at {currentHashrate}H/s ({Helpers.ToCurrency(currentReward, "$")} per day)");
                            }
                            else
                            {
                                Console.WriteLine($"Mining: '{_currentMineable.DisplayName}' at {currentHashrate}H/s ({Helpers.ToCurrency(currentReward, "$")} per day)");
                            }

                            if (settings.EnableWatchdog)
                            {
                                Console.WriteLine($"Watchdog: {_watchdogOvershots} consecutive overshots out of {settings.WatchdogAllowedOversteps} allowed.");
                            }
                            else
                            {
                                Console.WriteLine("Watchdog: Not activated.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Not mining.");
                        }

                        Console.WriteLine();
                        if (settings.ProfitCheckInterval > 0)
                        {
                            int remainingSeconds = (int)estReset.Subtract(DateTimeOffset.Now).TotalSeconds;
                            Console.WriteLine($"Will refresh automatically in {remainingSeconds} seconds.");
                        }
                        else
                        {
                            Console.WriteLine("Won't refresh automatically.");
                        }
                        Task.Delay(TimeSpan.FromSeconds(settings.DisplayUpdateInterval), token).Wait();
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Cancelled status task.");
                    return;
                }
                catch (AggregateException)
                {
                    Console.WriteLine("Cancelled status task.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Status task failed: " + ex.Message);
                }
            }, token);
        }

        static Task WatchdogTask(Settings settings,string appFolderPath, DirectoryInfo appRootFolder, CancellationToken token)
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
                                ResetApp(settings, appFolderPath);
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
                            else if (readKey.Key == ConsoleKey.R)
                            {
                                //Reset app
                                ResetApp(settings, appFolderPath);
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
            Console.Clear();
            WriteInCyan("CRYPTONIGHT PROFIT SWITCHER");
            if (!_newVersionAvailable)
            {
                Console.WriteLine("Version: " + Helpers.GetApplicationVersion());
            }
            else
            {
                Console.Write("Version: " + Helpers.GetApplicationVersion());
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" (Update available!)");
                Console.ResetColor();
            }
            Console.WriteLine("On Github: https://github.com/cryptoprofitswitcher/CryptonightProfitSwitcher");
            Console.WriteLine();
        }

        static void WriteInCyan(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        static void PrintProfitTable(List<Coin> coins, Dictionary<string, double> poolProfitsDictionary, List<NicehashAlgorithm> nicehashAlgorithms, Dictionary<int, double> nicehashProfitsDictionary)
        {
            Console.WriteLine();
            WriteInCyan("POOL MINED COINS SUMMARY: ");
            foreach (var coin in coins)
            {
                double reward = poolProfitsDictionary.GetValueOrDefault(coin.TickerSymbol, 0);
                Console.WriteLine($"{coin.DisplayName}: {Helpers.ToCurrency(reward, "$")}");
            }
            Console.WriteLine();
            WriteInCyan("NICEHASH ALGORITHMS SUMMARY: ");
            foreach (var nicehashAlgorithm in nicehashAlgorithms)
            {
                double reward = nicehashProfitsDictionary.GetValueOrDefault(nicehashAlgorithm.ApiId, 0);
                Console.WriteLine($"{nicehashAlgorithm.DisplayName}: {Helpers.ToCurrency(reward, "$")}");
            }
            Console.WriteLine();
        }

        static void ResetApp(Settings settings, string appFolderPath)
        {
            _profitSwitcherTaskCts.Cancel();
            _profitSwitcherTask.Wait();
            StopMiner();
            Thread.Sleep(settings.MinerStartDelay);
            ExecuteResetScript(settings, appFolderPath);
            _keyPressesCts?.Cancel();
            _mainResetCts?.Cancel();
        }
        static void StartMiner(Mineable mineable, Settings settings, string appRoot, DirectoryInfo appRootFolder)
        {
            StopMiner();

            _currentMineable = mineable;

            switch (mineable.Miner)
            {
                case Miner.XmrStak:
                    _currentMiner = new XmrStakMiner(false);
                    break;
                case Miner.CastXmr:
                    _currentMiner = new CastXmrMiner();
                    break;
                case Miner.SRBMiner:
                    _currentMiner = new SrbMiner();
                    break;
                default:
                    throw new NotImplementedException("Couldn't start miner, unknown miner: " + mineable.Miner);
            }

            _currentMiner.StartMiner(mineable, settings, appRoot, appRootFolder);

            if (settings.EnableWatchdog)
            {
                _watchdogCts = new CancellationTokenSource();
                var watchdogTask = WatchdogTask(settings,appRoot, appRootFolder, _watchdogCts.Token);
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

        static void ExecuteResetScript(Settings settings, string appFolderPath)
        {
            if (!String.IsNullOrEmpty(settings.ResetScript))
            {
                //Execute reset script
                var resetProcess = new Process();
                resetProcess.StartInfo.FileName = "cmd.exe";
                resetProcess.StartInfo.Arguments = $"/c {Helpers.ResolveToArgumentPath(settings.ResetScript, appFolderPath)}";
                resetProcess.StartInfo.UseShellExecute = true;
                resetProcess.StartInfo.CreateNoWindow = false;
                resetProcess.StartInfo.RedirectStandardOutput = false;
                resetProcess.Start();
                resetProcess.WaitForExit();
            }
        }
    }
}
