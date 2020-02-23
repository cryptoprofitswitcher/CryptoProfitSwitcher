using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alba.CsConsoleFormat;
using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.Factories;
using CryptoProfitSwitcher.Miners;
using CryptoProfitSwitcher.Models;
using CryptoProfitSwitcher.ProfitSwitchingStrategies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;

namespace CryptoProfitSwitcher
{
    internal static class Program
    {
        private const int Version = 14;
        private static Config Config { get; set; }
        private static string AppFolderPath { get; set; }
        private static DirectoryInfo AppFolder { get; set; }
        private static bool _newVersionAvailable;
        private static bool _requestQuit;
        private static bool _manualMode;
        private static ProfitSwitchingStrategy? _profitSwitchingStrategy;
        private static CancellationTokenSource _displayUpdaterCts;
        private static CancellationTokenSource _profitFetcherTaskCts;
        private static CancellationTokenSource _miningSwitcherTaskCts;
        private static CancellationTokenSource _keyPressesTaskCts;

        private static Dictionary<Pool, Profit> _poolProfitData;
        private static Dictionary<string, MiningConfig> _currentMininigConfigs = new Dictionary<string, MiningConfig>();
        private static Dictionary<string, MiningConfig> _manualMininigConfigs = null;

        private static void Main(string[] args)
        {
            ResetConsole();
            InitAppFolder();
            InitConfig();
            SetProcessPriority();
            ResetConsole();
            InitLogging();
            CheckForUpdates();
            DownloadMiners();

            BenchmarkIfNeeded();
            FetchPoolProfit(CancellationToken.None);

            _manualMode = Config.EnableManualModeByDefault;
            _profitSwitchingStrategy = Config.ProfitSwitchStrategy;
            _keyPressesTaskCts = new CancellationTokenSource();

            Task.WhenAll(
                PoolProfitFetcherTaskAsync(),
                MiningSwitcherTaskAsync(),
                DisplayUpdaterTaskAsync(),
                KeyPressesTaskAsync(_keyPressesTaskCts.Token)
                ).Wait();
        }

        private static void InitAppFolder()
        {
            AppFolderPath = Helpers.GetApplicationRoot();
            AppFolder = new DirectoryInfo(AppFolderPath);
        }

        private static void InitLogging()
        {
            WriteInfo(" Initialize logging..");
            string logPath = Path.Combine(AppFolderPath, "current_log.txt");
            try
            {
                var logFile = new FileInfo(logPath);
                if (logFile.Exists)
                {
                    string oldlogPath = Path.Combine(AppFolderPath, "previous_log.txt");
                    if (File.Exists(oldlogPath))
                    {
                        File.Delete(oldlogPath);
                    }
                    logFile.MoveTo(oldlogPath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(" Couldn't initalize logging " + e.Message);
            }

            if (Config.EnableLogging)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console(LogEventLevel.Information)
                    .WriteTo.File(logPath)
                    .CreateLogger();
            }
            else
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console(LogEventLevel.Information)
                    .CreateLogger();
            }
        }

        private static void InitConfig()
        {
            WriteInfo(" Initialize config..");
            var configFile = AppFolder.GetFiles("config.json").First();
            var configJson = File.ReadAllText(configFile.FullName);
            Config = JsonConvert.DeserializeObject<Config>(configJson);
        }

        private static void SaveConfig()
        {
            WriteInfo(" Saving new config..");
            var configFile = AppFolder.GetFiles("config.json").First();
            var configJson = JsonConvert.SerializeObject(Config, Formatting.Indented, new Newtonsoft.Json.Converters.StringEnumConverter());
            File.WriteAllText(configFile.FullName, configJson);
        }

        private static void SetProcessPriority()
        {
            WriteInfo(" Setting process priority to " + Config.ProcessPriority);
            using Process p = Process.GetCurrentProcess();
            p.PriorityClass = Config.ProcessPriority;
        }

        private static void CheckForUpdates()
        {
            try
            {
                WriteInfo(" Check for updates..");

                var versionText = Helpers.GetJsonFromUrl("https://raw.githubusercontent.com/cryptoprofitswitcher/CryptonightProfitSwitcher/master/version.txt", false, AppFolder, CancellationToken.None);
                int remoteVersion = Int32.Parse(versionText, CultureInfo.InvariantCulture);
                if (remoteVersion > Version)
                {
                    _newVersionAvailable = true;
                    WriteInfo(" New update available!");
                }
                else
                {
                    WriteInfo(" Your version is up to date.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Couldn't check for updates: " + ex.Message);
            }
        }

        private static void DownloadMiners()
        {
            try
            {
                if (!Config.DisableDownloadMiners)
                {
                    var minersFolder = AppFolder.CreateSubdirectory("Miners");
                    if (!minersFolder.EnumerateFiles().Any())
                    {
                        WriteInfo(" Downloading miners..");

                        if (Helpers.IsWindows())
                        {
                            DownloadAndExtractFromGithub("https://api.github.com/repos/todxx/teamredminer/releases/latest", "win", "teamredminer", minersFolder);
                            DownloadAndExtractFromGithub("https://api.github.com/repos/xmrig/xmrig/releases/latest", "gcc-win64", "xmrig", minersFolder);
                        }

                        if (Helpers.IsLinux())
                        {
                            DownloadAndExtractFromGithub("https://api.github.com/repos/todxx/teamredminer/releases/latest", "linux", "teamredminer", minersFolder);
                            DownloadAndExtractFromGithub("https://api.github.com/repos/xmrig/xmrig/releases/latest", "xenial-x64", "xmrig", minersFolder);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning(e, "Downloading miners failed.");
            }
        }

        private static void DownloadAndExtractFromGithub(string apiUrl, string identifier, string folderName, DirectoryInfo minersFolder)
        {
            string lastReleaseJson = Helpers.GetJsonFromUrl(apiUrl, false, AppFolder, CancellationToken.None);
            JToken jLastRelease = JToken.Parse(lastReleaseJson);
            JToken jwinAsset = jLastRelease["assets"].Children<JToken>().First(asset => asset.Value<string>("name").Contains(identifier));
            string winDownloadUrl = jwinAsset.Value<string>("browser_download_url");
            using var wc = new System.Net.WebClient();
            string teamRedDownloadPath = Path.Combine(minersFolder.FullName, jwinAsset.Value<string>("name"));
            wc.DownloadFile(winDownloadUrl, teamRedDownloadPath);
            ZipFile.ExtractToDirectory(teamRedDownloadPath, minersFolder.FullName);
            DirectoryInfo newFolder = minersFolder.EnumerateDirectories().OrderByDescending(d => d.CreationTimeUtc).First();
            newFolder.MoveTo(Path.Combine(minersFolder.FullName, folderName));
        }

        private static Task PoolProfitFetcherTaskAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                while (!_requestQuit)
                {
                    try
                    {
                        _profitFetcherTaskCts = new CancellationTokenSource();
                        CancellationToken ct = _profitFetcherTaskCts.Token;
                        FetchPoolProfit(ct);
                        Task.Delay(TimeSpan.FromSeconds(Config.ProfitCheckInterval), ct).Wait(ct);
                    }
                    catch (Exception e)
                    {
                        Log.Debug("Profit fetching cancelled:" + e);
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private static void FetchPoolProfit(CancellationToken ct)
        {
            try
            {
                WriteInfo(" Fetching profit data..");
                Dictionary<Pool, Profit> poolProfitData = new Dictionary<Pool, Profit>();
                Dictionary<ProfitProvider, List<Pool>> profitProviderFetches = new Dictionary<ProfitProvider, List<Pool>>();
                foreach (Algorithm algorithm in Config.Algorithms)
                {
                    if (algorithm.Enabled)
                    {
                        foreach (Pool algorithmPool in algorithm.Pools.Where(p => p.Enabled))
                        {
                            if (!profitProviderFetches.ContainsKey(algorithmPool.ProfitProvider))
                            {
                                profitProviderFetches[algorithmPool.ProfitProvider] = new List<Pool>() { algorithmPool };
                            }
                            else
                            {
                                profitProviderFetches[algorithmPool.ProfitProvider].Add(algorithmPool);
                            }
                        }
                    }
                }

                foreach (var profitProviderFetch in profitProviderFetches)
                {
                    var profitProvider = PoolProfitProviderFactory.GetPoolProfitProvider(profitProviderFetch.Key);
                    var profits = profitProvider.GetProfits(profitProviderFetch.Value, Config.EnableCaching, AppFolder, ct);
                    foreach (var profit in profits)
                    {
                        poolProfitData[profit.Key] = profit.Value;
                    }
                }

                _poolProfitData = poolProfitData;
            }
            catch (Exception e)
            {
                Log.Debug("Fetching profit data failed: " + e);
            }
        }

        private static Task MiningSwitcherTaskAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                while (!_requestQuit)
                {
                    try
                    {
                        _miningSwitcherTaskCts = new CancellationTokenSource();
                        CancellationToken ct = _miningSwitcherTaskCts.Token;
                        CheckSwitching();
                        Task.Delay(TimeSpan.FromSeconds(Config.ProfitSwitchInterval), ct).Wait(ct);
                    }
                    catch (Exception e)
                    {
                        Log.Debug("Check switching cancelled:" + e);
                    }
                }

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private static void CheckSwitching()
        {
            try
            {
                WriteInfo(" Check switching..");
                IProfitSwitchingStrategy profitSwitchingStrategy = ProfitSwitchingStrategyFactory.GetProfitSwitchingStrategy(_profitSwitchingStrategy ?? ProfitSwitchingStrategy.MaximizeFiat);
                // Find optimal configs
                Dictionary<string, MiningConfig> optimalMiningConfigs = new Dictionary<string, MiningConfig>();
                if (_manualMode)
                {
                    foreach (var manualMininigConfig in _manualMininigConfigs)
                    {
                        MiningConfig miningConfig = new MiningConfig(manualMininigConfig.Value.DeviceConfig, manualMininigConfig.Value.Pool);
                        optimalMiningConfigs.Add(manualMininigConfig.Key, miningConfig);
                    }
                }
                else
                {
                    foreach (Algorithm algorithm in Config.Algorithms)
                    {
                        if (algorithm.Enabled)
                        {
                            foreach (DeviceConfig algorithmDeviceConfig in algorithm.DeviceConfigs)
                            {
                                if (algorithmDeviceConfig.Enabled)
                                {
                                    foreach (Pool algorithmPool in algorithm.Pools.Where(p => p.Enabled))
                                    {
                                        Profit? profit = GetAdjustedProfit(algorithmPool, algorithmDeviceConfig.ExpectedHashrate, true);
                                        if (profit.HasValue)
                                        {
                                            if (!optimalMiningConfigs.ContainsKey(algorithmDeviceConfig.FullDeviceId))
                                            {
                                                SetOptimalMiningConfigWithThreshold(profit.Value, profitSwitchingStrategy, algorithmDeviceConfig, algorithmPool, optimalMiningConfigs);
                                            }
                                            else
                                            {
                                                MiningConfig bestMiningConfig = optimalMiningConfigs[algorithmDeviceConfig.FullDeviceId];
                                                Profit? bestProfit = GetAdjustedProfit(bestMiningConfig.Pool, bestMiningConfig.DeviceConfig.ExpectedHashrate, true);
                                                if (!bestProfit.HasValue || profitSwitchingStrategy.IsProfitABetterThanB(profit.Value, algorithmPool.ProfitTimeframe, bestProfit.Value, bestMiningConfig.Pool.ProfitTimeframe, 0))
                                                {
                                                    SetOptimalMiningConfigWithThreshold(profit.Value, profitSwitchingStrategy, algorithmDeviceConfig, algorithmPool, optimalMiningConfigs);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Merge miners and get list of optimal miners
                List<IMiner> optimalMiners = new List<IMiner>();
                foreach (var optimalMiningConfigKeyValue in optimalMiningConfigs)
                {
                    MiningConfig miningConfig = optimalMiningConfigKeyValue.Value;
                    IMiner miner = MinerFactory.GetMiner(new HashSet<DeviceConfig>() { miningConfig.DeviceConfig }, miningConfig.Pool);
                    IMiner existingMiner = optimalMiners.FirstOrDefault(m => m.Name == miner.Name && m.Pool.Equals(miner.Pool));
                    if (existingMiner != null)
                    {
                        existingMiner.DeviceConfigs.Add(miningConfig.DeviceConfig);
                        miningConfig.Miner = existingMiner;
                    }
                    else
                    {
                        miningConfig.Miner = miner;
                        optimalMiners.Add(miner);
                    }
                }

                bool displayNeedsUpdate = false;
                // Get list of current miners
                List<IMiner> currentMiners = GetCurrentMiners();

                //Check if existing miner will be kept
                foreach (IMiner currentMiner in currentMiners)
                {
                    if (optimalMiners.Any(om => om.Pool.Equals(currentMiner.Pool) && om.DeviceConfigs.SetEquals(currentMiner.DeviceConfigs)))
                    {
                        foreach (DeviceConfig currentMinerDeviceConfig in currentMiner.DeviceConfigs)
                        {
                            optimalMiningConfigs[currentMinerDeviceConfig.FullDeviceId].Miner = currentMiner;
                        }
                    }
                }

                //Check if existing miner has to be closed
                foreach (IMiner currentMiner in currentMiners.ToArray())
                {
                    if (!optimalMiners.Any(om => om.Pool.Equals(currentMiner.Pool) && om.DeviceConfigs.SetEquals(currentMiner.DeviceConfigs)))
                    {
                        displayNeedsUpdate = true;
                        currentMiner.StopMiner();
                        currentMiners.Remove(currentMiner);
                    }
                }

                //Check if new miner has to start
                foreach (IMiner optimalMiner in optimalMiners)
                {
                    if (!currentMiners.Any(cm => cm.Pool.Equals(optimalMiner.Pool) && cm.DeviceConfigs.SetEquals(optimalMiner.DeviceConfigs)))
                    {
                        displayNeedsUpdate = true;
                        foreach (DeviceConfig deviceConfig in optimalMiner.DeviceConfigs)
                        {
                            if (!string.IsNullOrEmpty(deviceConfig.PrepareScript))
                            {
                                Helpers.ExecuteScript(deviceConfig.MinerPath, AppFolderPath);
                            }
                        }
                        Task.Delay(TimeSpan.FromSeconds(Config.MinerStartDelay)).Wait();
                        optimalMiner.StartMiner(Config.StartMinerMinimized);
                    }
                }

                _currentMininigConfigs = optimalMiningConfigs;

                if (displayNeedsUpdate)
                {
                    _displayUpdaterCts?.Cancel();
                }
            }
            catch (Exception e)
            {
                Log.Debug("Check switching failed: " + e);
            }
        }

        private static void SetOptimalMiningConfigWithThreshold(Profit profit, IProfitSwitchingStrategy profitSwitchingStrategy, DeviceConfig algorithmDeviceConfig, Pool algorithmPool, Dictionary<string, MiningConfig> optimalMiningConfigs)
        {
            MiningConfig newMiningConfig = new MiningConfig(algorithmDeviceConfig, algorithmPool);
            if (_currentMininigConfigs.ContainsKey(algorithmDeviceConfig.FullDeviceId))
            {
                MiningConfig currentMiningConfig = _currentMininigConfigs[algorithmDeviceConfig.FullDeviceId];
                Profit? currentProfit = GetAdjustedProfit(currentMiningConfig.Pool, currentMiningConfig.DeviceConfig.ExpectedHashrate, true);
                if (!currentProfit.HasValue || profitSwitchingStrategy.IsProfitABetterThanB(profit, algorithmPool.ProfitTimeframe, currentProfit.Value, currentMiningConfig.Pool.ProfitTimeframe, Config.ProfitSwitchThreshold))
                {
                    optimalMiningConfigs[newMiningConfig.DeviceConfig.FullDeviceId] = newMiningConfig;
                }
                else
                {
                    optimalMiningConfigs[algorithmDeviceConfig.FullDeviceId] = new MiningConfig(currentMiningConfig.DeviceConfig, currentMiningConfig.Pool);
                }
            }
            else
            {
                optimalMiningConfigs[newMiningConfig.DeviceConfig.FullDeviceId] = newMiningConfig;
            }
        }

        private static Task KeyPressesTaskAsync(CancellationToken ct)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        Task.Delay(3000, ct).Wait(ct);
                        while (Console.CursorVisible)
                        {
                            Task.Delay(1000, ct).Wait(ct);
                        }
                        ConsoleKeyInfo readKey = Console.ReadKey(true);
                        if (!Console.CursorVisible)
                        {
                            switch (readKey.Key)
                            {
                                case ConsoleKey.A:
                                    if (_manualMode)
                                    {
                                        WriteInfo(" Disabling manual mode..");
                                        _manualMode = false;
                                        _manualMininigConfigs = null;
                                        _miningSwitcherTaskCts?.Cancel();
                                    }
                                    else
                                    {
                                        WriteInfo(" Reconfigure profit switching strategy due to key press..");
                                        _profitSwitchingStrategy = null;
                                        _displayUpdaterCts?.Cancel();
                                    }
                                    break;
                                case ConsoleKey.U:
                                    WriteInfo(" Updating display due to key press..");
                                    _displayUpdaterCts?.Cancel();
                                    break;
                                case ConsoleKey.F:
                                    WriteInfo(" Fetching profits due to key press..");
                                    _profitFetcherTaskCts?.Cancel();
                                    break;
                                case ConsoleKey.C:
                                    WriteInfo(" Check switching due to key press..");
                                    _miningSwitcherTaskCts?.Cancel();
                                    break;
                                case ConsoleKey.M:
                                    if (_manualMode)
                                    {
                                        WriteInfo(" Reconfigure manual mode due to key press..");
                                        _manualMininigConfigs = null;
                                        _displayUpdaterCts?.Cancel();
                                    }
                                    else
                                    {
                                        WriteInfo(" Activating manual mode..");
                                        _manualMininigConfigs = null;
                                        _manualMode = true;
                                        _displayUpdaterCts?.Cancel();
                                    }
                                    break;
                                case ConsoleKey.R:
                                    WriteInfo(" Reloading config due to key press..");
                                    InitConfig();
                                    break;
                                case ConsoleKey.Q:
                                    WriteInfo(" Quitting due to key press..");
                                    Quit();
                                    break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Debug("Key presses task failed:" + e);
                    throw;
                }
            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private static Task DisplayUpdaterTaskAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                while (!_requestQuit)
                {
                    try
                    {
                        Thread.CurrentThread.Priority = ThreadPriority.Highest;
                        _displayUpdaterCts = new CancellationTokenSource();
                        CancellationToken ct = _displayUpdaterCts.Token;
                        UpdateDisplay();
                        Task.Delay(TimeSpan.FromSeconds(Config.DisplayUpdateInterval), ct).Wait(ct);
                    }
                    catch (Exception e)
                    {
                        Log.Debug("Update display cancelled:" + e);
                    }
                    finally
                    {
                        Thread.CurrentThread.Priority = ThreadPriority.Normal;
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        private static void UpdateDisplay()
        {
            try
            {
                ResetConsole();
                if (_poolProfitData != null)
                {
                    List<Pool> printedPools = PrintProfits();
                    if (_manualMode && _manualMininigConfigs == null)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        PrintManualModeConfig(printedPools);
                    }
                }
                if (!_manualMode && _profitSwitchingStrategy == null)
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    PrintAutoModeConfig();
                }
                Console.WriteLine();
                Console.WriteLine();
                PrintMiningStatus();
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Log.Debug("Update display failed: " + e);
            }
        }

        private static List<Pool> PrintProfits()
        {
            var printedPools = new List<Pool>();
            WriteLineColor(" EXPECTED PROFIT FOR POOLS: ", ConsoleColor.Cyan);
            var headerThickness = new LineThickness(LineWidth.Double, LineWidth.Single);
            var poolGrid = new Grid
            {
                Color = ConsoleColor.White,
                Margin = new Thickness(1, 0, 0, 0),
                Columns = { GridLength.Auto },
                Children =
                {
                        new Cell("Pool") { Stroke = headerThickness, Color = ConsoleColor.Magenta, Padding = new Thickness(2,0,10,0) },
                },
            };

            HashSet<string> deviceIds = GetActiveDeviceIds();

            foreach (var deviceId in deviceIds)
            {
                poolGrid.Columns.Add(GridLength.Auto);
                poolGrid.Children.Add(new Cell(deviceId) { Stroke = headerThickness, Color = ConsoleColor.Magenta, Padding = new Thickness(2, 0, 2, 0) });
            }

            List<DevicePoolProfitData> devicePoolProfitDatas = new List<DevicePoolProfitData>();
            foreach (var poolProfit in _poolProfitData)
            {
                DevicePoolProfitData devicePoolProfitData = new DevicePoolProfitData(poolProfit.Key);
                foreach (string deviceId in deviceIds)
                {
                    double expectedHashrate = 0;
                    DeviceConfig deviceConfig = GetDeviceConfigForPool(poolProfit.Key, deviceId);
                    if (deviceConfig != null)
                    {
                        expectedHashrate = deviceConfig.ExpectedHashrate;
                    }

                    Profit? profit = GetAdjustedProfit(poolProfit.Key, expectedHashrate, false);
                    devicePoolProfitData.DeviceProfits[deviceId] = profit;
                }
                devicePoolProfitDatas.Add(devicePoolProfitData);
            }

            Dictionary<string, double> maxProfits = new Dictionary<string, double>();
            foreach (string deviceId in deviceIds)
            {
                maxProfits[deviceId] = devicePoolProfitDatas.Max(dppds => dppds.DeviceProfits[deviceId]?.GetMostCurrentUsdReward() ?? 0);
            }

            int poolIndex = 0;
            foreach (DevicePoolProfitData devicePoolProfitData in devicePoolProfitDatas)
            {
                poolIndex++;
                printedPools.Add(devicePoolProfitData.Pool);
                string displayName = _manualMode && _manualMininigConfigs == null ? $"[{poolIndex}] " + devicePoolProfitData.Pool.UniqueName : devicePoolProfitData.Pool.UniqueName;
                poolGrid.Children.Add(new Cell(displayName) { Color = ConsoleColor.Yellow, Padding = new Thickness(2, 0, 10, 0) });
                foreach (string deviceId in deviceIds)
                {
                    double maxProfit = maxProfits[deviceId];
                    Profit? profit = devicePoolProfitData.DeviceProfits[deviceId];
                    if (profit != null)
                    {
                        ConsoleColor color = profit.Value.GetMostCurrentUsdReward() < maxProfit ? ConsoleColor.Gray : ConsoleColor.Green;
                        poolGrid.Children.Add(!profit.Value.HasValues()
                            ? new Cell("No data") { Color = ConsoleColor.DarkGray, Align = Align.Center, Padding = new Thickness(2, 0, 2, 0) }
                            : new Cell(profit) { Color = color, Align = Align.Center, Padding = new Thickness(2, 0, 2, 0) });
                    }
                }
            }

            var poolDoc = new Document(poolGrid);
            ConsoleRenderer.RenderDocument(poolDoc);
            return printedPools;
        }

        private static void PrintManualModeConfig(List<Pool> printedPools)
        {
            var manualMininigConfigs = new Dictionary<string, MiningConfig>();
            WriteLineColor(" MANUAL MODE CONFIG: ", ConsoleColor.Cyan);
            WriteLineColor(" Use the numbers above to set a pool.", ConsoleColor.DarkGray);
            WriteLineColor(" Or use \"0\" to disable the device.", ConsoleColor.DarkGray);
            Console.WriteLine();
            Console.CursorVisible = true;
            foreach (string deviceId in GetActiveDeviceIds())
            {
                DeviceConfig deviceConfig = null;
                Pool selectedPool = null;
                bool noPool = false;
                while ((selectedPool == null || deviceConfig == null) && !noPool)
                {
                    Console.Write(" Set the pool for ");
                    WriteColor(deviceId, ConsoleColor.Yellow);
                    Console.Write(": ");

                    string userInput = Console.ReadLine();
                    if (int.TryParse(userInput, out int poolUserIndex))
                    {
                        if (poolUserIndex == 0)
                        {
                            noPool = true;
                        }
                        else if (poolUserIndex > 0 && poolUserIndex <= printedPools.Count)
                        {
                            selectedPool = printedPools[poolUserIndex - 1];
                            deviceConfig = GetDeviceConfigForPool(selectedPool, deviceId);
                            if (deviceConfig == null)
                            {
                                Console.WriteLine(" The selected pool is not valid for this device.");
                            }
                        }
                    }
                }

                if (!noPool)
                {
                    manualMininigConfigs[deviceConfig.FullDeviceId] = new MiningConfig(deviceConfig, selectedPool);
                }
            }
            Console.CursorVisible = false;

            _manualMininigConfigs = manualMininigConfigs;
            _miningSwitcherTaskCts?.Cancel();
            Console.WriteLine();
        }

        private static void PrintAutoModeConfig()
        {
            Console.CursorVisible = true;
            WriteLineColor(" AUTO MODE CONFIG: ", ConsoleColor.Cyan);
            WriteLineColor(" Use the numbers below to set the profit switching strategy.", ConsoleColor.DarkGray);
            Console.WriteLine();
            var strategies = Enum.GetValues(typeof(ProfitSwitchingStrategy)).Cast<ProfitSwitchingStrategy>().ToList();
            for (var index = 0; index < strategies.Count; index++)
            {
                ProfitSwitchingStrategy profitSwitchingStrategy = strategies[index];
                Console.Write(" [");
                WriteColor((index + 1).ToString(CultureInfo.InvariantCulture), ConsoleColor.Yellow);
                Console.WriteLine($"] {profitSwitchingStrategy.ToString()}");
            }
            Console.WriteLine();

            while (!_profitSwitchingStrategy.HasValue)
            {
                Console.Write(" Type the number: ");
                string userInput = Console.ReadLine();
                if (int.TryParse(userInput, out int userIndex))
                {
                    if (userIndex > 0 && userIndex <= strategies.Count)
                    {
                        _profitSwitchingStrategy = strategies[userIndex - 1];
                    }
                }
            }
            Console.CursorVisible = false;
            Console.WriteLine();
            _miningSwitcherTaskCts?.Cancel();
        }

        private static void PrintMiningStatus()
        {
            const int extraPadding = 3;
            const string total = "Total";

            WriteLineColor(" CURRENT STATUS: ", ConsoleColor.Cyan);
            Console.WriteLine();
            if (_currentMininigConfigs.Count > 0)
            {
                List<IMiner> currentMiners = GetCurrentMiners();
                var deviceStatuses = new List<DeviceStatus>();

                foreach (var currentMiner in currentMiners)
                {
                    if (currentMiner.SupportsIndividualHashrate || currentMiner.DeviceConfigs.Count == 1)
                    {
                        foreach (DeviceConfig deviceConfig in currentMiner.DeviceConfigs)
                        {
                            double currentHashrate = currentMiner.GetCurrentHashrate(deviceConfig);
                            Profit? profit = GetAdjustedProfit(currentMiner.Pool, currentHashrate, false);
                            if (profit.HasValue)
                            {
                                deviceStatuses.Add(new DeviceStatus(deviceConfig.FullDeviceId, currentMiner.Pool, currentHashrate, profit.Value));
                            }
                        }
                    }
                    else
                    {
                        double currentHashrate = currentMiner.GetCurrentHashrate(null);
                        Profit? profit = GetAdjustedProfit(currentMiner.Pool, currentHashrate, false);
                        string combinedDeviceNames = string.Join(" | ", currentMiner.DeviceConfigs.Select(dc => dc.FullDeviceId));
                        if (profit.HasValue)
                        {
                            deviceStatuses.Add(new DeviceStatus(combinedDeviceNames, currentMiner.Pool, currentHashrate, profit.Value));
                        }
                    }
                }

                int maxDeviceNameLength = Math.Max(deviceStatuses.Max(ds => ds.Name.Length), total.Length);

                PrintMode(maxDeviceNameLength + extraPadding);

                foreach (DeviceStatus deviceStatus in deviceStatuses)
                {
                    Console.WriteLine();
                    string leftText = $" {deviceStatus.Name}:".PadRight(maxDeviceNameLength + extraPadding);
                    WriteColor(leftText, ConsoleColor.Magenta);
                    Console.Write(" Mining on ");
                    WriteColor(deviceStatus.Pool.UniqueName, ConsoleColor.Yellow);
                    Console.Write(" at ");
                    WriteColor(deviceStatus.Hashrate.ToHashrate(), ConsoleColor.Yellow);
                    double usdProfit = deviceStatus.Profit.GetMostCurrentUsdReward();
                    if (usdProfit > 0)
                    {
                        Console.Write(" (");
                        WriteColor(usdProfit.ToCurrency("$"), ConsoleColor.Yellow);
                        Console.Write(" per day)");
                    }
                }
                Console.WriteLine();
                Console.WriteLine();
                WriteColor($" {total}:".PadRight(maxDeviceNameLength + extraPadding), ConsoleColor.Magenta);
                Console.Write(" You are currently earning ");
                double totalUsdProfit = deviceStatuses.Sum(ds => ds.Profit.GetMostCurrentUsdReward());
                WriteColor(totalUsdProfit.ToCurrency("$"), ConsoleColor.Green);
                Console.Write(" per day.");
                Console.WriteLine();
            }
            else
            {
                PrintMode(4 + extraPadding);
                Console.WriteLine();
                WriteLineColor(" Not mining.", ConsoleColor.Yellow);
            }
        }

        private static void PrintMode(int leftWidth)
        {
            Console.Write(" Mode:".PadRight(leftWidth));
            if (_manualMode)
            {
                Console.Write(" Manual -> switch pools using the key '");
                WriteColor("m", ConsoleColor.Yellow);
                Console.WriteLine("'");
            }
            else
            {
                Console.Write(" Auto -> profit switching with ");
                WriteColor(_profitSwitchingStrategy.Value.ToString(), ConsoleColor.Yellow);
                Console.Write(" strategy (press '");
                WriteColor("a", ConsoleColor.Yellow);
                Console.WriteLine("' to change strategy)");
            }
        }

        private static void ResetConsole()
        {
            Console.CursorVisible = false;
            Console.Clear();
            string updateText = _newVersionAvailable ? " (NEW UPDATE AVAILABLE!)" : String.Empty;
            WriteLineColor($" CRYPTO PROFIT SWITCHER | VERSION: {Helpers.GetApplicationVersion()}{updateText}", ConsoleColor.Cyan);
            WriteLineColor(" https://github.com/cryptoprofitswitcher/CryptoProfitSwitcher", ConsoleColor.DarkGray);

            if (Config != null)
            {
                Console.Write(" Config:  DisplayUpdateInterval ");
                WriteColor(Config.DisplayUpdateInterval.ToString(CultureInfo.InvariantCulture), ConsoleColor.Yellow);
                Console.Write("s | ProfitCheckInterval ");
                WriteColor(Config.ProfitCheckInterval.ToString(CultureInfo.InvariantCulture), ConsoleColor.Yellow);
                Console.Write("s | SwitchInterval ");
                WriteColor(Config.ProfitSwitchInterval.ToString(CultureInfo.InvariantCulture), ConsoleColor.Yellow);
                Console.Write("s | SwitchThreshold ");
                WriteLineColor(Config.ProfitSwitchThreshold.ToString(CultureInfo.InvariantCulture), ConsoleColor.Yellow);
            }
            Console.Write(" Hotkeys: ");
            WriteColor("u", ConsoleColor.Yellow);
            Console.Write(" - update display, ");
            WriteColor("f", ConsoleColor.Yellow);
            Console.Write(" - fetch profits, ");
            WriteColor("c", ConsoleColor.Yellow);
            Console.Write(" - check switch, ");
            if (_manualMode)
            {
                WriteColor("a", ConsoleColor.Yellow);
                Console.Write(" - auto mode, ");
            }
            else
            {
                WriteColor("m", ConsoleColor.Yellow);
                Console.Write(" - manual mode, ");
            }
            WriteColor("r", ConsoleColor.Yellow);
            Console.Write(" - reload config, ");
            WriteColor("q", ConsoleColor.Yellow);
            Console.WriteLine(" - quit");
            Console.WriteLine();
        }

        private static void BenchmarkIfNeeded()
        {
            if (!Config.DisableBenchmarking)
            {
                WriteInfo(" Checking if benchmarking is needed..");
                List<MiningConfig> configsToTest = new List<MiningConfig>();
                foreach (Algorithm algorithm in Config.Algorithms)
                {
                    if (algorithm.Enabled)
                    {
                        foreach (DeviceConfig deviceConfig in algorithm.DeviceConfigs)
                        {
                            if (deviceConfig.Enabled && deviceConfig.ExpectedHashrate <= 0)
                            {
                                configsToTest.Add(new MiningConfig(deviceConfig, algorithm.Pools.First(p => p.Enabled)));
                            }
                        }
                    }
                }

                if (configsToTest.Count == 0)
                {
                    WriteInfo(" Benchmarking not needed: All algorithm-device-combinations have a valid expected hashrate.");
                }
                else
                {
                    WriteInfo(" Benchmarking needed: Not all algorithm-device-combinations have a valid expected hashrate.");
                    Console.WriteLine();
                    WriteLineColor(" BENCHMARKING:", ConsoleColor.Cyan);
                    foreach (MiningConfig miningConfig in configsToTest)
                    {
                        Console.WriteLine();
                        Console.Write(" Benchmarking ");
                        WriteColor(miningConfig.DeviceConfig.FullDeviceId, ConsoleColor.Yellow);
                        Console.Write(" on ");
                        WriteColor(miningConfig.Pool.UniqueName, ConsoleColor.Yellow);
                        Console.WriteLine("..");
                        IMiner miner = MinerFactory.GetMiner(new HashSet<DeviceConfig>() { miningConfig.DeviceConfig }, miningConfig.Pool);
                        miner.StartMiner(Config.StartMinerMinimized);
                        int userInputHashrate = 0;
                        while (userInputHashrate <= 0)
                        {
                            Console.Write(" Please enter the hashrate: ");
                            Console.CursorVisible = true;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            string input = Console.ReadLine();
                            Console.ResetColor();
                            int.TryParse(input, out userInputHashrate);
                        }
                        Console.CursorVisible = false;
                        miningConfig.DeviceConfig.ExpectedHashrate = userInputHashrate;
                        SaveConfig();
                        miner.StopMiner();
                    }
                }
            }
        }

        private static void Quit()
        {
            _requestQuit = true;
            _profitFetcherTaskCts?.Cancel();
            _miningSwitcherTaskCts?.Cancel();
            _displayUpdaterCts?.Cancel();
            _keyPressesTaskCts?.Cancel();
            StopMiners();
        }

        private static void StopMiners()
        {
            if (_currentMininigConfigs != null)
            {
                foreach (var currentMininigConfig in _currentMininigConfigs)
                {
                    currentMininigConfig.Value.Miner?.StopMiner();
                }
            }

            _currentMininigConfigs = new Dictionary<string, MiningConfig>();
        }

        private static void WriteLineColor(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        private static void WriteColor(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        private static List<IMiner> GetCurrentMiners()
        {
            var currentMiners = new List<IMiner>();
            foreach (var currentMininigConfigKeyValue in _currentMininigConfigs)
            {
                MiningConfig miningConfig = currentMininigConfigKeyValue.Value;
                if (!currentMiners.Contains(miningConfig.Miner))
                {
                    currentMiners.Add(miningConfig.Miner);
                }
            }

            return currentMiners;
        }

        private static HashSet<string> GetActiveDeviceIds()
        {
            var deviceIds = new HashSet<string>();
            foreach (Algorithm algorithm in Config.Algorithms)
            {
                if (algorithm.Enabled)
                {
                    foreach (DeviceConfig deviceConfig in algorithm.DeviceConfigs)
                    {
                        if (!deviceIds.Contains(deviceConfig.FullDeviceId))
                        {
                            deviceIds.Add(deviceConfig.FullDeviceId);
                        }
                    }
                }
            }

            return deviceIds;
        }

        private static DeviceConfig GetDeviceConfigForPool(Pool pool, string deviceId)
        {
            foreach (Algorithm algorithm in Config.Algorithms)
            {
                if (algorithm.Enabled)
                {
                    if (algorithm.Pools.Contains(pool))
                    {
                        return algorithm.DeviceConfigs.FirstOrDefault(dc => dc.FullDeviceId == deviceId);
                    }
                }
            }

            return null;
        }

        private static Profit? GetAdjustedProfit(Pool pool, double expectedHashrate, bool withPreferFactor)
        {
            if (_poolProfitData != null && _poolProfitData.ContainsKey(pool))
            {
                Profit p = _poolProfitData[pool];
                double hf = expectedHashrate / Profit.BaseHashrate;
                double pf = withPreferFactor ? pool.PreferFactor : 1;
                return new Profit(
                    p.UsdRewardLive * pf * hf,
                    p.UsdRewardDay * pf * hf,
                    p.CoinRewardLive * pf * hf,
                    p.CoinRewardDay * pf * hf, 
                    p.Source);
            }
            return null;
        }

        private static void WriteInfo(string text)
        {
            if (!Console.CursorVisible)
            {
                Console.WriteLine(text);
            }
        }
    }
}
