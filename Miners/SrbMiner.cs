using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CryptonightProfitSwitcher.Miners
{
    public class SrbMiner : IMiner
    {
        private int _customPort;
        private Process _process;
        private Mineable _mineable;
        private IMiner _cpuMiner;
        public string Name => "SRB Miner";

        public double GetCurrentHashrate(Settings settings, DirectoryInfo appRootFolder)
        {
            double gpuHashrate = 0;
            try
            {
                int port = _customPort > 0 ? _customPort : _mineable.SRBMinerApiPort;
                var json = Helpers.GetJsonFromUrl($"http://127.0.0.1:{port}", settings, appRootFolder, CancellationToken.None);
                dynamic api = JObject.Parse(json);
                gpuHashrate = api.hashrate_total_now;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't get current hashrate: " + ex.Message);
            }

            double cpuHashrate = 0;
            if (_cpuMiner != null)
            {
                cpuHashrate = _cpuMiner.GetCurrentHashrate(settings, appRootFolder);
            }

            return gpuHashrate + cpuHashrate;
        }

        public void StartMiner(Mineable mineable, Settings settings, string appRoot, DirectoryInfo appRootFolder)
        {
            _mineable = mineable;
            _process = new Process();
            string minerPath = Helpers.ResolveToFullPath(mineable.SRBMinerPath, appRoot);
            string minerFolderPath = Path.GetDirectoryName(minerPath);
            var minerDirectory = new DirectoryInfo(minerFolderPath);
            _process.StartInfo.FileName = minerPath;

            List<string> userDefindedArgs = new List<string>();
            if (!String.IsNullOrEmpty(mineable.SRBMinerExtraArguments))
            {
                userDefindedArgs.AddRange(mineable.SRBMinerExtraArguments.Split(" "));
            }

            string args = "";
            string space = "";

            if (!userDefindedArgs.Contains("--config"))
            {
                string configPath = Helpers.ResolveToFullPath(mineable.SRBMinerConfigPath, appRoot);
                File.Copy(configPath, Path.Combine(minerFolderPath, "current_config.txt"), true);
                args = $"{space}--config current_config.txt";
                space = " ";
            }

            if (!userDefindedArgs.Contains("--pools"))
            {
                if (!String.IsNullOrEmpty(mineable.SRBMinerPoolsPath))
                {
                    // Use given pool config
                    string poolPath = Helpers.ResolveToFullPath(mineable.SRBMinerPoolsPath, appRoot);
                    File.Copy(poolPath, Path.Combine(minerFolderPath, "current_pool.txt"), true);
                }
                else
                {
                    // Auto-Generate pool config from Mineable
                    string poolConfigJson = GeneratePoolConfigJson(mineable);
                    File.WriteAllText(Path.Combine(minerFolderPath, "current_pool.txt"), poolConfigJson);
                }
                args += $"{space}--pools current_pool.txt";
                space = " ";
            }

            if (!userDefindedArgs.Contains("--apienable"))
            {
                args += $"{space}--apienable";
                space = " ";
            }

            int portIndex = userDefindedArgs.IndexOf("--remoteport");
            if (portIndex == -1)
            {
                if (mineable.SRBMinerApiPort > 0)
                {
                    _customPort = mineable.SRBMinerApiPort;
                    args += $"{space}--apiport {_customPort}";
                    space = " ";
                }
            }
            else
            {
                _customPort = Int32.Parse(userDefindedArgs[portIndex + 1]);
            }

            if (!String.IsNullOrEmpty(mineable.SRBMinerExtraArguments))
            {
                args += space + mineable.SRBMinerExtraArguments;
            }

            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = true;
            _process.StartInfo.CreateNoWindow = false;
            _process.StartInfo.RedirectStandardOutput = false;
            _process.StartInfo.WorkingDirectory = minerFolderPath;
            _process.StartInfo.WindowStyle = settings.StartMinerMinimized ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal;

            Thread.Sleep(TimeSpan.FromSeconds(settings.MinerStartDelay));
            _process.Start();

            if (mineable.SRBMinerUseXmrStakCPUMining)
            {
                _cpuMiner = new XmrStakMiner(true);
                _cpuMiner.StartMiner(mineable, settings, appRoot, appRootFolder);
            }
        }

        public void StopMiner()
        {
            if (_process != null)
            {
                try
                {
                    _process.CloseMainWindow();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't close miner process: " + ex.Message);
                }
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't kill miner process: " + ex.Message);
                }
                _process.Dispose();
                _process = null;
                _mineable = null;
            }
            if (_cpuMiner != null)
            {
                _cpuMiner.StopMiner();
                _cpuMiner = null;
            }
        }

        private static string GeneratePoolConfigJson(Mineable mineable)
        {
            var dict = new Dictionary<string, object>();
            dict["pools"] = new Dictionary<string, object>[]
            {
                new Dictionary<string, object>
                {
                    { "pool", mineable.PoolAddress },
                    { "wallet", mineable.PoolWalletAddress },
                    { "password", mineable.PoolPassword },
                    { "nicehash", mineable is NicehashAlgorithm },
                    { "pool_use_tls", mineable.PoolUseTls },
                    { "worker", mineable.PoolRigId },
                }
            };

            return JsonConvert.SerializeObject(dict, Formatting.Indented);
        }
    }
}
