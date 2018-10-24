using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.ProfitPoviders;
using System;

namespace CryptonightProfitSwitcher.Factories
{
    public static class PoolProfitProviderFactory
    {
        public static IPoolProfitProvider GetPoolProfitProvider(ProfitProvider profitProvider)
        {
            switch (profitProvider)
            {
                case ProfitProvider.MineCryptonightApi:
                    return new MineCryptonightApi();
                case ProfitProvider.MinerRocksApi:
                    return new MinerRocksApi();
                case ProfitProvider.MoneroOceanApi:
                    return new MoneroOceanApi();
                case ProfitProvider.CryptoknightCCApi:
                    return new CryptoknightCcApi();
                case ProfitProvider.CryptunitApi:
                    return new CryptunitApi();
                case ProfitProvider.HeroMinersApi:
                    return new HeroMinersApi();
                default:
                    throw new NotImplementedException("Doesn't support ProfitProvider: " + profitProvider);
            }
        }
    }
}
