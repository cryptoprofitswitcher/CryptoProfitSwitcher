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
        public MineableReward GetBestPoolminedCoin(IList<Coin> coins, Dictionary<ProfitProvider, Dictionary<string, Profit>> poolProfitsDictionary, Settings settings)
        {
            Coin bestPoolminedCoin = null;
            double bestPoolminedCoinProfit = 0;
            foreach (var coin in coins.Where(c => c.IsEnabled()))
            {
                var profit = Helpers.GetPoolProfitForCoin(coin, poolProfitsDictionary, settings);
                double reward = GetReward(profit, coin, settings.ProfitTimeframe);
                double calcProfit = coin.PreferFactor.HasValue ? reward * coin.PreferFactor.Value : reward;
                if (bestPoolminedCoin == null || calcProfit > bestPoolminedCoinProfit)
                {
                    bestPoolminedCoinProfit = calcProfit;
                    bestPoolminedCoin = coin;
                }
            }
            return new MineableReward(bestPoolminedCoin, bestPoolminedCoinProfit);
        }

        public MineableReward GetBestNicehashAlgorithm(IList<NicehashAlgorithm> nicehashAlgorithms, Dictionary<int, Profit> nicehashProfitsDictionary, Settings settings)
        {
            var maximizeFiatStrategy = new MaximizeFiatStrategy();
            return maximizeFiatStrategy.GetBestNicehashAlgorithm(nicehashAlgorithms, nicehashProfitsDictionary, settings);
        }

        public MineableReward GetBestMineable(MineableReward bestPoolminedCoin, MineableReward bestNicehashAlgorithm)
        {
            if (bestPoolminedCoin.Mineable != null && bestPoolminedCoin.Reward > bestNicehashAlgorithm.Reward)
            {
                Console.WriteLine($"Determined best mining method: Mine {bestPoolminedCoin.Mineable.DisplayName} in a pool at {Helpers.ToCurrency(bestPoolminedCoin.Reward, "$")} per day.");
                return bestPoolminedCoin;
            }
            else if (bestNicehashAlgorithm.Mineable != null)
            {
                Console.WriteLine($"Determined best mining method: Provide hash power for {bestNicehashAlgorithm.Mineable.DisplayName} on NiceHash at {Helpers.ToCurrency(bestNicehashAlgorithm.Reward, "$")} per day.");
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
