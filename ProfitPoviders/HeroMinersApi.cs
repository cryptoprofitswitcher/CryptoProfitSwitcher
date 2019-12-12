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
                    {
                        string apiUrl = $"https://{pool.ProfitProviderInfo}.herominers.com/api/stats";
                        var profitsJson = Helpers.GetJsonFromUrl(apiUrl, enableCaching, appRootFolder, ct);
                        dynamic lastStats = JObject.Parse(profitsJson);

                        decimal diffDay = lastStats.charts.difficulty_1d;
                        decimal diffLive = lastStats.network.difficulty;

                        decimal reward = lastStats.lastblock.reward;

                        decimal profitDay = (Profit.BaseHashrate * (86400 / diffDay)) * reward;
                        decimal profitLive = (Profit.BaseHashrate * (86400 / diffLive)) * reward;

                        // Get amount of coins
                        decimal coinUnits = lastStats.config.coinUnits;
                        decimal amountDay = profitDay / coinUnits;
                        decimal amountLive = profitLive / coinUnits;

                        //Get usd price
                        decimal usdPrice = lastStats.charts.price_1h;

                        //Multiplicate
                        decimal usdRewardDecDay = amountDay * usdPrice;
                        double usdRewardDay = (double)usdRewardDecDay;

                        decimal usdRewardDecLive = amountLive * usdPrice;
                        double usdRewardLive = (double)usdRewardDecLive;

                        poolProfitsDictionary[pool] = new Profit(usdRewardLive, usdRewardDay, (double)amountLive, (double)amountDay, ProfitProvider.HeroMinersApi);
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
