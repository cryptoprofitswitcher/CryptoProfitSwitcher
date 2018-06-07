using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using Newtonsoft.Json.Linq;

namespace CryptonightProfitSwitcher.ProfitPoviders
{
    public static class NicehashApi
    {
        public static Dictionary<int, Profit> GetProfits(DirectoryInfo appRootFolder, Settings settings, IList<NicehashAlgorithm> nicehashAlgorithms)
        {
            var nicehashProfitsDictionary = new Dictionary<int, Profit>();

            try
            {
                if (nicehashAlgorithms.Any())
                {
                    var btcJson = Helpers.GetJsonFromUrl("https://api.coinmarketcap.com/v2/ticker/1/?convert=USD", settings, appRootFolder);
                    dynamic btc = JObject.Parse(btcJson);
                    double btcUsdPrice = btc.data.quotes.USD.price;
                    Console.WriteLine("Got BTC exchange rate: " + btcUsdPrice);


                    var liveAlgorithms = new List<NicehashAlgorithm>();
                    var dayAlgorithms = new List<NicehashAlgorithm>();

                    foreach(var niceHashAlgorithm in nicehashAlgorithms)
                    {
                        if (niceHashAlgorithm.OverrideProfitTimeframe.HasValue)
                        {
                            if (niceHashAlgorithm.OverrideProfitTimeframe.Value == ProfitTimeframe.Day)
                            {
                                dayAlgorithms.Add(niceHashAlgorithm);
                            }
                            else
                            {
                                liveAlgorithms.Add(niceHashAlgorithm);
                            }
                        }
                        else
                        {
                            if (settings.ProfitTimeframe == ProfitTimeframe.Day)
                            {
                                dayAlgorithms.Add(niceHashAlgorithm);
                            }
                            else
                            {
                                liveAlgorithms.Add(niceHashAlgorithm);
                            }
                        }
                    }

                    if (liveAlgorithms.Count > 0)
                    {
                        try
                        {
                            var nicehashLiveProfitsJson = Helpers.GetJsonFromUrl("https://api.nicehash.com/api?method=stats.global.current", settings, appRootFolder);
                            SetProfitFromJson(nicehashLiveProfitsJson, btcUsdPrice, settings, ProfitTimeframe.Live, liveAlgorithms, nicehashProfitsDictionary);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Couldn't get live profits data from NiceHash: " + ex.Message);
                        }
                    }

                    if (dayAlgorithms.Count > 0)
                    {
                        try
                        {
                            var nicehashDayProfitsJson = Helpers.GetJsonFromUrl("https://api.nicehash.com/api?method=stats.global.24h", settings, appRootFolder);
                            SetProfitFromJson(nicehashDayProfitsJson, btcUsdPrice, settings, ProfitTimeframe.Day, dayAlgorithms, nicehashProfitsDictionary);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Couldn't get daily profits data from NiceHash: " + ex.Message);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't get profits data from NiceHash: " + ex.Message);
            }
            return nicehashProfitsDictionary;
        }

        private static void SetProfitFromJson(string json, double btcUsdPrice, Settings settings, ProfitTimeframe timeframe, IList<NicehashAlgorithm> nicehashAlgorithms, Dictionary<int, Profit> nicehashProfitsDictionary)
        {
            dynamic nicehashProfits = JObject.Parse(json);
            var result = nicehashProfits.result;

            foreach (dynamic stat in result.stats)
            {
                int algo = stat.algo;
                double price = stat.price;

                var matchedAlgorithm = nicehashAlgorithms.FirstOrDefault(na => na.ApiId == algo);
                if (matchedAlgorithm != null)
                {
                    double btcReward = 0;
                    btcReward = (price / 1000000) * matchedAlgorithm.GetExpectedHashrate(settings);
                    var usdReward = btcReward * btcUsdPrice;
                    nicehashProfitsDictionary[matchedAlgorithm.ApiId] = new Profit(usdReward, ProfitProvider.NiceHashApi, timeframe);
                    Console.WriteLine("Got profit data for Nicehash: " + matchedAlgorithm.DisplayName);
                }
            }
        }
    }
}
