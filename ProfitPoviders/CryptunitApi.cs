using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CryptonightProfitSwitcher.ProfitPoviders
{
    internal class CryptunitApi : IPoolProfitProvider
    {
        public Dictionary<string, Profit> GetProfits(DirectoryInfo appRootFolder, Settings settings, IList<Coin> coins, CancellationToken ct)
        {
            var poolProfitsDictionary = new Dictionary<string, Profit>();
            try
            {
                var profitsLiveJson = Helpers.GetJsonFromUrl("https://www.cryptunit.com/api/earnings/?hashrate=10000&device=GPU&dataavg=1h&volumefilter=&algofilter=", settings, appRootFolder, ct);
                var profitsDayJson = Helpers.GetJsonFromUrl("https://www.cryptunit.com/api/earnings/?hashrate=10000&device=GPU&dataavg=24h&volumefilter=&algofilter=", settings, appRootFolder, ct);

                dynamic profitsLive = JsonConvert.DeserializeObject(profitsLiveJson);
                dynamic profitsDay = JsonConvert.DeserializeObject(profitsDayJson);

                foreach (dynamic rewardLive in profitsLive[0].coins)
                {
                    string tickerSymbol = rewardLive.coin_ticker;
                    //Adjust profits based on user defined hashrate
                    Coin matchedCoin = coins.FirstOrDefault(c => c.TickerSymbol == tickerSymbol);

                    if (matchedCoin != null)
                    {
                        double hashrate = rewardLive.hashrate_auto;
                        double rewardUsdLive = rewardLive.reward_day_usd;
                        double rewardCoinsLive = rewardLive.reward_day_coins;
                        foreach (dynamic rewardDay in profitsDay[0].coins)
                        {
                            if (rewardDay.coin_ticker == tickerSymbol)
                            {
                                double rewardUsdDay = rewardDay.reward_day_usd;
                                double rewardCoinsDay = rewardDay.reward_day_coins;
                                ProfitTimeframe timeFrame = matchedCoin.OverrideProfitTimeframe.HasValue ? matchedCoin.OverrideProfitTimeframe.Value : settings.ProfitTimeframe;
                                rewardUsdLive = (rewardUsdLive / hashrate) * matchedCoin.GetExpectedHashrate(settings);
                                rewardCoinsLive = (rewardCoinsLive / hashrate) * matchedCoin.GetExpectedHashrate(settings);
                                rewardUsdDay = (rewardUsdDay / hashrate) * matchedCoin.GetExpectedHashrate(settings);
                                rewardCoinsDay = (rewardCoinsDay / hashrate) * matchedCoin.GetExpectedHashrate(settings);
                                poolProfitsDictionary[tickerSymbol] = new Profit(rewardUsdLive, rewardUsdDay, rewardCoinsLive, rewardCoinsDay, ProfitProvider.CryptunitApi, timeFrame);
                                Console.WriteLine($"Got profit data for {tickerSymbol} from CryptunitAPI");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to get profits data from Cryptunit Api: " + ex.Message);
            }
            return poolProfitsDictionary;
        }
    }
}
