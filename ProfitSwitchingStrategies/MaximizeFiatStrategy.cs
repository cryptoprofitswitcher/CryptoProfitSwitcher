using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CryptonightProfitSwitcher.ProfitSwitchingStrategies
{
    public class MaximizeFiatStrategy : IProfitSwitchingStrategy
    {
        public MineableReward GetBestPoolminedCoin(IList<Coin> coins, Dictionary<ProfitProvider, Dictionary<string, Profit>> poolProfitsDictionary, Settings settings)
        {
            Coin bestPoolminedCoin = null;
            double bestPoolminedCoinProfit = 0;
            foreach (var coin in coins.Where(c => c.IsEnabled()))
            {
                var profit = Helpers.GetPoolProfitForCoin(coin, poolProfitsDictionary, settings);
                double calcProfit = coin.PreferFactor.HasValue ? GetReward(profit) * coin.PreferFactor.Value : GetReward(profit);
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
            NicehashAlgorithm bestNicehashAlgorithm = null;
            double bestNicehashAlgorithmProfit = 0;
            foreach (var nicehashAlgorithm in nicehashAlgorithms.Where(na => na.IsEnabled()))
            {
                Profit profit = nicehashProfitsDictionary.GetValueOrDefault(nicehashAlgorithm.ApiId, new Profit());
                double preferFactor = 1;
                if (nicehashAlgorithm.PreferFactor.HasValue)
                {
                    preferFactor = nicehashAlgorithm.PreferFactor.Value;
                }
                else
                {
                    //Backwards compatibility
                    if (settings.NicehashPreferFactor >= 0)
                    {
                        preferFactor = settings.NicehashPreferFactor;
                    }
                }
                double calcProfit = GetReward(profit) * preferFactor;
                if (bestNicehashAlgorithm == null || calcProfit > bestNicehashAlgorithmProfit)
                {
                    bestNicehashAlgorithmProfit = calcProfit;
                    bestNicehashAlgorithm = nicehashAlgorithm;
                }
            }
            return new MineableReward(bestNicehashAlgorithm, bestNicehashAlgorithmProfit);
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
        private double GetReward(Profit profit)
        {
            switch (profit.Timeframe)
            {
                case ProfitTimeframe.Live:
                    return profit.UsdRewardLive;
                case ProfitTimeframe.Day:
                    return profit.UsdRewardDay;
                default:
                    throw new NotImplementedException("Unknown ProfitTimeframe: " + profit.Timeframe);
            }
        }
    }
}
