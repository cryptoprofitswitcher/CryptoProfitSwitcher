using CryptonightProfitSwitcher.Mineables;

namespace CryptonightProfitSwitcher.Models
{
    public class MineableReward
    {
        public MineableReward(Mineable mineable, double reward)
        {
            Mineable = mineable;
            Reward = reward;
        }

        public Mineable Mineable { get; set; }
        public double Reward { get; set; }
    }
}
