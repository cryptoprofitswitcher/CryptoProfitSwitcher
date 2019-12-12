using CryptoProfitSwitcher.Miners;

namespace CryptoProfitSwitcher.Models
{
    public class MiningConfig
    {
        public DeviceConfig DeviceConfig { get; set; }
        public Pool Pool { get; set; }
        public IMiner Miner { get; set; }

        public MiningConfig(DeviceConfig deviceConfig, Pool pool)
        {
            DeviceConfig = deviceConfig;
            Pool = pool;
        }
    }
}
