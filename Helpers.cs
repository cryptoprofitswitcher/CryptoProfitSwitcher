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
using System.Reflection;
using System.Threading;

namespace CryptonightProfitSwitcher
{
    internal static class Helpers
    {
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
            while (profitSwitchingStrategy.GetReward(profit,coin,settings.ProfitTimeframe) == 0 && orderedProfitProviders.Count > 0)
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
                foreach(var profitProviderString in profitProvidersSplitted)
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

        static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        internal static string GetJsonFromUrl(string url, Settings settings, DirectoryInfo appRootFolder)
        {
            var cacheFolder = appRootFolder.CreateSubdirectory("Cache");
            string responseBody;
            try
            {
                using (var client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    
                    HttpResponseMessage response = client.GetAsync(url).Result;

                    response.EnsureSuccessStatusCode();

                    using (HttpContent content = response.Content)
                    {
                        responseBody = response.Content.ReadAsStringAsync().Result;
                        //Save to cache
                        if (settings.EnableCaching && !url.Contains("127.0.0.1"))
                        {
                            try
                            {
                                var urlMapFile = cacheFolder.GetFiles("urlmap.json").FirstOrDefault();
                                if (urlMapFile == null)
                                {
                                    var serialized2 = JsonConvert.SerializeObject(new Dictionary<string, string>());
                                    _lock.EnterWriteLock();
                                    File.WriteAllText(ResolveToFullPath("Cache/urlmap.json", appRootFolder.FullName), serialized2);
                                    _lock.ExitWriteLock();
                                    urlMapFile = cacheFolder.GetFiles("urlmap.json").FirstOrDefault();
                                }
                                _lock.EnterReadLock();
                                var urlMapJson = File.ReadAllText(urlMapFile.FullName);
                                _lock.ExitReadLock();
                                var urlMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(urlMapJson);
                                if (urlMap.ContainsKey(url))
                                {
                                    var cachedFilename = urlMap[url];
                                    var cachedFile = cacheFolder.GetFiles(cachedFilename).FirstOrDefault();
                                    cachedFile?.Delete();
                                }
                                string saveFilename = Guid.NewGuid().ToString() + ".json";
                                string savePath = ResolveToFullPath($"Cache/{saveFilename}", appRootFolder.FullName);
                                File.WriteAllText(savePath, responseBody);
                                urlMap[url] = saveFilename;
                                string serialized = JsonConvert.SerializeObject(urlMap);
                                string urlMapPath = ResolveToFullPath("Cache/urlmap.json", appRootFolder.FullName);
                                _lock.EnterWriteLock();
                                File.WriteAllText(urlMapPath, serialized);
                                _lock.ExitWriteLock();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Couldn't save to cache: " + ex.Message);
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
                var urlMapFile = cacheFolder.GetFiles("urlmap.json").FirstOrDefault();
                if (urlMapFile != null)
                {
                    var urlMapJson = File.ReadAllText(urlMapFile.FullName);
                    var urlMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(urlMapJson);
                    if (urlMap.ContainsKey(url))
                    {
                        var cachedFilename = urlMap[url];
                        var cachedFile = cacheFolder.GetFiles(cachedFilename).First();
                        var cachedContent = File.ReadAllText(urlMapFile.FullName);
                        Console.WriteLine("Got data from cache.");
                        return cachedContent;
                    }
                }
                throw;
            }
        }
    }
}
