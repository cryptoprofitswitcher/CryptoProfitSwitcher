using CryptonightProfitSwitcher.Enums;
using System;

namespace CryptonightProfitSwitcher.Mineables
{
    public abstract class Mineable
    {
        public string DisplayName { get; set; }
        public double? PreferFactor { get; set; }
        public string PrepareScript { get; set; }
        public bool? Enabled { get; set; }
        public string XmrStakPath { get; set; }
        public string ConfigPath { get; set; }
        public int XmrStakApiPort { get; set; }
        public string CpuPath { get; set; }
        public string AmdPath { get; set; }
        public string NvidiaPath { get; set; }
        public Algorithm Algorithm { get; set; }
        public int? OverrideExpectedHashrate { get; set; }
        public ProfitTimeframe? OverrideProfitTimeframe { get; set; }
        public string PoolsPath { get; set; }
        public string PoolAddress { get; set; }
        public string PoolWalletAddress { get; set; }
        public string PoolPassword { get; set; }
        public bool PoolUseTls { get; set; }
        public string PoolTlsFingerprint { get; set; }
        public int PoolWeight { get; set; }
        public string PoolRigId { get; set; }
        public Miner Miner { get; set; }
        public string CastXmrPath { get; set; }
        public string CastXmrExtraArguments { get; set; }
        public bool CastXmrUseXmrStakCPUMining { get; set; }
        public string SRBMinerPath { get; set; }
        public string SRBMinerConfigPath { get; set; }
        public string SRBMinerPoolsPath { get; set; }
        public int SRBMinerApiPort { get; set; }
        public bool SRBMinerUseXmrStakCPUMining { get; set; }
        public abstract string Id { get; }

        public bool IsEnabled()
        {
            return Enabled ?? true;
        }
        public int GetExpectedHashrate(Settings settings)
        {
            if (OverrideExpectedHashrate.HasValue)
            {
                return OverrideExpectedHashrate.Value;
            }
            switch (Algorithm)
            {
                case Algorithm.CryptonightV7:
                case Algorithm.CryptonightStellite:
                    return settings.CryptonightV7Hashrate;
                case Algorithm.CryptonightHeavy:
                case Algorithm.CryptonightHaven:
                    return settings.CryptonightHeavyHashrate;
                case Algorithm.CryptonightBittube:
                    // Backwards compatibilty
                    if (settings.CryptonightBittubeHashrate > 0)
                    {
                        return settings.CryptonightBittubeHashrate;
                    }
                    return settings.CryptonightLiteHashrate;
                case Algorithm.CryptonightLite:
                    return settings.CryptonightLiteHashrate;
                default:
                    throw new NotImplementedException("Can't get expected hashrate for algorithm: " + Algorithm);
            }
        }
    }
}
