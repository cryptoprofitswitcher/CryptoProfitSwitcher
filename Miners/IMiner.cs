using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using System.IO;

namespace CryptonightProfitSwitcher.Miners
{
    public interface IMiner
    {
        void StartMiner(Mineable mineable, Settings settings, string appRoot, DirectoryInfo appRootFolder);
        void StopMiner();
        double GetCurrentHashrate(Settings settings, DirectoryInfo appRootFolder);
    }
}
