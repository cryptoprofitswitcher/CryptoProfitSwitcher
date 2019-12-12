using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CryptoProfitSwitcher.Enums;
using CryptoProfitSwitcher.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CryptoProfitSwitcher.ProfitPoviders
{
    internal class CryptunitApi : IPoolProfitProvider
    {
        private static Dictionary<string, int> TickerToAlgoId { get; set; }
        public Dictionary<Pool, Profit> GetProfits(IList<Pool> pools, bool enableCaching, DirectoryInfo appRootFolder, CancellationToken ct)
        {
            var poolProfitsDictionary = new Dictionary<Pool, Profit>();
            try
            {

                if (pools.Any())
                {
                    var liveAlgorithms = new List<Pool>();
                    var dayAlgorithms = new List<Pool>();

                    foreach (var pool in pools)
                    {
                        if (pool.ProfitTimeframe == ProfitTimeframe.Day)
                        {
                            dayAlgorithms.Add(pool);
                        }
                        else
                        {
                            liveAlgorithms.Add(pool);
                        }
                    }

                    if (TickerToAlgoId == null)
                    {
                        Dictionary<string, int> tickerToAlgoIdDictionary = new Dictionary<string, int>();
                        var algosJson = Helpers.GetJsonFromUrl("https://www.cryptunit.com/api/coins/", enableCaching, appRootFolder, CancellationToken.None);
                        foreach (JToken jAlgo in JToken.Parse(algosJson).Children())
                        {
                            tickerToAlgoIdDictionary[jAlgo.Value<string>("ticker")] = jAlgo.Value<int>("algo_id");
                        }

                        TickerToAlgoId = tickerToAlgoIdDictionary;
                    }

                    List<int> algoIds = new List<int>();
                    foreach (Pool pool in pools)
                    {
                        int algoId = TickerToAlgoId[pool.ProfitProviderInfo];
                        if (!algoIds.Contains(algoId))
                        {
                            algoIds.Add(algoId);
                        }
                    }

                    if (algoIds.Count > 0)
                    {
                        StringBuilder apiRequestBuilder = new StringBuilder();
                        apiRequestBuilder.Append("https://www.cryptunit.com/api/earningscustom/");
                        apiRequestBuilder.Append("?hashrate[").Append(algoIds[0]).Append("]=").Append(Profit.BaseHashrate);
                        for (var index = 1; index < algoIds.Count; index++)
                        {
                            int algoId = algoIds[index];
                            apiRequestBuilder.Append("&hashrate[").Append(algoId).Append("]=").Append(Profit.BaseHashrate);
                        }

                        string apiRequest = apiRequestBuilder.ToString();
                        if (liveAlgorithms.Count > 0)
                        {
                            try
                            {
                                var liveProfitsJson = Helpers.GetJsonFromUrl(apiRequest + "&dataavg=live", enableCaching, appRootFolder, ct);
                                SetProfitFromJson(liveProfitsJson, ProfitTimeframe.Live, liveAlgorithms, poolProfitsDictionary);
                            }
                            catch (Exception ex)
                            {
                                Log.Warning("Couldn't get live profits data from Cryptunit: " + ex.Message);
                            }
                        }

                        if (dayAlgorithms.Count > 0)
                        {
                            try
                            {
                                var dayProfitsJson = Helpers.GetJsonFromUrl(apiRequest + "&dataavg=live", enableCaching, appRootFolder, ct);
                                SetProfitFromJson(dayProfitsJson, ProfitTimeframe.Day, liveAlgorithms, poolProfitsDictionary);
                            }
                            catch (Exception ex)
                            {
                                Log.Warning("Couldn't get daily profits data from Cryptunit: " + ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to get profits data from Cryptunit Api: " + ex.Message);
            }
            return poolProfitsDictionary;
        }

        private void SetProfitFromJson(string json, ProfitTimeframe profitTimeframe, List<Pool> pools, Dictionary<Pool, Profit> poolProfitsDictionary)
        {
            foreach (JToken jCoin in JToken.Parse(json).First["coins"].Children())
            {
                foreach (Pool matchedPool in pools.Where(p => p.ProfitProviderInfo == jCoin.Value<string>("coin_ticker")))
                {
                    switch (profitTimeframe)
                    {
                        case ProfitTimeframe.Live:
                            poolProfitsDictionary[matchedPool] = new Profit(jCoin.Value<double>("reward_day_usd"), 0, jCoin.Value<double>("reward_day_coins"), 0, ProfitProvider.CryptunitApi);
                            break;
                        case ProfitTimeframe.Day:
                            poolProfitsDictionary[matchedPool] = new Profit(0, jCoin.Value<double>("reward_day_usd"), 0, jCoin.Value<double>("reward_day_coins"), ProfitProvider.CryptunitApi);
                            break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}
