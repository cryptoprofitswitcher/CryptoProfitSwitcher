using System.Collections.Generic;
using CryptoProfitSwitcher.Models;

namespace CryptoProfitSwitcher.Miners
{
    public interface IMiner
    {
        HashSet<DeviceConfig> DeviceConfigs { get; set; }
        Pool Pool { get; set; }
        void StartMiner(bool minimized);
        void StopMiner();
        double GetCurrentHashrate(DeviceConfig deviceConfig);
        string Name { get; }
        bool SupportsIndividualHashrate { get; }
    }
}
