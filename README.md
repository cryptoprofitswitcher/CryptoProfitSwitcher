
![Screenshot](Images/logo.png?raw=true "Logo")

Crypto Profit Switcher is an extensible open-source .Net Core console application that helps you to **always mine the most profitable coin on a pool or an algorithm on NiceHash**.


### Screenshot

![Screenshot](screenshot.png?raw=true "Screenshot")

# Features

  - **Profit switching**: Between pool mined coins and NiceHash algorithms
  - **GPUs + CPU:** It supports mining with multiple GPUs (mainly AMD) and CPU.
  - **Benchmarking:** It helps you to find the expected hashrate.
  - **Easy Configuration:** Only one config file.
  - **Performance:** Resource friendly with 0%-CPU usage
  - **Miners:** TeamRedMiner, XmRig, Claymore's Dual Ethereum
  - **Profit data:** It supports a lot of profit providers e.g. CryptUnit, WhatToMine, NiceHash etc.
  - **Manual mode:** You can switch between automatic mode and manual mode


# How to use?

1. Download and extract the latest release of the app ([Latest release](https://github.com/cryptoprofitswitcher/CryptoProfitSwitcher/releases/latest))
2. Edit the *config.json* to configure it.
3. Start the app and profit.

# How does it work?

1. The app loads the configuration from the file **config.json**.
2. It downloads the miners if they have not been downloaded already.
3. It will benchmark all enabled algorithms that don't have a valid expected hashrate.
4. It starts to mine the most profitable combination of device, algorithm and pool.

# How to configure it?

All you need to do, is to edit the JSON file **config.json**.

### Config
| Property | Valid Values | Explanation |
| --- | --- | --- |
| ProcessPriority | Normal &#124; AboveNormal &#124; BelowNormal &#124; High &#124; RealTime | Sets the process priority
| ProfitSwitchStrategy | MaximizeFiat &#124; PreferLowDifficulty | **MaximizeFiat**: Will select the miner pool combination that has the most profit in USD per day.<br>**PreferLowDifficulty**: It will multiplicate the profit in USD per day with the relative coin difficulty and maximize this new value. If the profit provider of a pool does not have data about the relative difficulty, it will use a default value of 1.
| EnableCaching | true &#124; false | Enable or disable caching of JSON Api calls
| EnableLogging | true &#124; false | Enable or disable logging to a file
| EnableManualModeByDefault | true &#124; false | Set if the manual mode is enabled by default
| DisableBenchmarking | true &#124; false | Set if benchmarking on start should be disabled.
| DisableDownloadMiners | true &#124; false | Set if downloading of miners on start should be disabled.
| StartMinerMinimized | true &#124; false | Set if the miner should start minimized
| MinerStartDelay | integer number | Delay in seconds before miner start
| DisplayUpdateInterval | integer number | Time in seconds between display update
| ProfitCheckInterval | integer number | Time in seconds between profit checks
| ProfitSwitchInterval | integer number | Time in seconds between profit switch checks
| ProfitSwitchThreshold | decimal number | Threshold between current profit and highest profit before switching (e.g. 0 means switch immediately, 0.1 means switch when the most profitable is 10% percent more profitable than current)
| Algorithms | array of *Algorithm* objects | See **Algorithm**

### Algorithm
| Property | Valid Values | Explanation |
| --- | --- | --- |
| DisplayName | string | Self defined name of the algorithm
| Enabled | true &#124; false | Set if algorithm is enabled
| DeviceConfigs | array of *DeviceConfig* objects | See **Device Config**
| Pools | array of *Pool* objects | See **Pool**

### Device Config
| Property | Valid Values | Explanation |
| --- | --- | --- |
| DeviceType | CPU &#124; AMD &#124; NVIDIA | Type of the device
| DeviceId | string | Most likely the index of the GPU
| Enabled | true &#124; false | Set if device config is enabled
| ExpectedHashrate | decimal number | Expected hash rate in H/s
| PrepareScript | Path or empty | A script that will be executed before the miner starts
| Miner | TeamRedMiner &#124; XmRig &#124; Claymore | Type of the miner
| MinerPath | Path| Path to the executable of the miner
| MinerArguments | arguments | Arguments that will be forwarded to the miner. These should include the algorithm but not information about the pool or devices.
| MinerDeviceSpecificArguments | arguments | Arguments like *eth_config* or *cn_config* that are device specific.
| MinerApiPort | integer number | Port of the miner api or use 0 for auto port selection.

### Pool
| Property | Valid Values | Explanation |
| --- | --- | --- |
| UniqueName | string | Self defined unique name. Used for the UI and identification of the pool
| Enabled | true &#124; false | Set if pool is enabled
| ProfitTimeframe | Live &#124; Day | Sets the time frame of the profit data
| ProfitProvider | NiceHashApi &#124; CryptunitApi &#124; MoneroOceanApi &#124; MineXmrApi &#124; MinerRocksApi &#124; HeroMinersApi &#124; WhatToMineApi | Sets the profit provider
| ProfitProviderInfo | string | see **Profit provider info**
| PreferFactor | decimal number | On the determination of the most profitable pool, the actual estimated profit will be multiplied by this.
| PoolUrl | string | Pool url including the port number
| PoolUser | string | Pool user, most likely a wallet address
| PoolPassword | string | Pool password, most likely empty or x.


### Profit provider info
This value gives the profit provider information for which coin or algorithm you want to get the profit data. This value differs between the profit providers:

| Profit provider | Profit provider info |
| --- | --- |
| NiceHashApi | The API-ID of the algorithm (see the value of *order* in https://api2.nicehash.com/main/api/v2/mining/algorithms |
| CryptunitApi | The ticker value of the coin (see the value of *ticker* in https://www.cryptunit.com/api/coins |
| MinerRocksApi | The name of the subdomain for the coin e.g. for https://monero.miner.rocks/ it would be *monero*. |
| HeroMinersApi | The name of the subdomain for the coin e.g. for https://monero.herominers.com/ it would be *monero*. |
| WhatToMineApi | The key to the coin in https://whattomine.com/coins.json e.g. for Ethereum it would be *Ethereum*. |
| MoneroOceanApi | Not needed -> defaults to XMR |
| MineXmrApi | Not needed -> defaults to XMR|
| NimiqApi | Not needed -> defaults to Nimiq|



### Default configuration

If you download the latest release, there will be a default configuration, that is optimized for this config:

- CPU: AMD Ryzen 9 3900X
- GPU: AMD Vega 64 + AMD Vega 56

# How can I help?
You can help me to make the app better. I'm open for pull requests. It is pretty easy to add support for more miners or profit providers.
I did not test the app on Linux, so that would be something that would be helpful to do.

If you really liked the app you can donate me crypto coins (e.g. Monero) to one of the wallet addresses in the default config file.
You could also mine to a wallet address of mine, for example while benchmarking.

BTW: There are no fees from my side in this app :)

# Credits

* [TeamRedMiner](https://github.com/todxx/teamredminer)
* [XmRig](https://github.com/xmrig/xmrig)
* [Claymore](https://bitcointalk.org/index.php?topic=1433925.0)
* [CoinGecko API](https://www.coingecko.com/api/documentations/v3)
* [MineCryptoNight API](http://minecryptonight.net/api)
* [CryptUnit API](https://www.cryptunit.com/api/)
* [miner.rocks API](https://miner.rocks/)
* [HeroMiners API](https://herominers.com/)
* [MoneroOcean API](https://moneroocean.stream)
* [MineXmr API](https://minexmr.com/)
* [WhatToMine API](https://whattomine.com/)
* [NiceHash API](https://docs.nicehash.com/main/index.html)
* [NimiqX API](https://api.nimiqx.com/docs/about)
* [CsConsoleFormat](https://github.com/Athari/CsConsoleFormat)
* [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)

