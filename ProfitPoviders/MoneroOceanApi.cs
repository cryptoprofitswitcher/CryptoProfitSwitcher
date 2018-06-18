using System;
using System.Collections.Generic;
using System.IO;
using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using Newtonsoft.Json.Linq;

namespace CryptonightProfitSwitcher.ProfitPoviders
{
    public class MoneroOceanApi : IPoolProfitProvider
    {
        public Dictionary<string, Profit> GetProfits(DirectoryInfo appRootFolder, Settings settings, IList<Coin> coins)
        {
            var poolProfitsDictionary = new Dictionary<string, Profit>();

            foreach (var coin in coins)
            {
                if (coin.TickerSymbol == "MoneroOcean")
                {
                    try
                    {
                        const string apiUrl = "https://api.moneroocean.stream/pool/stats";
                        var statsJson = Helpers.GetJsonFromUrl(apiUrl, settings, appRootFolder);
                        dynamic lastStats = JObject.Parse(statsJson);

                        decimal activePortProfit = lastStats.pool_statistics.activePortProfit;
                        decimal profitXmrPerDay = activePortProfit * coin.GetExpectedHashrate(settings);

                        decimal usdPriceXmr = lastStats.pool_statistics.price.usd;

                        decimal usdRewardDec = profitXmrPerDay * usdPriceXmr;
                        double usdReward = (double)usdRewardDec;

                        poolProfitsDictionary[coin.TickerSymbol] = new Profit(usdReward,0,0,0,ProfitProvider.MoneroOceanApi, ProfitTimeframe.Live);
                        Console.WriteLine($"Got profit data for {coin.TickerSymbol} from MoneroOceanAPI");

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Couldn't get profits data for {coin.DisplayName} from MoneroOcean: " + ex.Message);
                    } 
                }
            }

            return poolProfitsDictionary;
        }
    }
}
