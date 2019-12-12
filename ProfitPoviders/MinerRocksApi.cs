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
                        decimal diffDay = lastStats["pool"]["stats"]["diffs"].Value<decimal>("wavg24h");
                        JToken jNetwork = lastStats["network"];
                        decimal diffLive = jNetwork.Value<decimal>("difficulty");

                        decimal reward = jNetwork.Value<decimal>("reward");

                        decimal profitDay = (Profit.BaseHashrate * (86400 / diffDay)) * reward;
                        decimal profitLive = (Profit.BaseHashrate * (86400 / diffLive)) * reward;

                        // Get amount of coins
                        decimal coinUnits = lastStats["config"].Value<decimal>("coinUnits");
                        decimal amountDay = profitDay / coinUnits;
                        decimal amountLive = profitLive / coinUnits;

                        //Get usd price
                        decimal usdPrice = lastStats["coinPrice"].Value<decimal>("coin-usd");

                        //Multiplicate
                        decimal usdRewardDecDay = amountDay * usdPrice;
                        double usdRewardDay = (double)usdRewardDecDay;

                        decimal usdRewardDecLive = amountLive * usdPrice;
                        double usdRewardLive = (double)usdRewardDecLive;

                        poolProfitsDictionary[pool] = new Profit(usdRewardLive, usdRewardDay, (double)amountLive, (double)amountDay, ProfitProvider.MinerRocksApi);
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
