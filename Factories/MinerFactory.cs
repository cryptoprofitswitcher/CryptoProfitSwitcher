using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Miners;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryptonightProfitSwitcher.Factories
{
    public static class MinerFactory
    {
        public static IMiner GetMiner(Miner miner)
        {
            switch (miner)
            {
                case Miner.XmrStak:
                    return new XmrStakMiner(false);
                case Miner.CastXmr:
                    return new CastXmrMiner();
                case Miner.SRBMiner:
                    return new SrbMiner();
                default:
                    throw new NotImplementedException("Couldn't start miner, unknown miner: " + miner);
            }
        }
    }
}
