using System.Collections.Generic;

namespace CryptoProfitSwitcher.Models
{
    public class Algorithm
    {
        public string DisplayName { get; set; }
        public bool Enabled { get; set; }
        public List<DeviceConfig> DeviceConfigs { get; set; }
        public List<Pool> Pools { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
