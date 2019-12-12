namespace CryptoProfitSwitcher.Models
{
    public class DeviceStatus
    {
        public string Name { get; set; }
        public Pool Pool { get; set; }
        public double Hashrate { get; set; }
        public Profit Profit { get; set; }

        public DeviceStatus(string name, Pool pool, double hashrate, Profit profit)
        {
            Name = name;
            Pool = pool;
            Hashrate = hashrate;
            Profit = profit;
        }
    }
}
