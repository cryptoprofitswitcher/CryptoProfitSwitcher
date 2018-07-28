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
    public class MinerRocksApi : IPoolProfitProvider
    {
        public Dictionary<string, Profit> GetProfits(DirectoryInfo appRootFolder, Settings settings, IList<Coin> coins, CancellationToken ct)
        {
            var poolProfitsDictionary = new Dictionary<string, Profit>();

            List<Task> tasks = new List<Task>();
            foreach (var coin in coins)
            {
                var requestedProfitProviders = Helpers.GetPoolProfitProviders(settings, coin);
                if (requestedProfitProviders.Contains(ProfitProvider.MinerRocksApi))
                {
                    tasks.Add(SetProfitForCoinTask(coin, settings, appRootFolder, poolProfitsDictionary, ct));
                }
            }
            Task.WhenAll(tasks).Wait(ct);
            return poolProfitsDictionary;
        }
        Task SetProfitForCoinTask(Coin coin, Settings settings, DirectoryInfo appRootFolder, Dictionary<string, Profit> poolProfitsDictionary, CancellationToken ct)
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
                        decimal diffDay = lastStats.pool.stats.diffs["wavg24h"];
                        decimal diffLive = lastStats.network.difficulty;

                        decimal reward = lastStats.network.reward;

                        decimal profitDay = (coin.GetExpectedHashrate(settings) * (86400 / diffDay)) * reward;
                        decimal profitLive = (coin.GetExpectedHashrate(settings) * (86400 / diffLive)) * reward;

                        // Get amount of coins
                        decimal coinUnits = lastStats.config.coinUnits;
                        decimal amountDay = profitDay / coinUnits;
                        decimal amountLive = profitLive / coinUnits;

                        //Get usd price
                        decimal usdPrice = lastStats.coinPrice["coin-usd"];

                        //Multiplicate
                        decimal usdRewardDecDay = amountDay * usdPrice;
                        double usdRewardDay = (double)usdRewardDecDay;

                        decimal usdRewardDecLive = amountLive * usdPrice;
                        double usdRewardLive = (double)usdRewardDecLive;

                        poolProfitsDictionary[coin.TickerSymbol] = new Profit(usdRewardLive, usdRewardDay, (double)amountLive, (double)amountDay, ProfitProvider.MinerRocksApi, timeFrame);
                        Console.WriteLine($"Got profit data for {coin.TickerSymbol} from MinerRocksAPI");

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldn't get profits data for {coin.DisplayName} from MinerRocksApi: " + ex.Message);
                }
            },ct);
        }
        private string GetApiUrl(Coin coin)
        {
            switch(coin.TickerSymbol)
            {
                case "ETN":
                    return "https://etn.miner.rocks/api/stats";
                case "GRFT":
                    return "https://graft.miner.rocks/api/stats";
                case "ITNS":
                    return "https://itns.miner.rocks/api/stats";
                case "MSR":
                    return "https://masari.miner.rocks/api/stats";
                case "XMR":
                    return "https://monero.miner.rocks/api/stats";
                case "XTL":
                    return "https://stellite.miner.rocks/api/stats";
                case "LOKI":
                    return "https://loki.miner.rocks/api/stats";
                case "XHV":
                    return "https://haven.miner.rocks/api/stats";
                case "XRN":
                    return "https://saronite.miner.rocks/api/stats";
                case "RYO":
                    return "https://ryo.miner.rocks/api/stats";
                case "AEON":
                    return "https://aeon.miner.rocks/api/stats";
                case "TUBE":
                    return "https://bittube.miner.rocks/api/stats";
            }
            return null;
        }
    }
}
