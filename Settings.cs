namespace CryptonightProfitSwitcher
{
    public class Settings
    {
        public int CryptonightV7Hashrate { get; set; }
        public int CryptonightHeavyHashrate { get; set; }
        public int CryptonightLiteHashrate { get; set; }
        public int CryptonightBittubeHashrate { get; set; }
        public double NicehashPreferFactor { get; set; }
        public ProfitTimeframe ProfitTimeframe { get; set; }
        public int XmrStakApiPort { get; set; }
        public int MinerStartDelay { get; set; }
        public int ProfitCheckInterval { get; set; }
        public int DisplayUpdateInterval { get; set; }
        public string ResetScript { get; set; }
        public bool EnableCaching { get; set; }
        public bool EnableWatchdog { get; set; }
        public int WatchdogDelay { get; set; }
        public double WatchdogCriticalThreshold { get; set; }
        public int WatchdogInterval { get; set; }
        public int WatchdogAllowedOversteps { get; set; }


    }
}
