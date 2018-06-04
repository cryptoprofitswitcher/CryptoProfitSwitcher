namespace CryptonightProfitSwitcher
{
    public abstract class Mineable
    {
        public string DisplayName { get; set; }
        public string XmrStakPath { get; set; }
        public string PoolsPath { get; set; }
        public string ConfigPath { get; set; }
        public string CpuPath { get; set; }
        public string AmdPath { get; set; }
        public string NvidiaPath { get; set; }
        public Algorithm Algorithm { get; set; }
        public abstract string Id { get; }

    }
}
