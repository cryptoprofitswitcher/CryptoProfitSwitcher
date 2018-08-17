using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Miners;
using System;

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
                case Miner.JceMiner:
                    return new JceMiner();
                default:
                    throw new NotImplementedException("Couldn't start miner, unknown miner: " + miner);
            }
        }
    }
}
