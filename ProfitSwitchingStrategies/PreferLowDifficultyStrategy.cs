using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.Models;

namespace CryptoProfitSwitcher.ProfitSwitchingStrategies
{
    public class PreferLowDifficultyStrategy : IProfitSwitchingStrategy
    {
        public bool IsProfitABetterThanB(Profit profitA, ProfitTimeframe timeframeA, Profit profitB, ProfitTimeframe timeframeB, double threshold)
        {
            double relativeRewardA = GetReward(profitA, timeframeA);
            double relativeRewardB = GetReward(profitB, timeframeB);
            return relativeRewardA > relativeRewardB + (relativeRewardB * threshold);
        }

        private double GetReward(Profit profit, ProfitTimeframe timeframe)
        {
            double reward = 0;
            switch (timeframe)
            {
                case ProfitTimeframe.Day:
                    reward = profit.UsdRewardDay;
                    break;
                case ProfitTimeframe.Live:
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
