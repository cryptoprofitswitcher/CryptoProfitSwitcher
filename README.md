
![Screenshot](Images/logo1.png?raw=true "Screenshot")

Whalien Mining Switcher is an extensible open-source .Net Core console application that helps you to **always mine the most profitable coin on a pool or an algorithm on NiceHash**.

It is very useful for **AMD Vega GPUs** because it can detect hash drops and reset the GPU(s) but it can be used with any GPU that is compatible with the miner.
It is optimized for **Windows** but also works for **Linux**.

### Screenshot

![Screenshot](screenshot.png?raw=true "Screenshot")

# Features

  - **Profit switching**: Between pool mined coins and NiceHash algorithms
  - **Watchdog:** Compares your actual hashrate with the expected hashrate and runs a reset if certain conditions are met.
  - **Reset:** Restarts the miner and runs a user-defined Reset script
  - **Performance:** Resource friendly with 0%-CPU usage
  - **Miners:** Xmr-Stak 
  - **Profit data:** [MineCryptoNight API](http://minecryptonight.net/api), [CryptUnit API](https://www.cryptunit.com/api/), [miner.rocks API](https://miner.rocks/), [cryptoknight.cc API](https://cryptoknight.cc/), [HeroMiners API](https://herominers.com/), [MoneroOcean](https://moneroocean.stream)
  - **Multiple profit switching strategies:** Maximize fiat profit, maximize coin reward or a combination
  - **Manual mode:** You can switch between automatic mode and manual mode where you select the coin / NiceHash algorithm yourself
  - **Supports all coins:** That are mineable using the miners
  - **Supports all pools:** That are supported by the miners


# How to use?

1. Download the latest release of the app
2. Extract it
3. Optimize the Xmr-Stak config
    1. Go to the folder *Xmr-Stak*
    2. Edit the *cpu_v7.txt*, *cpu_heavy.txt*, *cpu_lite.txt* according to your CPU.
    3. Edit the *amd_v7.txt*, *amd_v8.txt*, *amd_heavy.txt*, *amd_lite.txt* according to your GPU.
4. Set your pool mined coins
    1. Go to the folder *Coins*
    2. Delete the existing coins you don't want to mine
    3. Edit the coins you want to mine
    4. Add the additional coins you want to mine
4. Set your NiceHash algorithms
    1. Go to the folder *NicehashAlgorithms*
    2. Delete the existing NiceHash algorithms you don't want to mine
    3. Edit the NiceHash algorithms you want to mine
    4. Add the additional NiceHash algorithms you want to mine
5. Open the file *Settings.json* and edit the settings, most importantly edit the expected hashrates
6. Start *CryptonightProfitSwitcher.exe* (Optional: As administrator)
7. PROFIT!!!

### Default configuration

If you download the latest release, there will be a default configuration:

- CPU: null
- GPU: AMD Vega 64 + AMD Vega 56
- Coins: Monero, Conceal, Lethean, Sumo, Masari
- NiceHash: CryptonightV8, CryptonightV7, CryptonightHeavy
- Miner: XmrStak

You have to change the default configuration for your setup, see **How to use?**.

## How to add a pool mined coin?

1. Open the *Coins* folder
2. Copy an existing coin to the same location.
3. Rename it and edit the JSON-File.

## How to add a NiceHash algorithm?

1. Open the *NicehashAlgorithms* folder
2. Copy an existing algorithm to the same location.
3. Rename it and edit the JSON-File.

# How does what work?

### Profit switching

1. App will load pool mined coins from the *Coins* folder.
2. App will load NiceHash algorithms from the *NicehashAlgorithms* folder.
3. App will load settings from Settings.json.
4. App will periodically check the profitability.
5. App will start the most profitable mining method based on the defined strategy.

#### Strategy 1: MaximizeFiat (default)
Will select the coin / NiceHash algorithm that has the most profit in USD per day.
#### Strategy 2: MaximizeCoins
Will select the coin that has the least difficulty to mine compared to the 24h average difficulty.
This strategy will ignore the price of the coin and does only work with coins that have profit data for 24h average.
#### Strategy 3: WeightedCoinsPrice
This strategy is a combination of the above two strategies.
It will multiplicate the profit in USD per day with the relative coin difficulty and maximize this new value.
This strategy will work with all coins / NiceHash algorithms because it will use *1* for the relative coin difficulty if it can't get the actual relative coin difficulty.

### Reset

1. App will terminate the miner.
2. App will run the reset script, if it is set.
3. App will restart itself.

### Watchdog

1. App will periodically check the actual hashrate
2. If the actual hashrate is lower than the specified threshold in the settings, that will be an overshot.
3. If you get more consecutive overshots than allowed (specified in the settings) than the app will perform a reset.

# Credits

* [fireice-uk's and psychocrypt's Xmr-Stak](https://github.com/fireice-uk/xmr-stak)
* [MineCryptoNight API](http://minecryptonight.net/api)
* [CryptUnit API](https://www.cryptunit.com/api/)
* [miner.rocks API](https://miner.rocks/)
* [cryptoknight.cc API](https://cryptoknight.cc/)
* [HeroMiners API](https://herominers.com/)
* [MoneroOcean API](https://moneroocean.stream)
* [NiceHash API](https://www.nicehash.com/doc-api)
* [CoinMarketCap API](https://coinmarketcap.com/api/)
* [CsConsoleFormat](https://github.com/Athari/CsConsoleFormat)
* [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
* [Cryptonote Profit Switcher](https://github.com/cryptoprofitswitcher)
