using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.ProfitSwitchingStrategies;

namespace CryptoProfitSwitcher.Factories
{
    public static class ProfitSwitchingStrategyFactory
    {
        public static IProfitSwitchingStrategy GetProfitSwitchingStrategy(ProfitSwitchingStrategy strategy)
        {
            switch (strategy)
            {
                case ProfitSwitchingStrategy.PreferLowDifficulty:
                    return new PreferLowDifficultyStrategy();
                default:
                    return new MaximizeFiatStrategy();
            }
        }
    }
}
