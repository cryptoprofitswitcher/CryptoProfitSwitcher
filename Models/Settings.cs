using CryptonightProfitSwitcher.Enums;
using System;

namespace CryptonightProfitSwitcher.Models
{
    public class Settings
    {
        public int CryptonightV7Hashrate { get; set; }
        public int CryptonightHeavyHashrate { get; set; }
        public int CryptonightLiteHashrate { get; set; }
        public ProfitTimeframe ProfitTimeframe { get; set; }
        public ProfitSwitchingStrategy ProfitSwitchingStrategy { get; set; }
        public SortingMode ProfitSorting { get; set; }
        public string PoolProfitProviders { get; set; }
        public int MinerStartDelay { get; set; }
        public int ProfitSwitchCooldown { get; set; }
        public double ProfitSwitchThreshold { get; set; }
        public bool StartMinerMinimized { get; set; }
        public int ProfitCheckInterval { get; set; }
        public int DisplayUpdateInterval { get; set; }
        public string ResetScript { get; set; }
        public bool EnableCaching { get; set; }
        public bool EnableLogging { get; set; }
        public bool EnableWatchdog { get; set; }
        public int WatchdogDelay { get; set; }
        public double WatchdogCriticalThreshold { get; set; }
        public int WatchdogInterval { get; set; }
        public int WatchdogAllowedOversteps { get; set; }
        [Obsolete("CryptonightBittubeHashrate is deprecated, use CryptonightLiteHashrate or the override in Mineable.")]
        public int CryptonightBittubeHashrate { get; set; }

        [Obsolete("NicehashPreferFactor is deprecated, please use PreferFactor from Mineable.")]
        public double NicehashPreferFactor { get; set; }

        [Obsolete("XmrStakApiPort in Settings is deprecated, please use port from Mineable.")]
        public int XmrStakApiPort { get; set; }
    }
}
