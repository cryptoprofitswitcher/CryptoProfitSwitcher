using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CryptoProfitSwitcher.ProfitPoviders
{
    public class WhatToMineApi : IPoolProfitProvider
    {
        // Gets all profit data with the base hashrate 10000000 (10 MH)
        private const string ApiUrl = "https://whattomine.com/coins.json?utf8=%E2%9C%93&adapt_q_380=0&adapt_q_fury=0&adapt_q_470=0&adapt_q_480=3&adapt_q_570=0&adapt_q_580=0&adapt_q_vega56=0&adapt_q_vega64=1&adapt_q_vii=0&adapt_q_1050Ti=0&adapt_q_10606=0&adapt_q_1070=0&adapt_q_1070Ti=0&adapt_q_1080=0&adapt_q_1080Ti=0&adapt_q_1660=0&adapt_q_1660Ti=0&adapt_q_2060=0&adapt_q_2070=0&adapt_q_2080=0&adapt_q_2080Ti=0&eth=true&factor%5Beth_hr%5D=10&factor%5Beth_p%5D=0.0&zh=true&factor%5Bzh_hr%5D=10000000&factor%5Bzh_p%5D=0.0&cnh=true&factor%5Bcnh_hr%5D=10000000&factor%5Bcnh_p%5D=0.0&cng=true&factor%5Bcng_hr%5D=10000000&factor%5Bcng_p%5D=0.0&cnr=true&factor%5Bcnr_hr%5D=10000000&factor%5Bcnr_p%5D=0.0&cnf=true&factor%5Bcnf_hr%5D=10000000&factor%5Bcnf_p%5D=0.0&eqa=true&factor%5Beqa_hr%5D=10000000&factor%5Beqa_p%5D=0.0&cc=true&factor%5Bcc_hr%5D=10000000&factor%5Bcc_p%5D=0.0&cr29=true&factor%5Bcr29_hr%5D=10000000&factor%5Bcr29_p%5D=0.0&ct31=true&factor%5Bct31_hr%5D=10000000&factor%5Bct31_p%5D=0.0&eqb=true&factor%5Beqb_hr%5D=10000000&factor%5Beqb_p%5D=0.0&rmx=true&factor%5Brmx_hr%5D=10000000&factor%5Brmx_p%5D=0.0&ns=true&factor%5Bns_hr%5D=10000&factor%5Bns_p%5D=0.0&tt10=true&factor%5Btt10_hr%5D=10&factor%5Btt10_p%5D=0.0&x16r=true&factor%5Bx16r_hr%5D=10&factor%5Bx16r_p%5D=0.0&phi2=true&factor%5Bphi2_hr%5D=10&factor%5Bphi2_p%5D=0.0&xn=true&factor%5Bxn_hr%5D=10&factor%5Bxn_p%5D=0.0&eqz=true&factor%5Beqz_hr%5D=10000000&factor%5Beqz_p%5D=0.0&zlh=true&factor%5Bzlh_hr%5D=10000000&factor%5Bzlh_p%5D=0.0&ppw=true&factor%5Bppw_hr%5D=10&factor%5Bppw_p%5D=0.0&x25x=true&factor%5Bx25x_hr%5D=10&factor%5Bx25x_p%5D=0.0&mtp=true&factor%5Bmtp_hr%5D=10&factor%5Bmtp_p%5D=0.0&lrev3=true&factor%5Blrev3_hr%5D=10&factor%5Blrev3_p%5D=0.0&factor%5Bcost%5D=0.0&sort=Profitability24&volume=0&revenue=current&factor%5Bexchanges%5D%5B%5D=&factor%5Bexchanges%5D%5B%5D=binance&factor%5Bexchanges%5D%5B%5D=bitfinex&factor%5Bexchanges%5D%5B%5D=bitforex&factor%5Bexchanges%5D%5B%5D=bittrex&factor%5Bexchanges%5D%5B%5D=dove&factor%5Bexchanges%5D%5B%5D=exmo&factor%5Bexchanges%5D%5B%5D=gate&factor%5Bexchanges%5D%5B%5D=graviex&factor%5Bexchanges%5D%5B%5D=hitbtc&factor%5Bexchanges%5D%5B%5D=hotbit&factor%5Bexchanges%5D%5B%5D=ogre&factor%5Bexchanges%5D%5B%5D=poloniex&factor%5Bexchanges%5D%5B%5D=stex&dataset=&commit=Calculate";

        public Dictionary<Pool, Profit> GetProfits(IList<Pool> pools, bool enableCaching, DirectoryInfo appRootFolder, CancellationToken ct)
        {
            var profitsDictionary = new Dictionary<Pool, Profit>();

            try
            {
                if (pools.Any())
                {
                    var btcJson = Helpers.GetJsonFromUrl("https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd", enableCaching, appRootFolder, ct);
                    double btcUsdPrice = JToken.Parse(btcJson)["bitcoin"].Value<double>("usd");

                    var coinsJson = Helpers.GetJsonFromUrl(ApiUrl, enableCaching, appRootFolder, ct);
                    JToken jCoins = JToken.Parse(coinsJson)["coins"];
                    foreach (Pool pool in pools)
                    {
                        JToken coin = jCoins[pool.ProfitProviderInfo];
                        double factor = Profit.BaseHashrate / 10000000d;
                        double liveBtcReward = coin.Value<double>("btc_revenue") * factor;
                        double dayBtcReward = coin.Value<double>("btc_revenue24") * factor;
                        double liveCoinRewards = coin.Value<double>("estimated_rewards");
                        double dayCoinRewards = coin.Value<double>("estimated_rewards24");
                        double liveUsdReward = liveBtcReward * btcUsdPrice;
                        double dayUsdReward = dayBtcReward * btcUsdPrice;
                        profitsDictionary[pool] = new Profit(liveUsdReward, dayUsdReward, liveCoinRewards, dayCoinRewards, ProfitProvider.WhatToMineApi);
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Warning("Couldn't get profits data from WhatToMine Api: " + ex.Message);
            }
            return profitsDictionary;
        }


    }
}
