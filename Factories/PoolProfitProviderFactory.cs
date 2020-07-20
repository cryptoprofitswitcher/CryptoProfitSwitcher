using System;
using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.ProfitPoviders;

namespace CryptoProfitSwitcher.Factories
{
    public static class PoolProfitProviderFactory
    {
        public static IPoolProfitProvider GetPoolProfitProvider(ProfitProvider profitProvider)
        {
            switch (profitProvider)
            {
                case ProfitProvider.MinerRocksApi:
                    return new MinerRocksApi();
                case ProfitProvider.MoneroOceanApi:
                    return new MoneroOceanApi();
                case ProfitProvider.CryptunitApi:
                    return new CryptunitApi();
                case ProfitProvider.HeroMinersApi:
                    return new HeroMinersApi();
                case ProfitProvider.NiceHashApi:
                    return new NiceHashApi();
                case ProfitProvider.MineXmrApi:
                    return new MineXmrApi();
                case ProfitProvider.WhatToMineApi:
                    return new WhatToMineApi();
                case ProfitProvider.NimiqApi:
                    return new NimiqApi();
                default:
                    throw new NotImplementedException("Doesn't support ProfitProvider: " + profitProvider);
            }
        }
    }
}
