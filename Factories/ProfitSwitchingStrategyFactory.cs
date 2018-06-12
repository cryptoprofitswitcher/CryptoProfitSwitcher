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
                default:
                    return new MaximizeFiatStrategy();
            }
        }
    }
}
