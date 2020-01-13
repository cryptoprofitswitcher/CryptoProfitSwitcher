using System;
using CryptoProfitSwitcher.Enums;

namespace CryptoProfitSwitcher.Models
{
    public struct Profit
    {
        public const int BaseHashrate = 1000;
        public Profit(double usdRewardLive, double usdRewardDay, double coinRewardLive, double coinRewardDay, ProfitProvider source)
        {
            UsdRewardLive = usdRewardLive;
            UsdRewardDay = usdRewardDay;
            CoinRewardLive = coinRewardLive;
            CoinRewardDay = coinRewardDay;
            Source = source;
        }

        public double UsdRewardLive { get; set; }
        public double UsdRewardDay { get; set; }
        public double CoinRewardLive { get; set; }
        public double CoinRewardDay { get; set; }
        public ProfitProvider Source { get; set; }

        public bool HasValues()
        {
            if (UsdRewardLive > 0) return true;
            if (UsdRewardDay > 0) return true;
            if (CoinRewardLive > 0) return true;
            if (CoinRewardDay > 0) return true;
            
            return false;
        }

        public double GetMostCurrentUsdReward()
        {
            if (UsdRewardLive > 0)
            {
                return UsdRewardLive;
            }

            return UsdRewardDay;
        }

        public override string ToString()
        {
            string result = "";
            if (UsdRewardLive > 0)
            {
                result += "Live:  " + UsdRewardLive.ToCurrency("$");
            }
            if (UsdRewardDay > 0)
            {
                if (!String.IsNullOrEmpty(result))
                {
                    result += "\n";
                }
                result += "24h:   " + UsdRewardDay.ToCurrency("$");
            }
            if (CoinRewardDay > 0 && CoinRewardLive > 0)
            {
                if (!String.IsNullOrEmpty(result))
                {
                    result += "\n";
                }
                double relativeCoinReward = CoinRewardLive / CoinRewardDay;
                result += "Diff-: " + Math.Round(relativeCoinReward * 100, 0, MidpointRounding.AwayFromZero) + "%"; ;
            }
            return result;
        }
    }
}
