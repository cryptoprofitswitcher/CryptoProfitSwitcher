using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CryptoProfitSwitcher.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CryptoProfitSwitcher.Miners
{
    public class ClaymoreMiner : IMiner
    {
        public HashSet<DeviceConfig> DeviceConfigs { get; set; }
        public Pool Pool { get; set; }

        public ClaymoreMiner(HashSet<DeviceConfig> deviceConfigs, Pool pool)
        {
            DeviceConfigs = deviceConfigs;
            Pool = pool;
        }

        private int _port;
        private Process _process;
        private bool _requestClose;
        private bool _minimized;

        public string Name => "Claymore Miner";
        public bool SupportsIndividualHashrate => true;
        public double GetCurrentHashrate(DeviceConfig deviceConfig)
        {
            double gpuHashrate = 0;
            try
            {
                var json = GetApiDataAsync(_port, "{\"id\":0,\"jsonrpc\":\"2.0\",\"method\":\"miner_getstat1\"}\n").Result;

                var jResults = JToken.Parse(json)["result"].Value<JArray>();
                string hashrates = jResults[3].Value<string>();
                if (hashrates.Contains(";", StringComparison.OrdinalIgnoreCase))
                {
                    string[] splitHashrates = hashrates.Split(';');
                    int index = DeviceConfigs.OrderBy(dc => dc.DeviceId).ToList().IndexOf(deviceConfig);
                    double hashrateInKh = double.Parse(splitHashrates[index], NumberStyles.None, CultureInfo.InvariantCulture);
                    return hashrateInKh * 1000;
                }
                else
                {
                    double hashrateInKh = double.Parse(hashrates, NumberStyles.None, CultureInfo.InvariantCulture);
                    return hashrateInKh * 1000;
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't get current hashrate: " + ex.Message);
            }

            return gpuHashrate;
        }

        private async Task<string> GetApiDataAsync(int port, string dataToSend)
        {
            try
            {
                var bytesToSend = Encoding.ASCII.GetBytes(dataToSend);
                using var client = new TcpClient("127.0.0.1", port);
                using (var nwStream = client.GetStream())
                {
                    await nwStream.WriteAsync(bytesToSend, 0, bytesToSend.Length).ConfigureAwait(false);
                    var bytesToRead = new byte[client.ReceiveBufferSize];
                    var bytesRead = await nwStream.ReadAsync(bytesToRead, 0, client.ReceiveBufferSize).ConfigureAwait(false);
                    return Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't get claymore api data: " + ex.Message);
                return null;
            }
        }


        public void StartMiner(bool minimized)
        {
            _minimized = minimized;
            _process = new Process();
            DeviceConfig firstDeviceConfig = DeviceConfigs.First();
            string minerPath = Helpers.ResolveToFullPath(firstDeviceConfig.MinerPath, Helpers.GetApplicationRoot());
            string minerFolderPath = Path.GetDirectoryName(minerPath);
            _process.StartInfo.FileName = "cmd";
            List<string> userDefindedArgs = new List<string>();

            if (!String.IsNullOrEmpty(firstDeviceConfig.MinerArguments))
            {
                userDefindedArgs.AddRange(firstDeviceConfig.MinerArguments.Split(" "));
            }

            if (!String.IsNullOrEmpty(firstDeviceConfig.MinerDeviceSpecificArguments))
            {
                userDefindedArgs.AddRange(firstDeviceConfig.MinerDeviceSpecificArguments.Split(" "));
            }

            string args = "";
            string space = "";
            if (!userDefindedArgs.Contains("-epool"))
            {
                args = $"{space}-epool {Pool.PoolUrl}";
                space = " ";
            }

            if (!userDefindedArgs.Contains("-ewal"))
            {
                args += $"{space}-ewal {Pool.PoolUser}";
                space = " ";
            }

            if (!userDefindedArgs.Contains("-epsw"))
            {
                args += $"{space}-epsw {Pool.PoolPassword}";
                space = " ";
            }

            if (!userDefindedArgs.Contains("-di"))
            {
                string devicesString = string.Join("",DeviceConfigs.Select(dc => dc.DeviceId));
                args += $"{space}-di {devicesString}";
                space = " ";
            }

            _port = firstDeviceConfig.MinerApiPort > 0 ? firstDeviceConfig.MinerApiPort : Helpers.GetAvailablePort();
            if (!userDefindedArgs.Contains("-mport"))
            {
                args += $"{space}-mport -{_port}";
                space = " ";
            }

            if (!String.IsNullOrEmpty(firstDeviceConfig.MinerArguments))
            {
                args += space + firstDeviceConfig.MinerArguments;
            }

            if (DeviceConfigs.Any(dc => !string.IsNullOrEmpty(dc.MinerDeviceSpecificArguments)))
            {
                Dictionary<string, List<string>> combinedArgumentsDictionary = new Dictionary<string, List<string>>();
                foreach (DeviceConfig deviceConfig in DeviceConfigs)
                {
                    var splittedArguments = deviceConfig.MinerDeviceSpecificArguments.Split(" ");
                    for (var index = 0; index + 1 < splittedArguments.Length; index += 2)
                    {
                        AddToListInDictionary(combinedArgumentsDictionary,splittedArguments[index], splittedArguments[index + 1]);
                    }
                }
                StringBuilder argumentsBuilder = new StringBuilder();
                bool first = true;
                foreach (var combinedArgument in combinedArgumentsDictionary)
                {
                    if (!first)
                    {
                        argumentsBuilder.Append(" ");
                    }

                    first = false;
                    argumentsBuilder.Append(combinedArgument.Key);
                    argumentsBuilder.Append(" ");
                    argumentsBuilder.Append(string.Join(',', combinedArgument.Value));
                }
                args += space + argumentsBuilder;
            }

            _process.EnableRaisingEvents = true;
            _process.Exited += ProcessOnExited;
            _process.StartInfo.Arguments = $"/c \"{Path.GetFileName(minerPath)} {args}\"";
            _process.StartInfo.UseShellExecute = true;
            _process.StartInfo.CreateNoWindow = false;
            _process.StartInfo.RedirectStandardOutput = false;
            _process.StartInfo.WorkingDirectory = minerFolderPath;
            _process.StartInfo.WindowStyle = minimized ? ProcessWindowStyle.Minimized : ProcessWindowStyle.Normal;
            Log.Debug($"Miner Start Info: \"{_process.StartInfo.FileName}\" {_process.StartInfo.Arguments}");
            _process.Start();
        }

        private void AddToListInDictionary(Dictionary<string, List<string>> dict, string key, string item)
        {
            if (dict.ContainsKey(key))
            {
                dict[key].Add(item);
            }
            else
            {
                dict[key] = new List<string>() { item };
            }
        }

        private void ProcessOnExited(object sender, EventArgs e)
        {
            if (!_requestClose)
            {
                Log.Warning("Restart miner after failure: " + Name);
                Task.Delay(5000).Wait();
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
