using System.Collections.Generic;
using System.Diagnostics;
using CryptoProfitSwitcher.Enums;

namespace CryptoProfitSwitcher.Models
{
    public class Config
    {
        public ProcessPriorityClass ProcessPriority { get; set; }
        public ProfitSwitchingStrategy ProfitSwitchStrategy { get; set; }
        public bool EnableCaching { get; set; }
        public bool EnableLogging { get; set; }
        public bool EnableManualModeByDefault { get; set; }
        public bool DisableBenchmarking { get; set; }
        public bool DisableDownloadMiners { get; set; }
        public bool StartMinerMinimized { get; set; }
        public int MinerStartDelay { get; set; }
        public int DisplayUpdateInterval { get; set; }
        public int ProfitCheckInterval { get; set; }
        public int ProfitSwitchInterval { get; set; }
        public double ProfitSwitchThreshold { get; set; }

        public List<Algorithm> Algorithms { get; set; }
    }
}
