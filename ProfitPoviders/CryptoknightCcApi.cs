using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using Newtonsoft.Json.Linq;

namespace CryptonightProfitSwitcher.ProfitPoviders
{
    public class CryptoknightCcApi : IPoolProfitProvider
    {
        public Dictionary<string, Profit> GetProfits(DirectoryInfo appRootFolder, Settings settings, IList<Coin> coins)
        {
            var poolProfitsDictionary = new Dictionary<string, Profit>();

            List<Task> tasks = new List<Task>();
            foreach(var coin in coins)
            {
                tasks.Add(SetProfitForCoinTask(coin, settings, appRootFolder, poolProfitsDictionary));
            }
            Task.WhenAll(tasks).Wait();
            return poolProfitsDictionary;
        }

        Task SetProfitForCoinTask (Coin coin, Settings settings, DirectoryInfo appRootFolder, Dictionary<string, Profit> poolProfitsDictionary)
        {
            return Task.Run(() =>
            {
                try
                {
                    string apiUrl = GetApiUrl(coin);
                    if (!String.IsNullOrEmpty(apiUrl))
                    {
                        var profitsJson = Helpers.GetJsonFromUrl(apiUrl, settings, appRootFolder);
                        dynamic lastStats = JObject.Parse(profitsJson);

                        ProfitTimeframe timeFrame = coin.OverrideProfitTimeframe.HasValue ? coin.OverrideProfitTimeframe.Value : settings.ProfitTimeframe;

                        // Get live profit
                        decimal diffLive = lastStats.network.difficulty;
                        decimal reward = lastStats.network.reward;
                        decimal profitLive = (coin.GetExpectedHashrate(settings) * (86400 / diffLive)) * reward;

                        // Get amount of coins
                        decimal coinUnits = lastStats.config.coinUnits;
                        decimal amountLive = profitLive / coinUnits;

                        //Get usd price
                        JArray usdPrices = lastStats.charts.priceUSD;
                        JArray lastUsdPriceData = usdPrices.Last().ToObject<JArray>();
                        decimal lastUsdPrice = lastUsdPriceData[1].ToObject<decimal>();

                        //Multiplicate
                        decimal usdRewardDecLive = amountLive * lastUsdPrice;
                        double usdRewardLive = (double)usdRewardDecLive;

                        //Get day profit
                        double amountDay = 0;
                        double usdRewardDay = 0;
                        JArray difficulties = lastStats.charts.difficulty;
                        List<decimal> validDifficulties = new List<decimal>();
                        foreach (var diffToken in difficulties)
                        {
                            var diffData = diffToken.ToObject<JArray>();
                            long unixEpochTime = diffData[0].ToObject<long>();
                            var date = DateTimeOffset.FromUnixTimeSeconds(unixEpochTime);
                            if (date.UtcDateTime > DateTimeOffset.UtcNow.AddDays(-1))
                            {
                                decimal validDiff = diffData[1].ToObject<decimal>();
                                validDifficulties.Add(validDiff);
                            }
                        }
                        if (validDifficulties.Count > 0)
                        {
                            List<decimal> validPrices = new List<decimal>();
                            foreach (var usdPriceToken in usdPrices)
                            {
                                var priceData = usdPriceToken.ToObject<JArray>();
                                long unixEpochTime = priceData[0].ToObject<long>();
                                var date = DateTimeOffset.FromUnixTimeSeconds(unixEpochTime);
                                if (date.UtcDateTime > DateTimeOffset.UtcNow.AddDays(-1))
                                {
                                    decimal validPrice = priceData[1].ToObject<decimal>();
                                    validPrices.Add(validPrice);
                                }
                            }

                            if (validPrices.Count > 0)
                            {
                                decimal averagePrice = validPrices.Sum() / validPrices.Count;
                                decimal averageDiff = validDifficulties.Sum() / validDifficulties.Count;
                                decimal profitDay = (coin.GetExpectedHashrate(settings) * (86400 / averageDiff)) * reward;
                                decimal amountDayDec = profitDay / coinUnits;
                                amountDay = (double)amountDayDec;
                                decimal usdRewardDecDay = amountDayDec * averagePrice;
                                usdRewardDay = (double)usdRewardDecDay;
                            }
                        }

                        poolProfitsDictionary[coin.TickerSymbol] = new Profit(usdRewardLive, usdRewardDay, (double)amountLive, amountDay, ProfitProvider.CryptoknightCCApi, timeFrame);
                        Console.WriteLine($"Got profit data for {coin.TickerSymbol} from CryptonightCCAPI");

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldn't get profits data for {coin.DisplayName} from CryptoknightCCApi: " + ex.Message);
                }
            });
        }

        private string GetApiUrl(Coin coin)
        {
            switch(coin.TickerSymbol)
            {
                case "ETN":
                    return "https://cryptoknight.cc/rpc/etn/live_stats";
                case "GRFT":
                case "GRF":
                    return "https://cryptoknight.cc/rpc/graft/live_stats";
                case "ITNS":
                    return "https://cryptoknight.cc/rpc/itns/live_stats";
                case "MSR":
                    return "https://cryptoknight.cc/rpc/msr/live_stats";
                case "XMV":
                    return "https://cryptoknight.cc/rpc/monerov/live_stats";
                case "XTL":
                    return "https://cryptoknight.cc/rpc/stellite/live_stats";
                case "LOKI":
                case "LOK":
                    return "https://cryptoknight.cc/rpc/loki/live_stats";
                case "XHV":
                    return "https://cryptoknight.cc/rpc/haven/live_stats";
                case "XRN":
                    return "https://cryptoknight.cc/rpc/saronite/live_stats";
                case "AEON":
                    return "https://cryptoknight.cc/rpc/aeon/live_stats";
                case "XAO":
                    return "https://cryptoknight.cc/rpc/alloy/live_stats";
                case "ARQ":
                    return "https://cryptoknight.cc/rpc/arq/live_stats";
                case "RTO":
                    return "https://cryptoknight.cc/rpc/arto/live_stats";
                case "B2B":
                    return "https://cryptoknight.cc/rpc/b2b/live_stats";
                case "BBS":
                    return "https://cryptoknight.cc/rpc/bbs/live_stats";
                case "BTCN":
                    return "https://cryptoknight.cc/rpc/btcn/live_stats";
                case "IPBC":
                case "TUBE":
                    return "https://cryptoknight.cc/rpc/ipbc/live_stats";
                case "CREP":
                    return "https://cryptoknight.cc/rpc/crep/live_stats";
                case "EDL":
                    return "https://cryptoknight.cc/rpc/edl/live_stats";
                case "ELYA":
                    return "https://cryptoknight.cc/rpc/elya/live_stats";
                case "IRD":
                    return "https://cryptoknight.cc/rpc/iridium/live_stats";
                case "ITA":
                    return "https://cryptoknight.cc/rpc/italo/live_stats";
                case "KRB":
                    return "https://cryptoknight.cc/rpc/karbo/live_stats";
                case "LNS":
                    return "https://cryptoknight.cc/rpc/lines/live_stats";
                case "NBR":
                    return "https://cryptoknight.cc/rpc/niobio/live_stats";
                case "OMB":
                    return "https://cryptoknight.cc/rpc/ombre/live_stats";
                case "QWC":
                    return "https://cryptoknight.cc/rpc/qwerty/live_stats";
                case "SOL":
                    return "https://cryptoknight.cc/rpc/solace/live_stats";
                case "SUMO":
                    return "https://cryptoknight.cc/rpc/sumo/live_stats";
                case "TRIT":
                    return "https://cryptoknight.cc/rpc/triton/live_stats";
                case "TRTL":
                    return "https://cryptoknight.cc/rpc/turtle/live_stats";
                case "XUN":
                    return "https://cryptoknight.cc/rpc/xun/live_stats";
                case "WOW":
                    return "https://cryptoknight.cc/rpc/wownero/live_stats";
            }
            return null;
        }
    }
}
