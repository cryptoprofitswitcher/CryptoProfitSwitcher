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
        private Process _process;
        private Mineable _mineable;
        private bool _cpuOnly;
        public string Name => "XmrStak";

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
#pragma warning disable CS0618
                if (port == 0 && settings.XmrStakApiPort != 0)
                {
                    port = settings.XmrStakApiPort;
                }
#pragma warning restore CS0618
                var xmrJson = Helpers.GetJsonFromUrl($"http://127.0.0.1:{port}/api.json", settings, appRootFolder, CancellationToken.None);
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
            _process.StartInfo.FileName = "cmd";
            if (Helpers.IsLinux())
            {
                _process.StartInfo.FileName = "x-terminal-emulator";
            }
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

            _process.StartInfo.Arguments = $"/c \"{"set GPU_FORCE_64BIT_PTR=1 & set GPU_MAX_HEAP_SIZE=100 & set GPU_MAX_ALLOC_PERCENT=100 & set GPU_SINGLE_ALLOC_PERCENT=100 & " + Path.GetFileName(xmrPath)} {args}\"";

            if (Helpers.IsLinux())
            {
                _process.StartInfo.Arguments = $"-e \"'{xmrPath}' {args}\"";
            }

            _process.StartInfo.UseShellExecute = true;
            _process.StartInfo.CreateNoWindow = false;
            _process.StartInfo.RedirectStandardOutput = false;
            _process.StartInfo.WorkingDirectory = xmrFolderPath;
            _process.StartInfo.WindowStyle = settings.StartMinerMinimized ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal;
            _process.EnableRaisingEvents = true;
            Thread.Sleep(TimeSpan.FromSeconds(settings.MinerStartDelay));
            _process.Start();
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

                if (Helpers.IsLinux())
                {
                    try
                    {
                        var killProcess = new Process();
                        killProcess.StartInfo.FileName = "x-terminal-emulator";
                        string xmrName = Path.GetFileName(_mineable.XmrStakPath);
                        Console.WriteLine("Kill: " + xmrName);
                        killProcess.StartInfo.Arguments = $"-e \"killall -9 {xmrName}\"";
                        killProcess.StartInfo.UseShellExecute = true;
                        killProcess.StartInfo.CreateNoWindow = false;
                        killProcess.StartInfo.RedirectStandardOutput = false;
                        killProcess.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Couldn't kill process: " + ex.Message);
                    }
                }

                _mineable = null;
            }
        }

        private static string GeneratePoolConfigJson(Mineable mineable)
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
                    if (mineable is Coin coin && coin.TickerSymbol == "XMR")
                    {
                        dict["currency"] = "monero";
                    }
                    break;
                case Algorithm.aeon7:
                    dict["currency"] = "aeon7";
                    break;
                case Algorithm.CryptonightBitTube2:
                    dict["currency"] = "Cryptonight_bitTube2";
                    break;
                case Algorithm.CryptonightConseal:
                    dict["currency"] = "cryptonight_conceal";
                    break;
                case Algorithm.Cryptonight:
                    dict["currency"] = "cryptonight";
                    break;
                case Algorithm.CryptonightGpu:
                    dict["currency"] = "cryptonight_gpu";
                    break;
                case Algorithm.CryptonightHaven:
                    dict["currency"] = "cryptonight_haven";
                    break;
                case Algorithm.CryptonightHeavy:
                    dict["currency"] = "cryptonight_heavy";
                    break;
                case Algorithm.CryptonightLite:
                    dict["currency"] = "cryptonight_lite";
                    break;
                case Algorithm.CryptonightLiteV7:
                    dict["currency"] = "cryptonight_lite_v7";
                    break;
                case Algorithm.CryptonightLiteV7Xor:
                    dict["currency"] = "cryptonight_lite_v7_xor";
                    break;
                case Algorithm.CryptonightMasari:
                    dict["currency"] = "cryptonight_masari";
                    break;
                case Algorithm.CryptonightR:
                    dict["currency"] = "cryptonight_r";
                    break;
                case Algorithm.CryptonightSuperfast:
                    dict["currency"] = "cryptonight_superfast";
                    break;
                case Algorithm.CryptonightTurtle:
                    dict["currency"] = "cryptonight_turtle";
                    break;
                case Algorithm.CryptonightV8Double:
					dict["currency"] = "cryptonight_v8_double";
                    break;
				case Algorithm.CryptonightV8:
                    dict["currency"] = "cryptonight_v8";
                    break;
                case Algorithm.CryptonightV8Half:
                    dict["currency"] = "cryptonight_v8_half";
                    break;
                case Algorithm.CryptonightV8Reversewaltz:
                    dict["currency"] = "cryptonight_v8_reversewaltz";
                    break;
                case Algorithm.CryptonightV7Stellite:
                    dict["currency"] = "cryptonight_v7_stellite";
                    break;
                case Algorithm.CryptonightV8Zelerius:
                    dict["currency"] = "cryptonight_v8_zelerius";
                    break;
                case Algorithm.bbscoin:
                    dict["currency"] = "bbscoin";
                    break;
                case Algorithm.bittube:
                    dict["currency"] = "bittube";
                    break;
                case Algorithm.freehaven:
                    dict["currency"] = "freehaven";
                    break;
                case Algorithm.graft:
                    dict["currency"] = "graft";
                    break;
                case Algorithm.haven:
                    dict["currency"] = "haven";
                    break;
                case Algorithm.lethean:
                    dict["currency"] = "lethean";
                    break;
                case Algorithm.masari:
                    dict["currency"] = "masari";
                    break;
                case Algorithm.monero:
                    dict["currency"] = "monero";
                    break;
                case Algorithm.qrl:
                    dict["currency"] = "qrl";
                    break;
                case Algorithm.ryo:
                    dict["currency"] = "ryo";
                    break;
                case Algorithm.stellite:
                    dict["currency"] = "stellite";
                    break;
                case Algorithm.turtlecoin:
                    dict["currency"] = "turtlecoin";
                    break;
                case Algorithm.plenteum:
                    dict["currency"] = "plenteum";
                    break;
                case Algorithm.zelerius:
                    dict["currency"] = "zelerius";
                    break;
                case Algorithm.xcash:
                    dict["currency"] = "xcash";
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
            return String.Join("\r\n", lines);
        }
    }
}
