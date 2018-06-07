using CryptonightProfitSwitcher.Enums;
using System;

namespace CryptonightProfitSwitcher
{
    public struct Profit
    {
        public Profit(double reward, ProfitProvider source, ProfitTimeframe timeframe)
        {
            Reward = reward;
            Source = source;
            Timeframe = timeframe;
        }

        public double Reward { get; set; }
        public ProfitProvider Source { get; set; }
        public ProfitTimeframe Timeframe { get; set; }
        public override string ToString()
        {
            string result = Reward.ToCurrency("$ (");
            switch (Timeframe)
            {
                case ProfitTimeframe.Live:
                    result += "Live)";
                    break;
                case ProfitTimeframe.Day:
                    result += "24h)";
                    break;
                default:
                    throw new NotImplementedException("Unknown profit timeframe: " + Timeframe);
            }
            //result += Source + ")";
            return result;

        }
    }
}
