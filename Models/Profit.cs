using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using System;

namespace CryptonightProfitSwitcher
{
    public struct Profit
    {
        public Profit(double usdRewardLive, double usdRewardDay, double coinRewardLive, double coinRewardDay, ProfitProvider source, ProfitTimeframe timeframe)
        {
            UsdRewardLive = usdRewardLive;
            UsdRewardDay = usdRewardDay;
            CoinRewardLive = coinRewardLive;
            CoinRewardDay = coinRewardDay;
            Source = source;
            Timeframe = timeframe;
        }

        public double UsdRewardLive { get; set; }
        public double UsdRewardDay { get; set; }
        public double CoinRewardLive { get; set; }
        public double CoinRewardDay { get; set; }
        public ProfitProvider Source { get; set; }
        public ProfitTimeframe Timeframe { get; set; }

        public override string ToString()
        {
            string result = "";
            if (UsdRewardLive > 0)
            {
                result += "Live: " + UsdRewardLive.ToCurrency("$");
            }
            if (UsdRewardDay > 0)
            {
                if (!String.IsNullOrEmpty(result))
                {
                    result += " | ";
                }
                result += "24h: " + UsdRewardDay.ToCurrency("$");
            }
            if (CoinRewardDay > 0 && CoinRewardLive > 0)
            {
                if (!String.IsNullOrEmpty(result))
                {
                    result += " | ";
                }
                double relativeCoinReward = CoinRewardLive / CoinRewardDay;
                result += "Coins: " + (Math.Round(relativeCoinReward, 4) * 100) + "%"; ;
            }
            return result;
        }
    }
}
