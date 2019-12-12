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
    public class MineXmrApi : IPoolProfitProvider
    {
        public Dictionary<Pool, Profit> GetProfits(IList<Pool> pools, bool enableCaching, DirectoryInfo appRootFolder, CancellationToken ct)
        {
            var poolProfitsDictionary = new Dictionary<Pool, Profit>();
            try
            {
                if (pools.Any())
                {
                    var moneroJson = Helpers.GetJsonFromUrl("https://api.coingecko.com/api/v3/simple/price?ids=monero&vs_currencies=usd", enableCaching, appRootFolder, ct);
                    double moneroUsdPrice = JToken.Parse(moneroJson)["monero"].Value<double>("usd");

                    const string apiUrl = "https://minexmr.com/api/pool/stats";
                    var statsJson = Helpers.GetJsonFromUrl(apiUrl, enableCaching, appRootFolder, ct);
                    JToken networkStats = JToken.Parse(statsJson)["network"];
                    ulong difficulty = networkStats.Value<ulong>("difficulty");
                    double reward = networkStats.Value<ulong>("reward") / 1000000000000d;
                    double profitCoins = (Profit.BaseHashrate * (86400d / difficulty)) * reward;
                    double profitUsd = profitCoins * moneroUsdPrice;

                    foreach (Pool pool in pools)
                    {
                        poolProfitsDictionary[pool] = new Profit(profitUsd, 0, profitCoins, 0, ProfitProvider.MineXmrApi);
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
