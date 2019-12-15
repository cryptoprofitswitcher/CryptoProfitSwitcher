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
    public class MinerRocksApi : IPoolProfitProvider
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
                        string apiUrl = $"https://{pool.ProfitProviderInfo}.miner.rocks/api/stats";
                        var profitsJson = Helpers.GetJsonFromUrl(apiUrl, enableCaching, appRootFolder, ct);
                        JToken lastStats = JToken.Parse(profitsJson);
                        double diffDay = lastStats["pool"]["stats"]["diffs"].Value<double>("wavg24h");
                        JToken jNetwork = lastStats["network"];
                        double diffLive = jNetwork.Value<double>("difficulty");

                        double reward = jNetwork.Value<double>("reward");

                        double profitDay = (Profit.BaseHashrate * (86400 / diffDay)) * reward;
                        double profitLive = (Profit.BaseHashrate * (86400 / diffLive)) * reward;

                        // Get amount of coins
                        double coinUnits = lastStats["config"].Value<double>("coinUnits");
                        double amountDay = profitDay / coinUnits;
                        double amountLive = profitLive / coinUnits;

                        //Get usd price
                        double usdPrice = lastStats["coinPrice"].Value<double>("coin-usd");

                        //Multiplicate
                        double usdRewardDay = amountDay * usdPrice;

                        double usdRewardLive = amountLive * usdPrice;

                        poolProfitsDictionary[pool] = new Profit(usdRewardLive, usdRewardDay, amountLive, amountDay, ProfitProvider.MinerRocksApi);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to get profits data from MinerRocksApi: " + ex.Message);
            }
            return poolProfitsDictionary;
        }
    }
}
