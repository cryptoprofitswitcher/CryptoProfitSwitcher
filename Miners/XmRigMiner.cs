using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CryptoProfitSwitcher.Miners
{
    public class XmRigMiner : IMiner
    {
        private int _port;
        private bool _requestClose;
        private bool _minimized;
        private Process _process;

        public string Name => "XMRig Miner";
        public bool SupportsIndividualHashrate => false;
        public HashSet<DeviceConfig> DeviceConfigs { get; set; }
        public Pool Pool { get; set; }

        public XmRigMiner(HashSet<DeviceConfig> deviceConfigs, Pool pool)
        {
            DeviceConfigs = deviceConfigs;
            Pool = pool;
        }

        public double GetCurrentHashrate(DeviceConfig deviceConfig)
        {
            double hashrate = 0;
            try
            {
                var json = Helpers.GetJsonFromUrl($"http://127.0.0.1:{_port}/1/summary", false, null, CancellationToken.None);
                double? hashrateOrNull = JToken.Parse(json)["hashrate"]["total"].First.ToObject<double?>();
                if (hashrateOrNull.HasValue)
                {
                    hashrate = hashrateOrNull.Value;
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't get current hashrate: " + ex.Message);
            }
            return hashrate;
        }

        public void StartMiner(bool minimized)
        {
            _minimized = minimized;
            _process = new Process();
            DeviceConfig firstDevice = DeviceConfigs.First();
            string minerPath = Helpers.ResolveToFullPath(firstDevice.MinerPath, Helpers.GetApplicationRoot());
            _port = firstDevice.MinerApiPort > 0 ? firstDevice.MinerApiPort : Helpers.GetAvailablePort();

            string minerFolderPath = Path.GetDirectoryName(minerPath);
            _process.StartInfo.FileName = minerPath;

            List<string> userDefindedArgs = new List<string>();
            if (!String.IsNullOrEmpty(firstDevice.MinerArguments))
            {
                userDefindedArgs.AddRange(firstDevice.MinerArguments.Split(" "));
            }

            string args = "";
            string space = "";
            if (!userDefindedArgs.Contains("-o"))
            {
                args = $"{space}-o {Pool.PoolUrl}";
                space = " ";
            }
            if (!userDefindedArgs.Contains("-u"))
            {
                args += $"{space}-u {Pool.PoolUser}";
                space = " ";
            }
            if (!userDefindedArgs.Contains("-p"))
            {
                args += $"{space}-p {Pool.PoolPassword}";
                space = " ";
            }

            if (DeviceConfigs.All(dc => dc.DeviceType != DeviceType.CPU))
            {
                if (!userDefindedArgs.Contains("--no-cpu"))
                {
                    args += $"{space}--no-cpu";
                    space = " ";
                }
            }

            if (DeviceConfigs.Any(dc => dc.DeviceType == DeviceType.AMD))
            {
                if (!userDefindedArgs.Contains("--opencl"))
                {
                    args += $"{space}--opencl";
                    space = " ";
                }
                if (!userDefindedArgs.Any(a => a.StartsWith("--opencl-devices=", StringComparison.OrdinalIgnoreCase)))
                {
                    args += $"{space}--opencl-devices={string.Join(',', DeviceConfigs.Where(dc => dc.DeviceType == DeviceType.AMD).Select(dc => dc.DeviceId))}";
                    space = " ";
                }
            }

            if (DeviceConfigs.Any(dc => dc.DeviceType == DeviceType.NVIDIA))
            {
                if (!userDefindedArgs.Contains("--cuda"))
                {
                    args += $"{space}--cuda";
                    space = " ";
                }
                if (!userDefindedArgs.Any(a => a.StartsWith("--opencl-devices=", StringComparison.OrdinalIgnoreCase)))
                {
                    args += $"{space}--cuda-devices={string.Join(',', DeviceConfigs.Where(dc => dc.DeviceType == DeviceType.NVIDIA).Select(dc => dc.DeviceId))}";
                    space = " ";
                }
            }

            args += $"{space}--http-port={_port}";
            space = " ";


            if (!String.IsNullOrEmpty(firstDevice.MinerArguments))
            {
                args += space + firstDevice.MinerArguments;
            }

            _process.EnableRaisingEvents = true;
            _process.Exited += ProcessOnExited;
            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = true;
            _process.StartInfo.CreateNoWindow = false;
            _process.StartInfo.RedirectStandardOutput = false;
            _process.StartInfo.WorkingDirectory = minerFolderPath;
            _process.StartInfo.WindowStyle = minimized ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal;
            Log.Debug($"Miner Start Info: \"{_process.StartInfo.FileName}\" {_process.StartInfo.Arguments}");
            _process.Start();
        }

        private void ProcessOnExited(object sender, EventArgs e)
        {
            if (!_requestClose)
            {
                Log.Warning("Restart miner after failure: " + Name);
                Task.Delay(1000).Wait();
                StartMiner(_minimized);
            }
        }

        public void StopMiner()
        {
            if (_process != null)
            {
                _requestClose = true;
                Log.Debug($"Stopping miner={Name}, args={_process?.StartInfo?.Arguments}");
                try
                {
                    _process.CloseMainWindow();
                }
                catch (Exception ex)
                {
                    Log.Warning("Couldn't close miner process: " + ex.Message);
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
                    Log.Warning("Couldn't kill miner process: " + ex.Message);
                }
                _process.Dispose();
                _process = null;
            }
        }
    }
}
