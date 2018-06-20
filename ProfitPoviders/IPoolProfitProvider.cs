using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace CryptonightProfitSwitcher.ProfitPoviders
{
    public interface IPoolProfitProvider
    {
        Dictionary<string, Profit> GetProfits(DirectoryInfo appRootFolder, Settings settings, IList<Coin> coins, CancellationToken ct);
    }
}
