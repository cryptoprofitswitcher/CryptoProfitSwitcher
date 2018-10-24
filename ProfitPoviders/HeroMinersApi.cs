using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CryptonightProfitSwitcher.Enums;
using CryptonightProfitSwitcher.Mineables;
using CryptonightProfitSwitcher.Models;
using Newtonsoft.Json.Linq;

namespace CryptonightProfitSwitcher.ProfitPoviders
{
    public class HeroMinersApi : IPoolProfitProvider
    {
        public Dictionary<string, Profit> GetProfits(DirectoryInfo appRootFolder, Settings settings, IList<Coin> coins, CancellationToken ct)
        {
            var poolProfitsDictionary = new Dictionary<string, Profit>();

            List<Task> tasks = new List<Task>();
            foreach (var coin in coins)
            {
                var requestedProfitProviders = Helpers.GetPoolProfitProviders(settings, coin);
                if (requestedProfitProviders.Contains(ProfitProvider.HeroMinersApi))
                {
                    tasks.Add(SetProfitForCoinTaskAsync(coin, settings, appRootFolder, poolProfitsDictionary, ct));
                }
            }
            Task.WhenAll(tasks).Wait(ct);
            return poolProfitsDictionary;
        }
        private Task SetProfitForCoinTaskAsync(Coin coin, Settings settings, DirectoryInfo appRootFolder, Dictionary<string, Profit> poolProfitsDictionary, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                try
                {
                    string apiUrl = GetApiUrl(coin);
                    if (!String.IsNullOrEmpty(apiUrl))
                    {
                        var profitsJson = Helpers.GetJsonFromUrl(apiUrl, settings, appRootFolder, ct);
                        dynamic lastStats = JObject.Parse(profitsJson);
                        ProfitTimeframe timeFrame = coin.OverrideProfitTimeframe.HasValue ? coin.OverrideProfitTimeframe.Value : settings.ProfitTimeframe;

                        decimal diffDay = lastStats.charts.difficulty_1d;
                        decimal diffLive = lastStats.network.difficulty;

                        decimal reward = lastStats.lastblock.reward;

                        decimal profitDay = (coin.GetExpectedHashrate(settings) * (86400 / diffDay)) * reward;
                        decimal profitLive = (coin.GetExpectedHashrate(settings) * (86400 / diffLive)) * reward;

                        // Get amount of coins
                        decimal coinUnits = lastStats.config.coinUnits;
                        decimal amountDay = profitDay / coinUnits;
                        decimal amountLive = profitLive / coinUnits;

                        //Get usd price
                        decimal usdPrice = lastStats.charts.price_1h;

                        //Multiplicate
                        decimal usdRewardDecDay = amountDay * usdPrice;
                        double usdRewardDay = (double)usdRewardDecDay;

                        decimal usdRewardDecLive = amountLive * usdPrice;
                        double usdRewardLive = (double)usdRewardDecLive;

                        poolProfitsDictionary[coin.TickerSymbol] = new Profit(usdRewardLive, usdRewardDay, (double)amountLive, (double)amountDay, ProfitProvider.HeroMinersApi, timeFrame);
                        Console.WriteLine($"Got profit data for {coin.TickerSymbol} from MinerRocksAPI");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldn't get profits data for {coin.DisplayName} from HeroMinersApi: " + ex.Message);
                }
            },ct);
        }
        private string GetApiUrl(Coin coin)
        {
            switch(coin.TickerSymbol)
            {
                case "DERO":
                    return "https://dero.herominers.com/api/stats";
                case "ETN":
                    return "https://electroneum.herominers.com/api/stats";
                case "KRB":
                    return "https://karbo.herominers.com/api/stats";
                case "SUMO":
                    return "https://sumo.herominers.com/api/stats";
                case "CTL":
                    return "https://citadel.herominers.com/api/stats";
                case "GRFT":
                    return "https://graft.herominers.com/api/stats";
                case "LTHN":
                    return "https://lethean.herominers.com/api/stats";
                case "XMR":
                    return "https://monero.herominers.com/api/stats";
                case "XMV":
                    return "https://monerov.herominers.com/api/stats";
                case "QRL":
                    return "https://qrl.herominers.com/api/stats";
                case "SAFE":
                case "SFX":
                    return "https://safex.herominers.com/api/stats";
                case "XCA":
                    return "https://xcash.herominers.com/api/stats";
                case "BXB":
                    return "https://bixbite.herominers.com/api/stats";
                case "CCH":
                    return "https://citadel.herominers.com/api/stats";
                case "LOK":
                case "LOKI":
                    return "https://loki.herominers.com/api/stats";
                case "RYO":
                    return "https://ryo.herominers.com/api/stats";
                case "XRN":
                    return "https://saronite.herominers.com/api/stats";
                case "BLOC":
                    return "https://bloc.herominers.com/api/stats";
                case "XHV":
                    return "https://haven.herominers.com/api/stats";
                case "IPBC":
                case "TUBE":
                    return "https://tube.herominers.com/api/stats";
                case "XTL":
                    return "https://stellite.herominers.com/api/stats";
                case "CCX":
                    return "https://conceal.herominers.com/api/stats";
                case "MSR":
                    return "https://masari.herominers.com/api/stats";
                case "AEON":
                    return "https://aeon.herominers.com/api/stats";
                case "ARQ":
                    return "https://arqma.herominers.com/api/stats";
            }
            return $"https://{coin.TickerSymbol.ToLowerInvariant()}.herominers.com/api/stats";
        }
    }
}
