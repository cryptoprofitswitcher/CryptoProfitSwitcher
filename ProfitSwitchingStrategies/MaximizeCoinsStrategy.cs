using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;

namespace CryptonightProfitSwitcher.ProfitSwitchingStrategies
{
    public class MaximizeCoinsStrategy : IProfitSwitchingStrategy
    {
        public MineableReward GetBestPoolminedCoin(IList<Coin> coins, Dictionary<ProfitProvider, Dictionary<string, Profit>> poolProfitsDictionary, Settings settings)
        {
            Coin bestPoolminedCoin = null;
            double bestPoolminedCoinProfit = 0;
            foreach (var coin in coins.Where(c => c.IsEnabled()))
            {
                var profit = Helpers.GetPoolProfitForCoin(coin, poolProfitsDictionary, settings);
                double calcProfit = coin.PreferFactor.HasValue ? GetRelativeReward(profit) * coin.PreferFactor.Value : GetRelativeReward(profit);
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
            Console.WriteLine("This ProfitSwitchingStrategy is incompatible with NiceHash algorithms!");
            return null;
        }

        public MineableReward GetBestMineable(MineableReward bestPoolminedCoin, MineableReward bestNicehashAlgorithm)
        {
            if (bestPoolminedCoin.Mineable != null)
            {
                Console.WriteLine($"Determined best mining method: Mine {bestPoolminedCoin.Mineable.DisplayName} in a pool with relative factor {bestPoolminedCoin.Reward}.");
                return bestPoolminedCoin;
            }
            else
            {
                Console.WriteLine("Couldn't determine best mining method.");
                return null;
            }
        }

        private double GetRelativeReward(Profit profit)
        {
            if (profit.CoinRewardDay > 0 && profit.CoinRewardLive > 0)
            {
                double relativeReward = profit.CoinRewardLive / profit.CoinRewardDay;
                return relativeReward;
            }
            return 0;
        }


    }
}
