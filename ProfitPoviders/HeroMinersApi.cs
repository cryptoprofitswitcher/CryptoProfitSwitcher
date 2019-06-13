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
                case "ADON":
                    return "https://adon.herominers.com/api/stats";
                case "ARQ":
				case "ARQTRTL":
                    return "https://arqma.herominers.com//api/stats";
                case "ARQPLE":
                    return "https://arqple.herominers.com/api/stats";case "IPBC":
                case "TUBE":
                    return "https://tube.herominers.com/api/stats";
                case "CCX":
                    return "https://conceal.herominers.com/api/stats";
                case "DERO":
                    return "https://dero.herominers.com/api/stats";
                case "ETN":
                    return "https://electroneum.herominers.com/api/stats";
                case "GRFT":
                    return "https://graft.herominers.com/api/stats";
                case "XHV":
                    return "https://haven.herominers.com/api/stats";
                case "XHVBLOC":
				case "HAVENBLOC":
                    return "https://havenbloc.herominers.com/api/stats";
                case "PLE":
				case "LOKIPLE":
                    return "https://plenteum.herominers.com//api/stats";
                case "LOK":
                case "LOKI":
				case "LOKITRTL":
				case "LOKITURTLE":
                    return "https://lokiturtle.herominers.com/api/stats";
                case "MSR":
                    return "https://masari.herominers.com/api/stats";
                case "XMR":
                    return "https://monero.herominers.com/api/stats";
                case "XSC":
                    return "https://obscure.herominers.com/api/stats";
				case "QRL":
                    return "https://qrl.herominers.com/api/stats";
                case "RYO":
                    return "https://ryo.herominers.com/api/stats";
                case "XWP":
				case "SWAP":
                    return "https://swap.herominers.com/api/stats";
                case "XTL":
				case "XTC":
					return "https://torque.herominers.com//api/stats";               	
				case "XTRI":
                    return "https://triton.herominers.com/api/stats";
                case "TRTL":
					return "https://turtlecoin.herominers.com//api/stats";               	
				case "XCA":
                case "UPX":
                    return "https://uplexa.herominers.com/api/stats";
                case "XCASH":
                    return "https://xcash.herominers.com/api/stats";
                case "XTNCPLE":
                    return "https://xtncple.herominers.com//api/stats";
                case "XTNC":
				case "XTNCTRTL":
                    return "https://xtendcash.herominers.com/api/stats";
            }
            return $"https://{coin.TickerSymbol.ToLowerInvariant()}.herominers.com/api/stats";
        }
    }
}
