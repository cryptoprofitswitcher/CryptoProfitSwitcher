using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace CryptonightProfitSwitcher.Miners
{
    public class XmrStakMiner : IMiner
    {
        Process _process = null;
        Mineable _mineable = null;
        bool _cpuOnly = false;
        public XmrStakMiner(bool cpuOnly)
        {
            _cpuOnly = cpuOnly;
        }

        public double GetCurrentHashrate(Settings settings, DirectoryInfo appRootFolder)
        {
            try
            {
                int port = _mineable.XmrStakApiPort;
                //Backwards compatibility
                if (port == 0 && settings.XmrStakApiPort != 0)
                {
                    port = settings.XmrStakApiPort;
                }
                var xmrJson = Helpers.GetJsonFromUrl($"http://127.0.0.1:{port}/api.json", settings, appRootFolder);
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

        public void StartMiner(Mineable mineable, Settings settings, string appRoot, DirectoryInfo appRootFolder)
        {
            _mineable = mineable;
            _process = new Process();
            string xmrPath = Helpers.ResolveToFullPath(mineable.XmrStakPath, appRoot);
            string xmrFolderPath = Path.GetDirectoryName(xmrPath);
            var xmrDirectory = new DirectoryInfo(xmrFolderPath);
            _process.StartInfo.FileName = xmrPath;
            string configPath = Helpers.ResolveToFullPath(mineable.ConfigPath, appRoot);
            File.Copy(configPath, Path.Combine(xmrFolderPath, "current_config.txt"), true);
            string args = "-c current_config.txt";

            if (!String.IsNullOrEmpty(mineable.PoolsPath))
            {
                // Use given pool config
                string poolPath = Helpers.ResolveToFullPath(mineable.PoolsPath, appRoot);
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
                string cpuPath = Helpers.ResolveToFullPath(mineable.CpuPath, appRoot);
                File.Copy(cpuPath, Path.Combine(xmrFolderPath, "current_cpu.txt"), true);
                args += " --cpu current_cpu.txt";
            }

            if (String.IsNullOrEmpty(mineable.AmdPath) || _cpuOnly)
            {
                args += " --noAMD";
            }
            else
            {
                string amdPath = Helpers.ResolveToFullPath(mineable.AmdPath, appRoot);
                File.Copy(amdPath, Path.Combine(xmrFolderPath, "current_amd.txt"), true);
                args += " --amd current_amd.txt";
            }

            if (String.IsNullOrEmpty(mineable.NvidiaPath) || _cpuOnly)
            {
                args += " --noNVIDIA";
            }
            else
            {
                string nvidiaPath = Helpers.ResolveToFullPath(mineable.NvidiaPath, appRoot);
                File.Copy(nvidiaPath, Path.Combine(xmrFolderPath, "current_nvidia.txt"), true);
                args += " --nvidia current_nvidia.txt";
            }

            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = true;
            _process.StartInfo.CreateNoWindow = false;
            _process.StartInfo.RedirectStandardOutput = false;
            _process.StartInfo.WorkingDirectory = xmrFolderPath;
            _process.StartInfo.WindowStyle = settings.StartMinerMinimized ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal;
            Thread.Sleep(TimeSpan.FromSeconds(settings.MinerStartDelay));
            _process.Start();
        }

        public void StopMiner()
        {
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
                _mineable = null;
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
                case Algorithm.CryptonightStellite:
                    dict["currency"] = "stellite";
                    break;
                case Algorithm.CryptonightHaven:
                    dict["currency"] = "haven";
                    break;
                case Algorithm.CryptonightMasari:
                    dict["currency"] = "masari";
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
    }
}
