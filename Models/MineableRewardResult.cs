using System;
using System.Collections.Generic;
using System.Text;

namespace CryptonightProfitSwitcher.Models
{
    public class MineableRewardResult
    {
        public MineableReward Result { get; set; }
        public MineableReward Current { get; set; }

        public MineableRewardResult(MineableReward result, MineableReward current)
        {
            Result = result;
            Current = current;
        }
    }
}
