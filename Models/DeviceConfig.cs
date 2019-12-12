using CryptoProfitSwitcher.Enums;
using Newtonsoft.Json;

namespace CryptoProfitSwitcher.Models
{
    public class DeviceConfig
    {
        public DeviceType DeviceType { get; set; }
        public string DeviceId { get; set; }
        public bool Enabled { get; set; }
        public double ExpectedHashrate { get; set; }
        public string PrepareScript { get; set; }
        public Miner Miner { get; set; }
        public string MinerPath { get; set; }
        public string MinerArguments { get; set; }
        public string MinerDeviceSpecificArguments { get; set; }
        public int MinerApiPort { get; set; }

        [JsonIgnore]
        internal string FullDeviceId => DeviceType + DeviceId;

        private string GetHashString()
        {
            return DeviceType + DeviceId + Miner + ExpectedHashrate + MinerPath + MinerArguments + MinerDeviceSpecificArguments;
        }

        public override int GetHashCode()
        {
            string id = GetHashString();
            return id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            string id = GetHashString();
            return obj is DeviceConfig deviceConfig2 && string.Equals(deviceConfig2.GetHashString(), id);
        }

        public override string ToString()
        {
            return FullDeviceId;
        }
    }
}
