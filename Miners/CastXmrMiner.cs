using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CryptonightProfitSwitcher.Miners
{
    public class CastXmrMiner : IMiner
    {
        Process _process = null;
        Mineable _mineable = null;
        IMiner _cpuMiner = null;
        public double GetCurrentHashrate(Settings settings, DirectoryInfo appRootFolder)
        {
            double gpuHashrate = 0;
            try
            {
                const int port = 7777;

                var json = Helpers.GetJsonFromUrl($"http://127.0.0.1:{port}", settings, appRootFolder);
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

            double totalHashRate = gpuHashrate + cpuHashrate;
            return totalHashRate;
        }

        public void StartMiner(Mineable mineable, Settings settings, string appRoot, DirectoryInfo appRootFolder)
        {
            _mineable = mineable;
            _process = new Process();
            string minerPath = Helpers.ResolveToFullPath(mineable.CastXmrPath, appRoot);
            string minerFolderPath = Path.GetDirectoryName(minerPath);
            var minerDirectory = new DirectoryInfo(minerFolderPath);
            _process.StartInfo.FileName = minerPath;

            string args = $"-S \"{mineable.PoolAddress}\"";
            args += $" -u \"{mineable.PoolWalletAddress}\"";
            switch (mineable.Algorithm)
            {
                case Algorithm.CryptonightV7:
                    args += " --algo=1";
                    break;
                case Algorithm.CryptonightHeavy:
                    args += " --algo=2";
                    break;
                case Algorithm.CryptonightLite:
                    args += " --algo=4";
                    break;
                case Algorithm.CryptonightBittube:
                    args += " --algo=5";
                    break;
                case Algorithm.CryptonightStellite:
                    args += " --algo=6";
                    break;
                default:
                    throw new NotImplementedException("Couldn't start CastXmr, unknown algo: " + mineable.Algorithm);
            }
            args += " --remoteaccess";
            if (!String.IsNullOrEmpty(mineable.CastXmrExtraArguments))
            {
                args += " " + mineable.CastXmrExtraArguments;
            }
            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = true;
            _process.StartInfo.CreateNoWindow = false;
            _process.StartInfo.RedirectStandardOutput = false;
            _process.StartInfo.WorkingDirectory = minerFolderPath;
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
            if (_cpuMiner != null)
            {
                _cpuMiner.StopMiner();
                _cpuMiner = null;
            }
        }
    }
}
