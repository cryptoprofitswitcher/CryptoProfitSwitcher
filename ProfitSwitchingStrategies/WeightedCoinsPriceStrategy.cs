using System;
using System.Collections.Generic;
using System.Linq;
using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;

namespace CryptonightProfitSwitcher.ProfitSwitchingStrategies
{
    public class WeightedCoinsPriceStrategy : IProfitSwitchingStrategy
    {
        public MineableRewardResult GetBestPoolminedCoin(IList<Coin> coins, Mineable currentMineable, Dictionary<ProfitProvider, Dictionary<string, Profit>> poolProfitsDictionary, Settings settings)
        {
            MineableReward currentReward = null;
            Coin bestPoolminedCoin = null;
            double bestPoolminedCoinProfit = 0;
            foreach (var coin in coins.Where(c => c.IsEnabled()))
            {
                var profit = Helpers.GetPoolProfitForCoin(coin, poolProfitsDictionary, settings);
                double reward = GetReward(profit, coin, settings.ProfitTimeframe);
                double calcProfit = coin.PreferFactor.HasValue ? reward * coin.PreferFactor.Value : reward;
                if (currentMineable != null && coin.Id == currentMineable.Id)
                {
                    currentReward = new MineableReward(currentMineable, calcProfit);
                }
                if (bestPoolminedCoin == null || calcProfit > bestPoolminedCoinProfit)
                {
                    bestPoolminedCoinProfit = calcProfit;
                    bestPoolminedCoin = coin;
                }
            }
            return new MineableRewardResult(new MineableReward(bestPoolminedCoin, bestPoolminedCoinProfit), currentReward);
        }

        public MineableRewardResult GetBestNicehashAlgorithm(IList<NicehashAlgorithm> nicehashAlgorithms, Mineable currentMineable, Dictionary<int, Profit> nicehashProfitsDictionary, Settings settings)
        {
            var maximizeFiatStrategy = new MaximizeFiatStrategy();
            return maximizeFiatStrategy.GetBestNicehashAlgorithm(nicehashAlgorithms,currentMineable, nicehashProfitsDictionary, settings);
        }

        public MineableReward GetBestMineable(MineableReward bestPoolminedCoin, MineableReward bestNicehashAlgorithm, MineableReward currentReward, Settings settings)
        {
            if (bestPoolminedCoin.Mineable != null && bestPoolminedCoin.Reward > bestNicehashAlgorithm.Reward)
            {
                Console.WriteLine($"Determined best mining method: Mine {bestPoolminedCoin.Mineable.DisplayName} in a pool at {Helpers.ToCurrency(bestPoolminedCoin.Reward, "$")} per day.");
                if (currentReward != null && settings.ProfitSwitchThreshold > 0 && currentReward.Mineable.Id != bestPoolminedCoin.Mineable.Id)
                {
                    if (currentReward.Reward * (1 + settings.ProfitSwitchThreshold) > bestPoolminedCoin.Reward)
                    {
                        Console.WriteLine($"But will stay mining {currentReward.Mineable.DisplayName} because of the profit switch threshold.");
                        return currentReward;
                    }
                }
                return bestPoolminedCoin;
            }
            else if (bestNicehashAlgorithm.Mineable != null)
            {
                Console.WriteLine($"Determined best mining method: Provide hash power for {bestNicehashAlgorithm.Mineable.DisplayName} on NiceHash at {Helpers.ToCurrency(bestNicehashAlgorithm.Reward, "$")} per day.");
                if (currentReward != null && settings.ProfitSwitchThreshold > 0 && currentReward.Mineable.Id != bestNicehashAlgorithm.Mineable.Id)
                {
                    if (currentReward.Reward * (1 + settings.ProfitSwitchThreshold) > bestNicehashAlgorithm.Reward)
                    {
                        Console.WriteLine($"But will stay mining {currentReward.Mineable.DisplayName} because of the profit switch threshold.");
                        return currentReward;
                    }
                }
                return bestNicehashAlgorithm;
            }
            else
            {
                Console.WriteLine("Couldn't determine best mining method.");
                return null;
            }
        }

        public double GetReward(Profit profit, Mineable mineable, ProfitTimeframe timeframe)
        {
            timeframe = mineable.OverrideProfitTimeframe.HasValue ? mineable.OverrideProfitTimeframe.Value : timeframe;
            double reward = 0;
            switch (timeframe)
            {
                case ProfitTimeframe.Day:
                    reward = profit.UsdRewardDay;
                    break;
                default:
                    reward = profit.UsdRewardLive;
                    break;
            }
            if (profit.CoinRewardDay > 0 && profit.CoinRewardLive > 0)
            {
                reward *= (profit.CoinRewardLive / profit.CoinRewardDay);
            }
            return reward;
        }
    }
}
