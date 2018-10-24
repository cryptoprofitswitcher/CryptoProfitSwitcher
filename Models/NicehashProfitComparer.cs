using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using System.Collections.Generic;

namespace CryptonightProfitSwitcher.Models
{
    public class NicehashProfitComparer : IComparer<NicehashAlgorithm>
    {
        private SortingMode _sortingMode;
        private Dictionary<int, Profit> _profitDictionary;

        public NicehashProfitComparer(SortingMode sortingMode, Dictionary<int, Profit> profitDictionary)
        {
            _sortingMode = sortingMode;
            _profitDictionary = profitDictionary;
        }

        public int Compare(NicehashAlgorithm x, NicehashAlgorithm y)
        {
            if (_sortingMode == SortingMode.None) return 0;
            double profitX = GetBestProfit(x);
            double profitY = GetBestProfit(y);
            return profitY.CompareTo(profitX);
        }

        private double GetBestProfit(NicehashAlgorithm nicehashAlgorithm)
        {
            var profit = _profitDictionary.GetValueOrDefault(nicehashAlgorithm.ApiId, new Profit());
            return profit.UsdRewardLive;
        }
    }
}
