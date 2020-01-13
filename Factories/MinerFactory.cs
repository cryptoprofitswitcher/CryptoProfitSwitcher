using System;
using System.Collections.Generic;
using System.Linq;
using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.Miners;
using CryptoProfitSwitcher.Models;

namespace CryptoProfitSwitcher.Factories
{
    public static class MinerFactory
    {
        public static IMiner GetMiner(HashSet<DeviceConfig> deviceConfigs, Pool pool)
        {
            switch (deviceConfigs.First().Miner)
            {
                case Miner.XmRig:
                    return new XmRigMiner(deviceConfigs, pool);
                case Miner.TeamRedMiner:
                    return new TeamRedMiner(deviceConfigs, pool);
                case Miner.Claymore:
                    return new ClaymoreMiner(deviceConfigs, pool);
                default:
                    throw new NotImplementedException("Couldn't start miner, unknown miner: " + deviceConfigs.First().Miner);
            }
        }
    }
}
