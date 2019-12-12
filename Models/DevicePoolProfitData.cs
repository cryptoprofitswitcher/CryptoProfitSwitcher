using System.Collections.Generic;

namespace CryptoProfitSwitcher.Models
{
    public class DevicePoolProfitData
    {
        public Pool Pool { get; set; }
        public Dictionary<string, Profit?> DeviceProfits { get; set; }

        public DevicePoolProfitData(Pool pool)
        {
            Pool = pool;
            DeviceProfits = new Dictionary<string, Profit?>();
        }
    }
}
