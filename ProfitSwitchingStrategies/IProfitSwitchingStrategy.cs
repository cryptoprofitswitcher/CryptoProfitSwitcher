using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.Models;

namespace CryptoProfitSwitcher.ProfitSwitchingStrategies
{
    public interface IProfitSwitchingStrategy
    {
        bool IsProfitABetterThanB(Profit profitA, ProfitTimeframe timeframeA,  Profit profitB, ProfitTimeframe timeframeB,  double threshold);
    }
}
