using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using System;
using System.Collections.Generic;

namespace CryptonightProfitSwitcher.Models
{
    public class CoinProfitComparer : IComparer<Coin>
    {
        private SortingMode _sortingMode;
        private Dictionary<ProfitProvider, Dictionary<string, Profit>> _profitDictionary;

        public CoinProfitComparer(SortingMode sortingMode, Dictionary<ProfitProvider, Dictionary<string, Profit>> profitDictionary)
        {
            _sortingMode = sortingMode;
            _profitDictionary = profitDictionary;
        }

        public int Compare(Coin x, Coin y)
        {
            if (_sortingMode == SortingMode.None) return 0;
            double profitX = GetBestProfit(x);
            double profitY = GetBestProfit(y);
            return profitY.CompareTo(profitX);
        }

        private double GetBestProfit(Coin coin)
        {
            double result = 0;
            foreach(var profitProvider in _profitDictionary)
            {
                var profit = profitProvider.Value.GetValueOrDefault(coin.TickerSymbol, new Profit());
                switch (_sortingMode)
                {
                    case SortingMode.ProfitLive:
                        result = profit.UsdRewardLive;
                        break;
                    case SortingMode.ProfitDay:
                        result = profit.UsdRewardDay;
                        break;
                    case SortingMode.Coins:
                        if (profit.CoinRewardDay > 0 && profit.CoinRewardLive > 0)
                        {
                            result = profit.CoinRewardLive / profit.CoinRewardDay;
                        }
                        break;
                    default:
                        throw new NotImplementedException("Sorting mode not implemented: " + _sortingMode);
                }
                if (result != 0)
                {
                    break;
                }
            }
            return result;
        }
    }
}
