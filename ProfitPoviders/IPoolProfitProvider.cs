using CryptonightProfitSwitcher.Mineables;
using System.Collections.Generic;
using System.IO;

namespace CryptonightProfitSwitcher.ProfitPoviders
{
    public interface IPoolProfitProvider
    {
        Dictionary<string, Profit> GetProfits(DirectoryInfo appRootFolder, Settings settings, IList<Coin> coins);
    }
}
