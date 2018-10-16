using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace CryptonightProfitSwitcher.Miners
{
    public class CastXmrMiner : IMiner
    {
        private Process _process;
        private Mineable _mineable;
        private IMiner _cpuMiner;

        public string Name => "Cast XMR";
        public double GetCurrentHashrate(Settings settings, DirectoryInfo appRootFolder)
        {
            double gpuHashrate = 0;
            try
            {
                const int port = 7777;

                var json = Helpers.GetJsonFromUrl($"http://127.0.0.1:{port}", settings, appRootFolder, CancellationToken.None);
                dynamic api = JObject.Parse(json);

                gpuHashrate = api.total_hash_rate;
                gpuHashrate = gpuHashrate / 1000;
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
            string minerPath = Helpers.ResolveToFullPath(mineable.CastXmrPath, appRoot);
            string minerFolderPath = Path.GetDirectoryName(minerPath);
            _process.StartInfo.FileName = "cmd";

            List<string> userDefindedArgs = new List<string>();
            if (!String.IsNullOrEmpty(mineable.CastXmrExtraArguments))
            {
                userDefindedArgs.AddRange(mineable.CastXmrExtraArguments.Split(" "));
            }

            string args = "";
            string space = "";
            if (!userDefindedArgs.Contains("-S"))
            {
                args = $"{space}-S {mineable.PoolAddress}";
                space = " ";
            }
            if (!userDefindedArgs.Contains("-u"))
            {
                args += $"{space}-u {mineable.PoolWalletAddress}";
                space = " ";
            }
            if (!userDefindedArgs.Contains("-p"))
            {
                args += $"{space}-p {mineable.PoolPassword}";
                space = " ";
            }
            if (!userDefindedArgs.Any(a => a.Contains("--algo=")))
            {
                switch (mineable.Algorithm)
                {
                    case Algorithm.CryptonightV7:
                        args += $"{space}--algo=1";
                        break;
                    case Algorithm.CryptonightHeavy:
                        args += $"{space}--algo=2";
                        break;
                    case Algorithm.CryptonightLite:
                        args += $"{space}--algo=4";
                        break;
                    case Algorithm.CryptonightBittube:
                        args += $"{space}--algo=5";
                        break;
                    case Algorithm.CryptonightStellite:
                        args += $"{space}--algo=6";
                        break;
                    case Algorithm.CryptonightHaven:
                        args += $"{space}--algo=7";
                        break;
                    case Algorithm.CryptonightMasari:
                        args += $"{space}--algo=8";
                        break;
                    case Algorithm.CryptonightV8:
                        args += $"{space}--algo=10";
                        break;
                    default:
                        throw new NotImplementedException("Couldn't start CastXmr, unknown algo: {mineable.Algorithm}\n" +
                                                          "You can set --algo yourself in the extra arguments.");
                }
                space = " ";
            }
            if (!userDefindedArgs.Contains("--remoteaccess"))
            {
                args += $"{space}--remoteaccess";
                space = " ";
            }
            if (!String.IsNullOrEmpty(mineable.CastXmrExtraArguments))
            {
                args += space + mineable.CastXmrExtraArguments;
            }
            _process.StartInfo.Arguments = $"/c \"{Path.GetFileName(minerPath)} {args}\"";
            _process.StartInfo.UseShellExecute = true;
            _process.StartInfo.CreateNoWindow = false;
            _process.StartInfo.RedirectStandardOutput = false;
            _process.StartInfo.WorkingDirectory = minerFolderPath;
            _process.StartInfo.WindowStyle = settings.StartMinerMinimized ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal;

            Thread.Sleep(TimeSpan.FromSeconds(settings.MinerStartDelay));
            _process.Start();

            if (mineable.CastXmrUseXmrStakCPUMining)
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
    }
}
