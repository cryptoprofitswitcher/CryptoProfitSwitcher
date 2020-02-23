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
    public class TeamRedMiner : IMiner
    {
        public HashSet<DeviceConfig> DeviceConfigs { get; set; }
        public Pool Pool { get; set; }

        public TeamRedMiner(HashSet<DeviceConfig> deviceConfigs, Pool pool)
        {
            DeviceConfigs = deviceConfigs;
            Pool = pool;
        }

        private int _port;
        private Process _process;
        private bool _requestClose;
        private bool _minimized;

        public string Name => "Team Red Miner";
        public bool SupportsIndividualHashrate => true;
        public double GetCurrentHashrate(DeviceConfig deviceConfig)
        {
            double gpuHashrate = 0;
            try
            {
                var json = GetApiDataAsync(_port, "{\"command\":\"devs\"}").Result;

                var jDevices = JToken.Parse(json)["DEVS"].Children().ToList();
                if (jDevices.Count == 1)
                {
                    gpuHashrate = jDevices[0].Value<double>("KHS 30s");
                }
                else if (jDevices.Count > 1)
                {
                    foreach (JToken jDevice in jDevices)
                    {
                        if (string.Equals(jDevice.Value<int>("GPU").ToString(CultureInfo.InvariantCulture), deviceConfig.DeviceId, StringComparison.OrdinalIgnoreCase))
                        {
                            gpuHashrate = jDevice.Value<double>("KHS 30s");
                        }
                    }
                }
                gpuHashrate *= 1000;
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't get current hashrate: " + ex.Message);
            }

            return gpuHashrate;
        }

        private async Task<string> GetApiDataAsync(int port, string dataToSend)
        {
            string responseFromServer = null;
            TcpClient tcpc = null;
            try
            {
                tcpc = new TcpClient("127.0.0.1", port);
                var nwStream = tcpc.GetStream();

                var bytesToSend = Encoding.ASCII.GetBytes(dataToSend);
                await nwStream.WriteAsync(bytesToSend, 0, bytesToSend.Length).ConfigureAwait(false);

                var incomingBuffer = new byte[tcpc.ReceiveBufferSize];
                var offset = 0;
                var fin = false;

                while (!fin && tcpc.Client.Connected)
                {
                    var r = await nwStream.ReadAsync(incomingBuffer, offset, tcpc.ReceiveBufferSize - offset).ConfigureAwait(false);
                    for (var i = offset; i < offset + r; i++)
                    {
                        if (incomingBuffer[i] == 0x7C || incomingBuffer[i] == 0x7d || incomingBuffer[i] == 0x00)
                        {
                            fin = true;
                            break;
                        }
                    }

                    offset += r;
                }

                if (offset > 0)
                    responseFromServer = Encoding.ASCII.GetString(incomingBuffer);
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't get sgminer api data: " + ex.Message);
                return null;
            }
            finally
            {
                tcpc?.Close();
            }

            return responseFromServer;
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
            if (!userDefindedArgs.Contains("-o"))
            {
                string address = Pool.PoolUrl.StartsWith("stratum+tcp://", StringComparison.OrdinalIgnoreCase) ? Pool.PoolUrl : "stratum+tcp://" + Pool.PoolUrl;
                args = $"{space}-o {address}";
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

            if (!userDefindedArgs.Contains("-d"))
            {
                string devicesString = string.Join(',', DeviceConfigs.Select(dc => dc.DeviceId));
                args += $"{space}-d {devicesString}";
                space = " ";
            }

            _port = firstDeviceConfig.MinerApiPort > 0 ? firstDeviceConfig.MinerApiPort : Helpers.GetAvailablePort();
            if (!userDefindedArgs.Any(a => a.StartsWith("--api_listen={", StringComparison.OrdinalIgnoreCase)))
            {
                args += $"{space}--api_listen={_port}";
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
                    foreach (string splittedArgument in splittedArguments)
                    {
                        const string cnConfigArgmument = "--cn_config=";
                        if (splittedArgument.StartsWith(cnConfigArgmument, StringComparison.OrdinalIgnoreCase))
                        {
                            AddToListInDictionary(combinedArgumentsDictionary, cnConfigArgmument, splittedArgument.Substring(cnConfigArgmument.Length));
                        }
                        const string ethConfigArgmument = "--eth_config=";
                        if (splittedArgument.StartsWith(ethConfigArgmument, StringComparison.OrdinalIgnoreCase))
                        {
                            AddToListInDictionary(combinedArgumentsDictionary, ethConfigArgmument, splittedArgument.Substring(ethConfigArgmument.Length));
                        }
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
