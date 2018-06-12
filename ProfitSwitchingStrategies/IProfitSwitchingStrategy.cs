using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using System.Collections.Generic;

namespace CryptonightProfitSwitcher.ProfitSwitchingStrategies
{
    public interface IProfitSwitchingStrategy
    {
        MineableReward GetBestPoolminedCoin(IList<Coin> coins, Dictionary<ProfitProvider, Dictionary<string, Profit>> poolProfitsDictionary, Settings settings);
        MineableReward GetBestNicehashAlgorithm(IList<NicehashAlgorithm> nicehashAlgorithms, Dictionary<int, Profit> nicehashProfitsDictionary, Settings settings);
        MineableReward GetBestMineable(MineableReward bestPoolminedCoin, MineableReward bestNicehashAlgorithm);
        double GetReward(Profit profit, Mineable mineable, ProfitTimeframe timeframe);
    }
}
