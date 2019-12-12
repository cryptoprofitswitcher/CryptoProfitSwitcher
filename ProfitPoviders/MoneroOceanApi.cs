using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CryptoProfitSwitcher.ProfitPoviders
{
    public class MoneroOceanApi : IPoolProfitProvider
    {
        public Dictionary<Pool, Profit> GetProfits(IList<Pool> pools, bool enableCaching, DirectoryInfo appRootFolder, CancellationToken ct)
        {
            var poolProfitsDictionary = new Dictionary<Pool, Profit>();
            try
            {

                if (pools.Any())
                {
                    const string apiUrl = "https://api.moneroocean.stream/pool/stats";
                    var statsJson = Helpers.GetJsonFromUrl(apiUrl, enableCaching, appRootFolder, ct);
                    JToken poolStats = JToken.Parse(statsJson)["pool_statistics"];

                    double activePortProfit = poolStats.Value<double>("activePortProfit");
                    double profitXmrPerDay = activePortProfit * Profit.BaseHashrate;
                    double usdPriceXmr = poolStats["price"].Value<double>("usd");
                    double usdReward = profitXmrPerDay * usdPriceXmr;

                    foreach (Pool pool in pools)
                    {
                        poolProfitsDictionary[pool] = new Profit(usdReward, 0, profitXmrPerDay, 0, ProfitProvider.MoneroOceanApi);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to get profits data from XmrMinerApi: " + ex.Message);
            }

            return poolProfitsDictionary;
        }
    }
}
