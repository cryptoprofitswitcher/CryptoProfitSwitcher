using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CryptoProfitSwitcher.ProfitPoviders
{
    public class HeroMinersApi : IPoolProfitProvider
    {
        public Dictionary<Pool, Profit> GetProfits(IList<Pool> pools, bool enableCaching, DirectoryInfo appRootFolder, CancellationToken ct)
        {
            var poolProfitsDictionary = new Dictionary<Pool, Profit>();
            try
            {
                foreach (Pool pool in pools)
                {
                    if (!ct.IsCancellationRequested)
                    {<
                        string apiUrl = $"https://{pool.ProfitProviderInfo}.herominers.com/api/stats";
                        var profitsJson = Helpers.GetJsonFromUrl(apiUrl, enableCaching, appRootFolder, ct);
                        JToken lastStats = JToken.Parse(profitsJson);

                        double diffDay = lastStats["charts"].Value<double>("difficulty_1d");
                        double diffLive = lastStats["network"].Value<double>("difficulty");

                        double reward = lastStats["lastblock"].Value<double>("reward");

                        double profitDay = (Profit.BaseHashrate * (86400 / diffDay)) * reward;
                        double profitLive = (Profit.BaseHashrate * (86400 / diffLive)) * reward;

                        // Get amount of coins
                        double coinUnits = lastStats["config"].Value<double>("coinUnits");
                        double amountDay = profitDay / coinUnits;
                        double amountLive = profitLive / coinUnits;

                        //Get usd price
                        double usdPrice = lastStats["charts"].Value<double>("price_1h");

                        //Multiplicate
                        double usdRewardDecDay = amountDay * usdPrice;
                        double usdRewardDay = (double)usdRewardDecDay;

                        double usdRewardDecLive = amountLive * usdPrice;
                        double usdRewardLive = (double)usdRewardDecLive;

                        poolProfitsDictionary[pool] = new Profit(usdRewardLive, usdRewardDay, amountLive, amountDay, ProfitProvider.HeroMinersApi);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to get profits data from HeroMinersApi: " + ex.Message);
            }
            return poolProfitsDictionary;
        }
    }
}
