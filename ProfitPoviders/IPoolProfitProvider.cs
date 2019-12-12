using System.Collections.Generic;
using System.IO;
using System.Threading;
using CryptoProfitSwitcher.Models;

namespace CryptoProfitSwitcher.ProfitPoviders
{
    public interface IPoolProfitProvider
    {
        Dictionary<Pool, Profit> GetProfits(IList<Pool> pools, bool enableCaching, DirectoryInfo appRootFolder, CancellationToken ct);
    }
}
