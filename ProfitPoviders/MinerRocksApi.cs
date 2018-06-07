using System;
using System.Collections.Generic;
using System.IO;
using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using Newtonsoft.Json.Linq;

namespace CryptonightProfitSwitcher.ProfitPoviders
{
    public class MinerRocksApi : IPoolProfitProvider
    {
        public Dictionary<string, Profit> GetProfits(DirectoryInfo appRootFolder, Settings settings, IList<Coin> coins)
        {
            var poolProfitsDictionary = new Dictionary<string, Profit>();

            foreach(var coin in coins)
            {
                try
                {
                    string apiUrl = GetApiUrl(coin);
                    if (!String.IsNullOrEmpty(apiUrl))
                    {
                        var profitsJson = Helpers.GetJsonFromUrl(apiUrl, settings, appRootFolder);
                        dynamic lastStats = JObject.Parse(profitsJson);
                        ProfitTimeframe timeFrame = coin.OverrideProfitTimeframe.HasValue ? coin.OverrideProfitTimeframe.Value : settings.ProfitTimeframe;
                        decimal diff;
                        switch (timeFrame)
                        {
                            case ProfitTimeframe.Live:
                                diff = lastStats.network.difficulty;
                                break;
                            case ProfitTimeframe.Day:
                                diff = lastStats.pool.stats.diffs["wavg24h"];
                                break;
                            default:
                                throw new NotImplementedException("Unsupported profit time frame for MinerRocksApi: " + timeFrame);
                        }

                        decimal reward = lastStats.network.reward;
                        decimal profit = (coin.GetExpectedHashrate(settings) * (86400 / diff)) * reward;

                        // Get amount of coins
                        decimal coinUnits = lastStats.config.coinUnits;
                        decimal amount = profit / coinUnits;
                        //Get usd price
                        decimal usdPrice = lastStats.coinPrice["coin-usd"];
                        //Multiplicate
                        decimal usdRewardDec = amount * usdPrice;

                        double usdReward = (double)usdRewardDec;

                        poolProfitsDictionary[coin.TickerSymbol] = new Profit(usdReward, ProfitProvider.MinerRocksApi, timeFrame);

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldn't get profits data for {coin.DisplayName} from MinerRocksApi: " + ex.Message);
                }
            }

            return poolProfitsDictionary;
        }

        private string GetApiUrl(Coin coin)
        {
            switch(coin.TickerSymbol)
            {
                case "ETN":
                    return "https://etn.miner.rocks/api/stats";
                case "GRFT":
                    return "https://graft.miner.rocks/api/stats";
                case "ITNS":
                    return "https://itns.miner.rocks/api/stats";
                case "MSR":
                    return "https://masari.miner.rocks/api/stats";
                case "XMR":
                    return "https://monero.miner.rocks/api/stats";
                case "XMV":
                    return "https://monerov.miner.rocks/api/stats";
                case "XTL":
                    return "https://stellite.miner.rocks/api/stats";
                case "LOKI":
                    return "https://loki.miner.rocks/api/stats";
                case "XHV":
                    return "https://haven.miner.rocks/api/stats";
                case "XRN":
                    return "https://saronite.miner.rocks/api/stats";
                case "RYO":
                    return "https://ryo.miner.rocks/api/stats";
                case "AEON":
                    return "https://aeon.miner.rocks/api/stats";
            }
            return null;
        }
    }
}
