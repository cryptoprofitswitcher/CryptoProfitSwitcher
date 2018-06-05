using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CryptonightProfitSwitcher
{
    class Program
    {
        const int VERSION = 2;

        static Process _process = null;
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
                var appFolderPath = GetApplicationRoot();
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
                    var versionText = GetJsonFromUrl("https://raw.githubusercontent.com/cryptoprofitswitcher/CryptonightProfitSwitcher/master/version.txt", settings, appFolder);
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
                        var profitsJson = GetJsonFromUrl("https://minecryptonight.net/api/rewards?hr=1000", settings, appRootFolder);
                        dynamic profits = JsonConvert.DeserializeObject<ExpandoObject>(profitsJson, new ExpandoObjectConverter());
                        long baseHashrate = profits.hash_rate;
                        double cnHeavyFactor = GetProperty<double>(profits, "cryptonight-heavy_factor");
                        double cnLiteFactor = GetProperty<double>(profits, "cryptonight-lite_factor");
                        double cnBittubeFactor = GetProperty<double>(profits, "cryptonight-lite-tube_factor");
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
                                    throw new NotImplementedException();

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
                        var btcJson = GetJsonFromUrl("https://api.coinmarketcap.com/v2/ticker/1/?convert=USD", settings, appRootFolder);
                        dynamic btc = JObject.Parse(btcJson);
                        double btcUsdPrice = btc.data.quotes.USD.price;
                        Console.WriteLine("Got BTC exchange rate: " + btcUsdPrice);

                        //Get Nicehash Profit
                       var nicehashProfitsDictionary = new Dictionary<int, double>();
                        var nicehashProfitsJson = GetJsonFromUrl("https://api.nicehash.com/api?method=stats.global.current", settings, appRootFolder);
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
                            Console.WriteLine($"Determined best mining method: Mine {bestPoolminedCoin.DisplayName} in a pool at {ToCurrency(bestPoolminedCoinProfit, "$")} per day.");
                            if (_currentMineable == null || _currentMineable.Id != bestPoolminedCoin.Id)
                            {
                                StartMiner(bestPoolminedCoin, settings, appFolderPath, appRootFolder);
                            }
                        }
                        else if (bestNicehashAlgorithm != null)
                        {
                            Console.WriteLine($"Determined best mining method: Provide hash power for {bestNicehashAlgorithm.DisplayName} on NiceHash at {ToCurrency(bestNicehashAlgorithmProfit, "$")} per day.");
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

                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine("CURRENT STATUS: ");

                        if (_currentMineable != null)
                        {
                            var currentHashrate = GetCurrentHashrate(settings.XmrStakApiPort, settings, appRootFolder);
                            double currentReward = 0;
                            var estimatedReward = _currentMineable is Coin ? poolProfitsDictionary[((Coin)_currentMineable).TickerSymbol] : nicehashProfitsDictionary[((NicehashAlgorithm)_currentMineable).ApiId];

                            switch (_currentMineable.Algorithm)
                            {
                                case Algorithm.CryptonightV7:
                                    currentReward = (estimatedReward / _currentMineable.GetExpectedHashrate(settings)) * currentHashrate;
                                    break;
                                case Algorithm.CryptonightHeavy:
                                    currentReward = (estimatedReward / _currentMineable.GetExpectedHashrate(settings)) * currentHashrate;
                                    break;
                                case Algorithm.CryptonightLite:
                                    currentReward = (estimatedReward / _currentMineable.GetExpectedHashrate(settings)) * currentHashrate;
                                    break;
                                case Algorithm.CryptonightBittube:
                                    currentReward = (estimatedReward / _currentMineable.GetExpectedHashrate(settings)) * currentHashrate;
                                    break;
                            }

                            if (_currentMineable is NicehashAlgorithm)
                            {
                                Console.WriteLine($"Providing hashpower on NiceHash: '{_currentMineable.DisplayName}' at {currentHashrate}H/s ({ToCurrency(currentReward, "$")} per day)");
                            }
                            else
                            {
                                Console.WriteLine($"Mining: '{_currentMineable.DisplayName}' at {currentHashrate}H/s ({ToCurrency(currentReward, "$")} per day)");
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
                        if (_currentMineable != null)
                        {
                            double estimatedHashrate = _currentMineable.GetExpectedHashrate(settings);
                            double actualHashrate = GetCurrentHashrate(settings.XmrStakApiPort, settings, appRootFolder);
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
            Console.WriteLine("CRYPTONIGHT PROFIT SWITCHER");
            if (!_newVersionAvailable)
            {
                Console.WriteLine("Version: " + GetApplicationVersion());
            }
            else
            {
                Console.WriteLine("Version: " + GetApplicationVersion() + " (Update available)");
            }
            Console.WriteLine("On Github: https://github.com/cryptoprofitswitcher/CryptonightProfitSwitcher");
            Console.WriteLine();
        }

        static void PrintProfitTable(List<Coin> coins, Dictionary<string, double> poolProfitsDictionary, List<NicehashAlgorithm> nicehashAlgorithms, Dictionary<int, double> nicehashProfitsDictionary)
        {
            Console.WriteLine();
            Console.WriteLine("POOL MINED COINS SUMMARY: ");
            foreach (var coin in coins)
            {
                double reward = poolProfitsDictionary.GetValueOrDefault(coin.TickerSymbol, 0);
                Console.WriteLine($"{coin.DisplayName}: {ToCurrency(reward, "$")}");
            }
            Console.WriteLine();
            Console.WriteLine("NICEHASH ALGORITHMS SUMMARY: ");
            foreach (var nicehashAlgorithm in nicehashAlgorithms)
            {
                double reward = nicehashProfitsDictionary.GetValueOrDefault(nicehashAlgorithm.ApiId, 0);
                Console.WriteLine($"{nicehashAlgorithm.DisplayName}: {ToCurrency(reward, "$")}");
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
            _process = new Process();
            string xmrPath = ResolveToFullPath(mineable.XmrStakPath, appRoot);
            string xmrFolderPath = Path.GetDirectoryName(xmrPath);
            var xmrDirectory = new DirectoryInfo(xmrFolderPath);
            _process.StartInfo.FileName = xmrPath;
            string configPath = ResolveToFullPath(mineable.ConfigPath, appRoot);
            File.Copy(configPath, Path.Combine(xmrFolderPath, "current_config.txt"), true);
            string args = "-c current_config.txt";

            if (!String.IsNullOrEmpty(mineable.PoolsPath))
            {
                // Use given pool config
                string poolPath = ResolveToFullPath(mineable.PoolsPath, appRoot);
                File.Copy(poolPath, Path.Combine(xmrFolderPath, "current_pool.txt"), true);
            }
            else
            {
                // Auto-Generate pool config from Mineable
                string poolConfigJson = GeneratePoolConfigJson(mineable);
                File.WriteAllText(Path.Combine(xmrFolderPath, "current_pool.txt"), poolConfigJson);
            }
            args += " -C current_pool.txt";

            if (String.IsNullOrEmpty(mineable.CpuPath))
            {
                args += " --noCPU";
            }
            else
            {
                string cpuPath = ResolveToFullPath(mineable.CpuPath, appRoot);
                File.Copy(cpuPath, Path.Combine(xmrFolderPath, "current_cpu.txt"), true);
                args += " --cpu current_cpu.txt";
            }

            if (String.IsNullOrEmpty(mineable.AmdPath))
            {
                args += " --noAMD";
            }
            else
            {
                string amdPath = ResolveToFullPath(mineable.AmdPath, appRoot);
                File.Copy(amdPath, Path.Combine(xmrFolderPath, "current_amd.txt"), true);
                args += " --amd current_amd.txt";
            }

            if (String.IsNullOrEmpty(mineable.NvidiaPath))
            {
                args += " --noNVIDIA";
            }
            else
            {
                string nvidiaPath = ResolveToFullPath(mineable.NvidiaPath, appRoot);
                File.Copy(nvidiaPath, Path.Combine(xmrFolderPath, "current_nvidia.txt"), true);
                args += " --nvidia current_nvidia.txt";
            }

            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = true;
            _process.StartInfo.CreateNoWindow = false;
            _process.StartInfo.RedirectStandardOutput = false;
            _process.StartInfo.WorkingDirectory = xmrFolderPath;
            Thread.Sleep(TimeSpan.FromSeconds(settings.MinerStartDelay));
            _process.Start();

            if (settings.EnableWatchdog)
            {
                _watchdogCts = new CancellationTokenSource();
                var watchdogTask = WatchdogTask(settings,appRoot, appRootFolder, _watchdogCts.Token);
            }
        }

        static void StopMiner()
        {
            _watchdogCts?.Cancel();
            if (_process != null)
            {
                try
                {
                    _process.Kill();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't kill miner process: " + ex.Message);
                }
                _process.Dispose();
                _process = null;
            }
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
                resetProcess.StartInfo.Arguments = $"/c {ResolveToArgumentPath(settings.ResetScript, appFolderPath)}";
                resetProcess.StartInfo.UseShellExecute = true;
                resetProcess.StartInfo.CreateNoWindow = false;
                resetProcess.StartInfo.RedirectStandardOutput = false;
                resetProcess.Start();
                resetProcess.WaitForExit();
            }
        }

        static string GeneratePoolConfigJson(Mineable mineable)
        {
            var dict = new Dictionary<string, object>();
            dict["pool_list"] = new Dictionary<string, object>[]
            {
                new Dictionary<string, object>
                {
                    { "pool_address", mineable.PoolAddress },
                    { "wallet_address", mineable.PoolWalletAddress },
                    { "pool_password", mineable.PoolPassword },
                    { "use_nicehash", mineable is NicehashAlgorithm },
                    { "use_tls", mineable.PoolUseTls },
                    { "tls_fingerprint", mineable.PoolTlsFingerprint },
                    { "pool_weight", mineable.PoolWeight },
                    { "rig_id", mineable.PoolRigId },
                }
            };
            switch (mineable.Algorithm)
            {
                case Algorithm.CryptonightV7:
                    dict["currency"] = "cryptonight_v7";
                    break;
                case Algorithm.CryptonightHeavy:
                    dict["currency"] = "cryptonight_heavy";
                    break;
                case Algorithm.CryptonightLite:
                    dict["currency"] = "cryptonight_lite_v7";
                    break;
                case Algorithm.CryptonightBittube:
                    dict["currency"] = "ipbc";
                    break;
                default:
                    throw new NotImplementedException("Can't get pool algorithm: " + mineable.Algorithm);
            }

            string generatedJson = JsonConvert.SerializeObject(dict, Formatting.Indented);
            
            // Ensure that json starts and ends with an empty line
            var lines = generatedJson.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            if (lines.First().Contains("{"))
            {
                lines.RemoveAt(0);
                lines.Insert(0, String.Empty);
            }
            if (lines.Last().Contains("}"))
            {
                lines.RemoveAt(lines.Count - 1);
                lines.Add(String.Empty);
            }
            string correctedJson = String.Join("\r\n", lines);
            return correctedJson;

        }
        static string ResolveToFullPath(string path, string appRootPath)
        {
            string resolvedPath = Path.Combine(appRootPath, path);
            string fullPath = Path.GetFullPath(resolvedPath);
            return fullPath;
        }

        static string ResolveToArgumentPath(string path, string appRootPath)
        {
            string resolvedPath = Path.Combine(appRootPath, path);
            string fullPath = Path.GetFullPath(resolvedPath);
            return "\"" + fullPath + "\"";
        }

        static double GetCurrentHashrate(int port, Settings settings, DirectoryInfo appRootFolder)
        {
            try
            {
                var xmrJson = GetJsonFromUrl($"http://127.0.0.1:{port}/api.json", settings, appRootFolder);
                dynamic xmr = JObject.Parse(xmrJson);
                JArray totalHashRates = xmr.hashrate.total;
                return totalHashRates[0].ToObject<double>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't get current hashrate: " + ex.Message);
                return 0;
            }
        }

        static string ToCurrency(double val, string currencySymbol)
        {
            var rounded = Math.Round(val, 2, MidpointRounding.AwayFromZero);
            return rounded.ToString() + currencySymbol;
        }
        static T GetProperty<T>(ExpandoObject expando, string propertyName)
        {
            var expandoDict = expando as IDictionary<string, object>;
            var propertyValue = expandoDict[propertyName];
            return (T)propertyValue;
        }

        static string GetApplicationRoot()
        {
            var exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return exePath;
        }

        static string GetApplicationVersion()
        {
            var ver = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            return ver;
        }
        static string GetJsonFromUrl(string url, Settings settings, DirectoryInfo appRootFolder)
        {
            var cacheFolder = appRootFolder.CreateSubdirectory("Cache");
            string responseBody;
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = client.GetAsync(url).Result;

                    response.EnsureSuccessStatusCode();

                    using (HttpContent content = response.Content)
                    {
                        responseBody = response.Content.ReadAsStringAsync().Result;
                        //Save to cache
                        if (settings.EnableCaching && !url.Contains("127.0.0.1"))
                        {
                            try
                            {
                                var urlMapFile = cacheFolder.GetFiles("urlmap.json").FirstOrDefault();
                                if (urlMapFile == null)
                                {
                                    var serialized2 = JsonConvert.SerializeObject(new Dictionary<string, string>());
                                    File.WriteAllText(ResolveToFullPath("Cache/urlmap.json", appRootFolder.FullName), serialized2);
                                    urlMapFile = cacheFolder.GetFiles("urlmap.json").FirstOrDefault();
                                }

                                var urlMapJson = File.ReadAllText(urlMapFile.FullName);
                                var urlMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(urlMapJson);
                                if (urlMap.ContainsKey(url))
                                {
                                    var cachedFilename = urlMap[url];
                                    var cachedFile = cacheFolder.GetFiles(cachedFilename).First();
                                    cachedFile.Delete();
                                }
                                string saveFilename = Guid.NewGuid().ToString() + ".json";
                                string savePath = ResolveToFullPath($"Cache/{saveFilename}", appRootFolder.FullName);
                                File.WriteAllText(savePath, responseBody);
                                urlMap[url] = saveFilename;
                                string serialized = JsonConvert.SerializeObject(urlMap);
                                string urlMapPath = ResolveToFullPath("Cache/urlmap.json", appRootFolder.FullName);
                                File.WriteAllText(urlMapPath, serialized);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Couldn't save to cache: " + ex.Message);
                            }
                        }
                        return responseBody;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't get data from: " + url);
                Console.WriteLine("Error message: " + ex.Message);

                //Try to get from cache
                var urlMapFile = cacheFolder.GetFiles("urlmap.json").FirstOrDefault();
                if (urlMapFile != null)
                {
                    var urlMapJson = File.ReadAllText(urlMapFile.FullName);
                    var urlMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(urlMapJson);
                    if (urlMap.ContainsKey(url))
                    {
                        var cachedFilename = urlMap[url];
                        var cachedFile = cacheFolder.GetFiles(cachedFilename).First();
                        var cachedContent = File.ReadAllText(urlMapFile.FullName);
                        Console.WriteLine("Got data from cache.");
                        return cachedContent;
                    }
                }
                throw;
            }

        }
    }
}
