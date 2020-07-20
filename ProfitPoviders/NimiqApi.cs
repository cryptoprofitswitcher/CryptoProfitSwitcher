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
    public class NimiqApi : IPoolProfitProvider
    {
        private const string ApiKey = "9384ddec2ab01e1ebdb8a7cf54324aa2";

        public Dictionary<Pool, Profit> GetProfits(IList<Pool> pools, bool enableCaching, DirectoryInfo appRootFolder, CancellationToken ct)
        {
            var poolProfitsDictionary = new Dictionary<Pool, Profit>();
            try
            {

                if (pools.Any())
                {
                    string statsLiveApiUrl = $"https://api.nimiqx.com/network-stats/?api_key={ApiKey}";
                    var statsLiveJson = Helpers.GetJsonFromUrl(statsLiveApiUrl, enableCaching, appRootFolder, ct);
                    var statsLive = JToken.Parse(statsLiveJson);

                    string priceUsdApiUrl = $"https://api.nimiqx.com/price/usd?api_key={ApiKey}";
                    var priceUsdJson = Helpers.GetJsonFromUrl(priceUsdApiUrl, enableCaching, appRootFolder, ct);
                    var priceUsd = JToken.Parse(priceUsdJson);

                    double nim_day_kh = statsLive.Value<double>("nim_day_kh");

                    double amountLive = (nim_day_kh / 1000) * Profit.BaseHashrate;

                    //Get usd price
                    double usdPrice = priceUsd.Value<double>("usd");

                    //Multiplicate
                    double usdRewardLive = amountLive * usdPrice;


                    foreach (Pool pool in pools)
                    {
                        poolProfitsDictionary[pool] = new Profit(usdRewardLive, 0, amountLive, 0, ProfitProvider.NimiqApi);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to get profits data from NimiqAPI: " + ex.Message);
            }

            return poolProfitsDictionary;
        }
    }
}
