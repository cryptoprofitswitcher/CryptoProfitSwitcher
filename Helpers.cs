using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Factories;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace CryptonightProfitSwitcher
{
    internal static class Helpers
    {
       
        internal static Dictionary<ConsoleKey, string> ManualSelectionDictionary = new Dictionary<ConsoleKey, string>
        {
            {ConsoleKey.D1, "1"}, {ConsoleKey.D2, "2"}, {ConsoleKey.D3, "3"},
            {ConsoleKey.D4, "4"}, {ConsoleKey.D5, "5"}, {ConsoleKey.D6, "6"},
            {ConsoleKey.D7, "7"}, {ConsoleKey.D8, "8"}, {ConsoleKey.D9, "9"},
            {ConsoleKey.NumPad1, "Num1"}, {ConsoleKey.NumPad2, "Num2"}, {ConsoleKey.NumPad3, "Num3"},
            {ConsoleKey.NumPad4, "Num4"}, {ConsoleKey.NumPad5, "Num5"}, {ConsoleKey.NumPad6, "Num6"},
            {ConsoleKey.NumPad7, "Num7"}, {ConsoleKey.NumPad8, "Num8"}, {ConsoleKey.NumPad9, "Num9"},
            {ConsoleKey.F1,"F1"}, {ConsoleKey.F2,"F2"}, {ConsoleKey.F3 ,"F3"},
            {ConsoleKey.F4,"F4"}, {ConsoleKey.F5,"F5"}, {ConsoleKey.F6 ,"F6"},
            {ConsoleKey.F7,"F7"}, {ConsoleKey.F8,"F8"}, {ConsoleKey.F9 ,"F9"},
            {ConsoleKey.F10,"F10"}, {ConsoleKey.F11,"F11"}, {ConsoleKey.F12 ,"F12"}
        };

        public static string CreateMD5(string input)
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
            return rounded.ToString() + currencySymbol;
        }

        internal static T GetProperty<T>(ExpandoObject expando, string propertyName)
        {
            var expandoDict = expando as IDictionary<string, object>;
            var propertyValue = expandoDict[propertyName];
            return (T)propertyValue;
        }
        private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);
        public static int GetAvailablePort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(DefaultLoopbackEndpoint);
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }
        internal static string GetApplicationRoot()
        {
            var exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return exePath;
        }

        internal static string GetApplicationVersion()
        {
            var ver = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            return ver;
        }

        internal static string ResolveToFullPath(string path, string appRootPath)
        {
            string resolvedPath = Path.Combine(appRootPath, path);
            string fullPath = Path.GetFullPath(resolvedPath);
            return fullPath;
        }

        internal static string ResolveToArgumentPath(string path, string appRootPath)
        {
            string resolvedPath = Path.Combine(appRootPath, path);
            string fullPath = Path.GetFullPath(resolvedPath);
            return "\"" + fullPath + "\"";
        }

        internal static Profit GetPoolProfitForCoin(Coin coin, Dictionary<ProfitProvider, Dictionary<string, Profit>> poolProfitsDictionary, Settings settings)
        {
            Profit profit = new Profit();
            List<ProfitProvider> orderedProfitProviders = GetPoolProfitProviders(settings, coin);
            var profitSwitchingStrategy = ProfitSwitchingStrategyFactory.GetProfitSwitchingStrategy(settings.ProfitSwitchingStrategy);
            while (profitSwitchingStrategy.GetReward(profit, coin, settings.ProfitTimeframe) == 0 && orderedProfitProviders.Count > 0)
            {
                ProfitProvider profitProvider = orderedProfitProviders[0];
                var poolProfits = poolProfitsDictionary.GetValueOrDefault(profitProvider, null);
                if (poolProfits != null)
                {
                    profit = poolProfits.GetValueOrDefault(coin.TickerSymbol, new Profit());
                }
                orderedProfitProviders.RemoveAt(0);
            }
            return profit;
        }

        internal static List<ProfitProvider> GetPoolProfitProviders(Settings settings, Coin coin)
        {
            var result = new List<ProfitProvider>();

            if (coin != null && !String.IsNullOrEmpty(coin.OverridePoolProfitProviders))
            {
                var overrrideProfitProvidersSplitted = coin.OverridePoolProfitProviders.Split(",");
                foreach (var profitProviderString in overrrideProfitProvidersSplitted)
                {
                    ProfitProvider profitProvider;
                    if (Enum.TryParse(profitProviderString, out profitProvider))
                    {
                        if (!result.Contains(profitProvider))
                        {
                            result.Add(profitProvider);
                        }
                    }
                }
            }

            if (!String.IsNullOrEmpty(settings.PoolProfitProviders))
            {
                var profitProvidersSplitted = settings.PoolProfitProviders.Split(",");
                foreach (var profitProviderString in profitProvidersSplitted)
                {
                    ProfitProvider profitProvider;
                    if (Enum.TryParse(profitProviderString, out profitProvider))
                    {
                        if (!result.Contains(profitProvider))
                        {
                            result.Add(profitProvider);
                        }
                    }
                }
            }

            if (result.Count == 0)
            {
                // Return default providers
                result.Add(ProfitProvider.MineCryptonightApi);
                result.Add(ProfitProvider.MinerRocksApi);
            }
            return result;
        }

        public static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsMacOS() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsLinux() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);


        static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        internal static string GetJsonFromUrl(string url, Settings settings, DirectoryInfo appRootFolder, CancellationToken ct)
        {
            var cacheFolder = appRootFolder.CreateSubdirectory("Cache");
            string responseBody;
            try
            {
                using (var client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = client.GetAsync(url, ct).Result;

                    response.EnsureSuccessStatusCode();

                    using (HttpContent content = response.Content)
                    {
                        responseBody = response.Content.ReadAsStringAsync().Result;
                        //Save to cache
                        if (settings.EnableCaching && !url.Contains("127.0.0.1"))
                        {
                            int tries = 0;
                            while(tries < 2)
                            {
                                tries++;
                                try
                                {
                                    string hashedFilename = CreateMD5(url) + ".json";
                                    string savePath = Path.Combine(cacheFolder.FullName, hashedFilename);
                                    _lock.EnterWriteLock();
                                    File.WriteAllText(savePath, responseBody);
                                    _lock.ExitWriteLock();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Couldn't save to cache: " + ex);
                                    // Reset Cache
                                    _lock.EnterWriteLock();
                                    cacheFolder.Delete();
                                    _lock.ExitWriteLock();
                                }
                            }
                        }
                        return responseBody;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't get data from: " + url);
                Console.WriteLine("Error message: " + ex.Message);

                //Try to get from cache
                string hashedFilename = CreateMD5(url) + ".json";
                var cachedFile = cacheFolder.GetFiles(hashedFilename).First();
                var cachedContent = File.ReadAllText(cachedFile.FullName);
                Console.WriteLine("Got data from cache.");
                return cachedContent;
                throw;
            }
        }
    }
}
