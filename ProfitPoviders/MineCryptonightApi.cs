using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CryptonightProfitSwitcher.ProfitPoviders
{
    public class MineCryptonightApi : IPoolProfitProvider
    {
        public Dictionary<string,Profit> GetProfits(DirectoryInfo appRootFolder, Settings settings, IList<Coin> coins, CancellationToken ct)
        {
            var poolProfitsDictionary = new Dictionary<string, Profit>();
            try
            {
                var profitsJson = Helpers.GetJsonFromUrl("https://minecryptonight.net/api/rewards?hr=1000", settings, appRootFolder, ct);
                dynamic profits = JsonConvert.DeserializeObject<ExpandoObject>(profitsJson, new ExpandoObjectConverter());
                long baseHashrate = profits.hash_rate;
                double cnV2Factor = Helpers.GetProperty<double>(profits, "cryptonight-v2_factor");
                double cnFastFactor = Helpers.GetProperty<double>(profits, "cryptonight-fast_factor");
                double cnHeavyFactor = Helpers.GetProperty<double>(profits, "cryptonight-heavy_factor");
                double cnLiteFactor = Helpers.GetProperty<double>(profits, "cryptonight-lite_factor");


                foreach (dynamic reward in profits.rewards)
                {
                    string tickerSymbol = reward.ticker_symbol;
                    string algorithm = reward.algorithm;
                    double rewardUsd = reward.reward_24h.usd;
                    double rewardCoins = reward.reward_24h.coins;

                    //Adjust profits based on user defined hashrate
                    Coin matchedCoin = coins.FirstOrDefault(c => c.TickerSymbol == tickerSymbol);

                    if (matchedCoin != null)
                    {
                        switch (algorithm)
                        {
                            case "cryptonight-v2":
                                rewardUsd = (rewardUsd / (1000 * cnV2Factor)) * matchedCoin.GetExpectedHashrate(settings);
                                rewardCoins = (rewardCoins / (1000 * cnV2Factor)) * matchedCoin.GetExpectedHashrate(settings);
                                break;
                            case "cryptonight-saber":
                            case "cryptonight-heavy":
                                rewardUsd = (rewardUsd / (1000 * cnHeavyFactor)) * matchedCoin.GetExpectedHashrate(settings);
                                rewardCoins = (rewardCoins / (1000 * cnHeavyFactor)) * matchedCoin.GetExpectedHashrate(settings);
                                break;
                            case "cryptonight-lite-v1":
                                rewardUsd = (rewardUsd / (1000 * cnLiteFactor)) * matchedCoin.GetExpectedHashrate(settings);
                                rewardCoins = (rewardCoins / (1000 * cnLiteFactor)) * matchedCoin.GetExpectedHashrate(settings);
                                break;
                            case "cryptonight-fast":
                                rewardUsd = (rewardUsd / (1000 * cnFastFactor)) * matchedCoin.GetExpectedHashrate(settings);
                                rewardCoins = (rewardCoins / (1000 * cnFastFactor)) * matchedCoin.GetExpectedHashrate(settings);
                                break;
                            default:
                                rewardUsd = (rewardUsd / 1000) * matchedCoin.GetExpectedHashrate(settings);
                                rewardCoins = (rewardCoins / 1000) * matchedCoin.GetExpectedHashrate(settings);
                                break;
                        }
                        poolProfitsDictionary[tickerSymbol] = new Profit(rewardUsd,0,rewardCoins,0, ProfitProvider.MineCryptonightApi, ProfitTimeframe.Live);
                        Console.WriteLine($"Got profit data for {tickerSymbol} from MineCryptonightAPI");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to get profits data from MineCryptonight Api: " + ex.Message);
            }
            return poolProfitsDictionary;
        }
    }
}
