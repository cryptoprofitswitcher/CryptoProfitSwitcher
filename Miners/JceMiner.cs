using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace CryptonightProfitSwitcher.Miners
{
    internal class JceMiner : IMiner
    {
        private Process _process;
        private Mineable _mineable;
        private int _port;
        public string Name => "JCE Miner";

        public double GetCurrentHashrate(Settings settings, DirectoryInfo appRootFolder)
        {
            double gpuHashrate = 0;
            try
            {
                var json = Helpers.GetJsonFromUrl($"http://127.0.0.1:{_port}", settings, appRootFolder, CancellationToken.None);
                dynamic api = JObject.Parse(json);

                gpuHashrate = api.hashrate.total;
                return gpuHashrate;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't get current hashrate: " + ex.Message);
            }
            return 0;
        }

        public void StartMiner(Mineable mineable, Settings settings, string appRoot, DirectoryInfo appRootFolder)
        {
            _mineable = mineable;
            _process = new Process();
            string minerPath = Helpers.ResolveToFullPath(mineable.JceMinerPath, appRoot);
            string minerFolderPath = Path.GetDirectoryName(minerPath);
            _process.StartInfo.FileName = "cmd";

            List<string> userDefindedArgs = new List<string>();
            if (!String.IsNullOrEmpty(mineable.JceMinerExtraArguments))
            {
                userDefindedArgs.AddRange(mineable.JceMinerExtraArguments.Split(" "));
            }

            string args = "";
            string space = "";
            if (!userDefindedArgs.Contains("-o"))
            {
                args = $"{space}-o {mineable.PoolAddress}";
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
            if (!userDefindedArgs.Contains("--auto"))
            {
                if (!userDefindedArgs.Contains("--c") && !String.IsNullOrEmpty(mineable.JceMinerConfig))
                {
                    string configPath = Helpers.ResolveToFullPath(mineable.JceMinerConfig, appRoot);
                    File.Copy(configPath, Path.Combine(minerFolderPath, "current_config.txt"), true);
                    args += $"{space}-c current_config.txt";
                }
                else
                {
                    args += $"{space}--auto";
                }
                space = " ";
            }
            if (!userDefindedArgs.Contains("--any"))
            {
                args += $"{space}--any";
                space = " ";
            }

            if (!userDefindedArgs.Contains("--variation"))
            {
                switch (mineable.Algorithm)
                {
                    case Algorithm.CryptonightV7:
                        args += $"{space}--variation 3";
                        break;
                    case Algorithm.CryptonightHeavy:
                        args += $"{space}--variation 5";
                        break;
                    case Algorithm.CryptonightLite:
                        args += $"{space}--variation 4";
                        break;
                    case Algorithm.CryptonightBittube:
                        args += $"{space}--variation 13";
                        break;
                    case Algorithm.CryptonightStellite:
                        args += $"{space}--variation 7";
                        break;
                    case Algorithm.CryptonightHaven:
                        args += $"{space}--variation 12";
                        break;
                    case Algorithm.CryptonightMasari:
                        args += $"{space}--variation 11";
                        break;
                    case Algorithm.CryptonightV8:
                        args += $"{space}--variation 15";
                        break;
                    default:
                        throw new NotImplementedException($"Couldn't start JceMiner, unknown algo: {mineable.Algorithm}\n" +
                                                          "You can set --variation yourself in the extra arguments.");
                }
                space = " ";
            }

            int mportIndex = userDefindedArgs.IndexOf("--mport");
            if (mportIndex == -1)
            {
                if (mineable.JceMinerApiPort < 1)
                {
                    _port = Helpers.GetAvailablePort();
                }
                else
                {
                    _port = mineable.JceMinerApiPort;
                }
                args += $"{space}--mport {_port}";
                space = " ";
            }
            else
            {
                _port = Int32.Parse(userDefindedArgs[mportIndex + 1]);
            }

            if (!String.IsNullOrEmpty(mineable.JceMinerExtraArguments))
            {
                args += space + mineable.JceMinerExtraArguments;
            }
            _process.StartInfo.Arguments = $"/c \"{Path.GetFileName(minerPath)} {args}\"";
            _process.StartInfo.UseShellExecute = true;
            _process.StartInfo.CreateNoWindow = false;
            _process.StartInfo.RedirectStandardOutput = false;
            _process.StartInfo.WorkingDirectory = minerFolderPath;
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
        }
    }
}
