using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.ProfitSwitchingStrategies;

namespace CryptonightProfitSwitcher.Factories
{
    public static class ProfitSwitchingStrategyFactory
    {
        public static IProfitSwitchingStrategy GetProfitSwitchingStrategy(ProfitSwitchingStrategy strategy)
        {
            switch (strategy)
            {
                case ProfitSwitchingStrategy.MaximizeCoins:
                    return new MaximizeCoinsStrategy();
                case ProfitSwitchingStrategy.WeightedCoinsPrice:
                    return new WeightedCoinsPriceStrategy();
                default:
                    return new MaximizeFiatStrategy();
            }
        }
    }
}
