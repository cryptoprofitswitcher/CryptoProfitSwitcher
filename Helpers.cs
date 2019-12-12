using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Serilog;

namespace CryptoProfitSwitcher
{
    internal static class Helpers
    {
        private static readonly Random _random = new Random();

        public static string CreateMd5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        internal static string ToCurrency(this double val, string currencySymbol)
        {
            var rounded = Math.Round(val, 2, MidpointRounding.AwayFromZero);
            return rounded.ToString(CultureInfo.InvariantCulture) + currencySymbol;
        }

        internal static string ToHashrate(this double val)
        {
            string unit = " H/s";
            if (val > 12000)
            {
                val = val / 1000;
                unit = " kH/s";
                if (val > 12000)
                {
                    val = val / 1000;
                    unit = " MH/s";
                    if (val > 12000)
                    {
                        val = val / 1000;
                        unit = " GH/s";
                    }
                }
            }
            var rounded = Math.Round(val, 3, MidpointRounding.AwayFromZero);
            return rounded.ToString(CultureInfo.InvariantCulture) + unit;
        }


        internal static string GetApplicationRoot()
        {
            return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        }

        internal static string GetApplicationVersion()
        {
            return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }

        internal static string ResolveToFullPath(string path, string appRootPath)
        {
            string resolvedPath = Path.Combine(appRootPath, path);
            return Path.GetFullPath(resolvedPath);
        }

        internal static string ResolveToArgumentPath(string path, string appRootPath)
        {
            string resolvedPath = Path.Combine(appRootPath, path);
            string fullPath = Path.GetFullPath(resolvedPath);
            return "\"" + fullPath + "\"";
        }


        public static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsMac() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsLinux() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        internal static string GetJsonFromUrl(string url, bool enableCaching, DirectoryInfo appRootFolder, CancellationToken ct)
        {
            DirectoryInfo cacheFolder = null;
            if (appRootFolder != null)
            {
                cacheFolder = appRootFolder.CreateSubdirectory("Cache");
            }
            string responseBody;
            try
            {
                using var client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
                HttpResponseMessage response = client.GetAsync(new Uri(url), ct).Result;

                response.EnsureSuccessStatusCode();

                using HttpContent content = response.Content;
                responseBody = response.Content.ReadAsStringAsync().Result;
                //Save to cache
                if (enableCaching && !url.Contains("127.0.0.1", StringComparison.InvariantCulture))
                {
                    int tries = 0;
                    while (tries < 2)
                    {
                        tries++;
                        try
                        {
                            string hashedFilename = CreateMd5(url) + ".json";
                            if (cacheFolder != null)
                            {
                                string savePath = Path.Combine(cacheFolder.FullName, hashedFilename);
                                _lock.EnterWriteLock();
                                File.WriteAllText(savePath, responseBody);
                            }

                            _lock.ExitWriteLock();
                        }
                        catch (Exception ex)
                        {
                            Log.Debug("Couldn't save to cache: " + ex);
                            // Reset Cache
                            _lock.EnterWriteLock();
                            cacheFolder?.Delete();
                            _lock.ExitWriteLock();
                        }
                    }
                }
                return responseBody;
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't get data from: " + url);
                Log.Debug("Error message: " + ex.Message);

                //Try to get from cache
                if (cacheFolder != null)
                {
                    string hashedFilename = CreateMd5(url) + ".json";
                    var cachedFile = cacheFolder.GetFiles(hashedFilename).First();
                    var cachedContent = File.ReadAllText(cachedFile.FullName);
                    Console.WriteLine("Got data from cache.");
                    return cachedContent;
                }
                throw;
            }
        }

        internal static void ExecuteScript(string scriptPath, string appFolderPath)
        {
            try
            {
                if (!String.IsNullOrEmpty(scriptPath))
                {
                    //Execute reset script
                    var fileInfo = new FileInfo(Helpers.ResolveToFullPath(scriptPath, appFolderPath));
                    switch (fileInfo.Extension)
                    {
                        case ".bat":
                        case ".BAT":
                        case ".cmd":
                        case ".CMD":
                            {
                                // Run batch in Windows
                                var resetProcess = new Process();
                                resetProcess.StartInfo.FileName = "cmd.exe";
                                resetProcess.StartInfo.Arguments = $"/c {Helpers.ResolveToArgumentPath(scriptPath, appFolderPath)}";
                                resetProcess.StartInfo.UseShellExecute = true;
                                resetProcess.StartInfo.CreateNoWindow = false;
                                resetProcess.StartInfo.RedirectStandardOutput = false;
                                resetProcess.Start();
                                resetProcess.WaitForExit();
                                break;
                            }
                        case ".sh":
                            {
                                // Run sh script in Linux
                                var resetProcess = new Process();
                                resetProcess.StartInfo.FileName = "x-terminal-emulator";
                                resetProcess.StartInfo.Arguments = $"-e \"'{Helpers.ResolveToFullPath(scriptPath, appFolderPath)}'\"";
                                resetProcess.StartInfo.UseShellExecute = true;
                                resetProcess.StartInfo.CreateNoWindow = false;
                                resetProcess.StartInfo.RedirectStandardOutput = false;
                                resetProcess.Start();
                                resetProcess.WaitForExit();
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't execute script: " + scriptPath);
                Log.Error("Exception: " + ex);
            }
        }

        public static int GetAvailablePort()
        {
            int startingPort = _random.Next(4000, 40000);
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            //getting active connections
            var tcpConnectionPorts = properties.GetActiveTcpConnections()
                .Where(n => n.LocalEndPoint.Port >= startingPort)
                .Select(n => n.LocalEndPoint.Port);

            //getting active tcp listners - WCF service listening in tcp
            var tcpListenerPorts = properties.GetActiveTcpListeners()
                .Where(n => n.Port >= startingPort)
                .Select(n => n.Port);

            //getting active udp listeners
            var udpListenerPorts = properties.GetActiveUdpListeners()
                .Where(n => n.Port >= startingPort)
                .Select(n => n.Port);

            var port = Enumerable
                .Range(startingPort, ushort.MaxValue)
                .Where(i => !tcpConnectionPorts.Contains(i))
                .Where(i => !tcpListenerPorts.Contains(i))
                .FirstOrDefault(i => !udpListenerPorts.Contains(i));

            return port;
        }
    }
}
