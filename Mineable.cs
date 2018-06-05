using System;

namespace CryptonightProfitSwitcher
{
    public abstract class Mineable
    {
        public string DisplayName { get; set; }
        public string XmrStakPath { get; set; }
        public string ConfigPath { get; set; }
        public string CpuPath { get; set; }
        public string AmdPath { get; set; }
        public string NvidiaPath { get; set; }
        public Algorithm Algorithm { get; set; }
        public abstract string Id { get; }
        public int? OverrideExpectedHashrate { get; set; }
        public string PoolsPath { get; set; }
        public string PoolAddress { get; set; }
        public string PoolWalletAddress { get; set; }
        public string PoolPassword { get; set; }
        public bool PoolUseTls { get; set; }
        public string PoolTlsFingerprint { get; set; }
        public int PoolWeight { get; set; }
        public string PoolRigId { get; set; }

        public int GetExpectedHashrate(Settings settings)
        {
            if (OverrideExpectedHashrate.HasValue)
            {
                return OverrideExpectedHashrate.Value;
            }
            switch (Algorithm)
            {
                case Algorithm.CryptonightV7:
                    return settings.CryptonightV7Hashrate;
                case Algorithm.CryptonightHeavy:
                    return settings.CryptonightHeavyHashrate;
                case Algorithm.CryptonightLite:
                    return settings.CryptonightLiteHashrate;
                case Algorithm.CryptonightBittube:
                    return settings.CryptonightBittubeHashrate;
                default:
                    throw new NotImplementedException("Can't get expected hashrate for algorithm: " + Algorithm);
            }
        }
    }
}
