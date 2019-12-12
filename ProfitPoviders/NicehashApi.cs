using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CryptoProfitSwitcher.ProfitPoviders
{
    public class NiceHashApi : IPoolProfitProvider
    {
        public Dictionary<Pool, Profit> GetProfits(IList<Pool> pools, bool enableCaching, DirectoryInfo appRootFolder, CancellationToken ct)
        {
            var nicehashProfitsDictionary = new Dictionary<Pool, Profit>();

            try
            {
                if (pools.Any())
                {
                    var btcJson = Helpers.GetJsonFromUrl("https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd", enableCaching, appRootFolder, ct);
                    double btcUsdPrice = JToken.Parse(btcJson)["bitcoin"].Value<double>("usd");

                    var liveAlgorithms = new List<Pool>();
                    var dayAlgorithms = new List<Pool>();

                    foreach (var niceHashAlgorithm in pools)
                    {
                        if (niceHashAlgorithm.ProfitTimeframe == ProfitTimeframe.Day)
                        {
                            dayAlgorithms.Add(niceHashAlgorithm);
                        }
                        else
                        {
                            liveAlgorithms.Add(niceHashAlgorithm);
                        }
                    }

                    if (liveAlgorithms.Count > 0)
                    {
                        try
                        {
                            var nicehashLiveProfitsJson = Helpers.GetJsonFromUrl("https://api2.nicehash.com/main/api/v2/public/stats/global/current", enableCaching, appRootFolder, ct);
                            SetProfitFromJson(nicehashLiveProfitsJson, btcUsdPrice, ProfitTimeframe.Live, liveAlgorithms, nicehashProfitsDictionary);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Couldn't get live profits data from NiceHash: " + ex.Message);
                        }
                    }

                    if (dayAlgorithms.Count > 0)
                    {
                        try
                        {
                            var nicehashDayProfitsJson = Helpers.GetJsonFromUrl("https://api2.nicehash.com/main/api/v2/public/stats/global/24h", enableCaching, appRootFolder, ct);
                            SetProfitFromJson(nicehashDayProfitsJson, btcUsdPrice, ProfitTimeframe.Day, dayAlgorithms, nicehashProfitsDictionary);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Couldn't get daily profits data from NiceHash: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Couldn't get profits data from NiceHash: " + ex.Message);
            }
            return nicehashProfitsDictionary;
        }

        private static void SetProfitFromJson(string json, double btcUsdPrice, ProfitTimeframe timeframe, IList<Pool> nicehashAlgorithms, Dictionary<Pool, Profit> nicehashProfitsDictionary)
        {
            foreach (JToken stat in JToken.Parse(json)["algos"].Children())
            {
                int algo = stat.Value<int>("a");
                double price = stat.Value<double>("p");

                foreach (Pool matchedPool in nicehashAlgorithms.Where(na => na.ProfitProviderInfo == algo.ToString(CultureInfo.InvariantCulture)))
                {
                    double pricePerHashPerDay = price / 100000000;
                    double btcReward = pricePerHashPerDay * Profit.BaseHashrate;
                    var usdReward = btcReward * btcUsdPrice;

                    if (timeframe == ProfitTimeframe.Day)
                    {
                        nicehashProfitsDictionary[matchedPool] = new Profit(0, usdReward, 0, 0, ProfitProvider.NiceHashApi);
                    }
                    else
                    {
                        nicehashProfitsDictionary[matchedPool] = new Profit(usdReward, 0, 0, 0, ProfitProvider.NiceHashApi);
                    }
                }
            }
        }
    }
}
