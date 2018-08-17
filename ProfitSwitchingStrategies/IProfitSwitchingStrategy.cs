using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using System.Collections.Generic;

namespace CryptonightProfitSwitcher.ProfitSwitchingStrategies
{
    public interface IProfitSwitchingStrategy
    {
        MineableRewardResult GetBestPoolminedCoin(IList<Coin> coins, Mineable currentMineable, Dictionary<ProfitProvider, Dictionary<string, Profit>> poolProfitsDictionary, Settings settings);
        MineableRewardResult GetBestNicehashAlgorithm(IList<NicehashAlgorithm> nicehashAlgorithms,Mineable currentMineable, Dictionary<int, Profit> nicehashProfitsDictionary, Settings settings);
        MineableReward GetBestMineable(MineableReward bestPoolminedCoin, MineableReward bestNicehashAlgorithm, MineableReward currentReward, Settings settings);
        double GetReward(Profit profit, Mineable mineable, ProfitTimeframe timeframe);
    }
}
