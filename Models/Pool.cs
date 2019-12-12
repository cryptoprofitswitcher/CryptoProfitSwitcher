using System;
using CryptoProfitSwitcher.Enums;

namespace CryptoProfitSwitcher.Models
{
    public class Pool
    {
        public string UniqueName { get; set; }
        public bool Enabled { get; set; }
        public ProfitTimeframe ProfitTimeframe { get; set; }
        public ProfitProvider ProfitProvider { get; set; }
        public string ProfitProviderInfo { get; set; }
        public double PreferFactor { get; set; }
        public string PoolUrl { get; set; }
        public string PoolUser { get; set; }
        public string PoolPassword { get; set; }

        public override int GetHashCode()
        {
            return string.GetHashCode(UniqueName, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is Pool pool2 && string.Equals(this.UniqueName, pool2.UniqueName, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return UniqueName;
        }
    }
}
