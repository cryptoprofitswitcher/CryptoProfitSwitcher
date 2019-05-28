SRBMiner Cryptonight AMD GPU Miner V1.8.9
-----------------------------------------

============================================================================
Significant hashrate increase (up to 15%) on VEGA56/64/FE/VII
Small hashrate increase on RX cards
Please read the TWEAKING PROFILES section
============================================================================


Download:
http://www.srbminer.com/download.html
https://mega.nz/#F!qVIgxAwB!kKmgCDICmQwbdVvMb-tAag
https://drive.google.com/drive/folders/1W8V_mafptC-mV08MHjVEsB7BDzKOpE-1

BitcoinTalk thread:
https://bitcointalk.org/index.php?topic=3167363.0

If you are a beginner miner and need help in setting up SRBMiner, check out this link : https://www.srbminer.com/start.html
If you need help in setting up algo switching, check out this link : https://www.srbminer.com/algoswitching.html
Check out some video tutorials : https://www.srbminer.com/video-tutorials.html


To create a basic configuration run the guided-setup.bat script


SUPPORTED ALGOS:
----------------

- Cryptonight Alloy 			[alloy]
- Cryptonight ArtoCash 			[artocash]
- Cryptonight B2N 				[b2n]
- Cryptonight BitTubeV2 		[bittubev2]
- Cryptonight Conceal 			[conceal]
- Cryptonight Dark 				[dark]
- Cryptonight Fast 				[fast]
- Cryptonight Fast2 			[fast2]
- Cryptonight Fest 				[festival]
- Cryptonight Gpu 				[gpu]
- Cryptonight Graft 			[graft]
- Cryptonight Haven 			[haven]
- Cryptonight Heavy				[heavy]
- Cryptonight Hospital 			[hospital]
- Cryptonight Hycon 			[hycon]
- Cryptonight Italo 			[italo]
- Cryptonight Lite 				[lite]
- Cryptonight Lite V7 			[litev7]
- Cryptonight MarketCash 		[marketcash]
- Cryptonight Mox 				[mox]
- Cryptonight 					[normal]
- Cryptonight V4 (R) 			[normalv4]
- Cryptonight V4_64				[normalv4_64]
- Cryptonight V7 				[normalv7]
- Cryptonight V8 				[normalv8]
- Cryptonight StelliteV4 		[stellitev4]
- Cryptonight StelliteV5-V8-V9 	[stellitev8]
- Cryptonight Swap				[swap]
- Cryptonight Turtle 			[turtle]
- Cryptonight Upx 				[upx]
- Cryptonight Upx2 				[upx2]
- Cryptonight Wownero			[wownero]
- Cryptonight Xcash				[xcash]
- Cryptonight Zelerius 			[zelerius]




Supports Nicehash & SSL/TLS encrypted connections

Recommended drivers: Adrenalin 18.5.1, Adrenalin 18.6.1



DevFee:
- DevFee is very low, it's only ~ 0.85% (both in normal mode and in algo switching mode)
- Using the same algo as user for devfee mining, so there is no difference in algo settings, power usage, etc..


Extra:
- Guided setup mode for basic configuration
- Watchdog that monitors your GPU threads, if they stop hashing for a few minutes, miner restarts itself
- Hash monitor, if 5 minute average hash falls under the value you define, miner restarts itself
- Startup monitor, if miner can't init GPU's and start mining in a defined time, restarts itself or runs a user defined script
- Monitoring of GPU temperature, and if a critical temperature is reached, that particular GPU is turned off until it cools down by 15c
- Set system shutdown temperature, to protect your GPU's from overheating
- Restart (disable/enable) Vega gpu's with before mining starts
- API for rig monitoring
- Web based GUI interface for miner statistics
- Set compute mode and disable crossfire on all AMD cards with one command
- Benchmark every algo locally without connecting to a pool
- Auto switch to new algo on coin fork

 

--------------------------------------------------------------------------
CONFIG.TXT
--------------------------------------------------------------------------


You can use these options :
Note: use all lowercase characters

"cryptonight_type" : algo name or alias (algo name is the text between [] on the upper algo list)
"intensity" : 0-800, if set to 0 miner will try to find best settings for every video card
"worksize" : 1-256, if set, every video card will use this worksize, if not set, using auto detected value
"fragments" : 0,1,2,4,8,16,32,64,128. It can boost your hashrate if you find the right value for your GPU.
"heavy_mode" : 1,2 or 3. Method for doing calculations on heavy algos (Heavy, Haven, Swap, Italo, BitTube). Default is 3.
"old_mode" : true or false, create kernels with the old method, useful probably only on Vegas with algos that have a scratchpad <= 1MB (default is false)
"thread_delay" : 1-1000, delay to maintain between same gpu threads (default is 319)
"double_threads" : true or false, set it to true for best performance
"giveup_limit" : number, how many times to try connecting to a pool before switching to next pool from pools.txt. If set to 0 miner will quit and won't retry connecting.
"timeout" : number - seconds, when is a connection to a pool treated as time out
"retry_time" : number - seconds, how much to wait before trying to reconnect to a pool
"reboot_script_gpu_watchdog" : filename to a batch file in miner directory, if set it turns off built in miner reset procedure on gpu failure, and instead runs this script
"reboot_script_min_rig_speed" : filename to a batch file in miner directory, if set it turns off built in miner reset procedure on min_rig_speed trigger, and instead runs this script
"main_pool_reconnect" : number - seconds (minimum is 3 minutes or 180 sec), how often to try to reconnect back to the main pool. Default is 10 minutes.
"min_rig_speed" : number - in H/S, it defines the minimum rig hashing speed we want to maintain. If average hashing speed is less than this, miner restarts.
"min_rig_speed_duration" : number - in seconds (minimum is 30 sec), defines the time period for checking the average hashrate, used with 'min_rig_speed' parameter
"target_temperature" : number between 0-99, miner will try to maintain this temperature on all found video cards (ADL must be enabled, works only on cards supporting OverdriveN)
"shutdown_temperature" : number between 0-100, if this temperature is reached, miner will shutdown system (ADL must be enabled)
"off_temperature" : temperature in C, when to turn off GPU if it reaches this value. After value - 15, the GPU is turned on again automatically
"tweak_profile" : number 0-10 , applies tweaks to the GPU that can increase hashrate. 0 - not using any tweaks, 10 - max tweaking



=================================
Manual GPU setup (advanced)
=================================

To manually set up video cards, you must create a "gpu_conf" array in the config.txt file.

Example :

"gpu_conf" : 
[ 
	{ "id" : 0, "intensity" : 0 , "worksize" : 16, "threads" : 2},
	{ "id" : 1, "intensity" : 0 , "worksize" : 16, "threads" : 2},
	{ "id" : 2, "intensity" : 60, "worksize" : 8 , "threads" : 2},
	{ "id" : 3, "intensity" : 55, "worksize" : 8 , "threads" : 2}
]


Additional parameters: 

"fragments" : 0,1,2,4,8,16,32,64 or 128. It can boost your hashrate if you find the right value for your GPU
"heavy_mode" : 1,2 or 3. Method for doing calculations on heavy algos (Heavy, Haven, FreeHaven, Italo, BitTube). Default is 3.
"off_temperature" : temperature in C, when to turn off GPU if it reaches this value. After value - 15, the GPU is turned on again automatically
"target_temperature" : 0-99, if set miner will try to maintain this temperature for this particular video card. If option 'target_temperature' on top of config.txt is set, this option WILL BE IGNORED. (ADL must be enabled, works only on cards supporting OverdriveN)
"target_fan_speed" : 0-6000, if set miner will try to set the video card fan speed to this speed. Setting is in RPM (rounds per minute) (ADL must be enabled)
"adl_type" : 1 or 2 , 1 - USE OVERDRIVEN , 2 - USE OVERDRIVE 5. Default is 1 if not set. Option 2 (Overdrive 5) is suitable for older cards
"persistent_memory" : true or false, if set miner will try to allocate extra memory for the video card, if it is available. CAUTION, MINER CAN BECOME UNSTABLE AND CRASH if using this option
"old_mode" : true or false, create kernels with the old method, useful probably only on Vegas with algos that have a scratchpad <= 1MB (default is false)
"thread_delay" : 1-1000, delay to maintain between same gpu threads (default is 319)
"tweak_profile" : number 0-10 , applies tweaks to the GPU that can increase hashrate. 0 - not using any tweaks, 10 - max tweaking




============================================================================
TWEAKING PROFILES [VEGA56/64/FE/VII] & [RX 550/560, RX 470/480/570/580/590]
============================================================================

If you have VEGA56/64/FE/VII cards i recommend using the 'tweak_profile' parameter because it can significantly (up to 15%) increase your hashrate almost on every algorithm!
For RX series cards, the 'tweak_profile' option can bring some extra hash for your modded GPU.
It can be used on stock bios GPU's too, but the performance will be far from the performance of a modded bios.

Please read these VERY IMPORTANT things:

1. Miner must run with administrator privileges [right click on SRBMiner-CN.exe->properties->compatibility-> check 'Run this program as an administrator' option-> click OK button
2. Be patient, it takes some time to find optimal settings for your gpu/rig. Always test 1 card at a time.
3. If you start getting invalid shares or compute errors, that means the profile is too much for that gpu, so lower it. Or decrease your memory frequency.

To use in config file, use the "tweak_profile" parameter on top of config so the same profile is used for all cards, or in gpu_conf to set a different profile for every card.
Not every gpu can handle the same profile, so you need to find the right one for every gpu.

Tweak levels :

0 - no change, uses your original settings
1 - light tweak
2
3
4
10 - max tweak


The simplest mode to apply a tweak profile is by using + or - on your keyboard while the miner is running.


Example for cmdline setup [4 gpu]:
Gpu 0 uses profile 3
Gpu 1 uses profile 4
Gpu 2 uses profile 4
Gpu 3 uses profile 5

SRBMiner-CN.exe --ccryptonighttype normalv4 --cgpuid 0,1,2,3 --cgpuintensity 112,112,111,110 --cgpuworksize 16,16,16,16 --cgputhreads 2,2,2,2 --cgputweakprofile 3,4,4,5 --cpool xmr-eu1.nanopool.org:14444 --cwallet 4A5hJyu2FvuM2azexYssHW2odrNCNWVqLLmzCowrA57xGJLNufXfzVgcMpAy3YWpzZSAPALhVH4Ed7xo6RZYyw2bUtbm12g.donation


Same example as above, using config file setup:

{
"cryptonight_type" : "normalv4",
"intensity" : 0,
"double_threads" : true,
"gpu_conf" : 
[ 
	{ 
	  "id" : 0, 
	  "intensity" : 112,
	  "worksize" : 16,
	  "threads" : 2,
	  "tweak_profile" : 10
	},
	{ 
	  "id" : 1, 
	  "intensity" : 112,
	  "worksize" : 16,
	  "threads" : 2,
	  "tweak_profile" : 6
	},
	{ 
	  "id" : 2, 
	  "intensity" : 111,
	  "worksize" : 16,
	  "threads" : 2,
	  "tweak_profile" : 4
	},
	{ 
	  "id" : 3, 
	  "intensity" : 110,
	  "worksize" : 16,
	  "threads" : 2,
	  "tweak_profile" : 5
	}
]
}


--------------------------------------------------------------------------
POOLS.TXT
--------------------------------------------------------------------------


Example:

{
"pools" :
[
	{"pool" : "pool_1_address", "wallet" : "pool_1_wallet", "password" : "x"},
	{"pool" : "pool_2_address", "wallet" : "pool_2_wallet", "password" : "x"},
	{"pool" : "pool_3_address", "wallet" : "pool_3_wallet", "password" : "x"}
]
}

Pool on first position is the MAIN pool, others are counted as FAILOVER pools.

Additional parameters:

"worker" : worker name, not every pool supports this
"nicehash" : true or false, set this to true if you are using Nicehash
"keepalive" : true or false, not every pool supports this
"pool_use_tls": true or false, if true miner will use SSL/TLS to connect to pool
"job_timeout" : number in seconds, if no job is received for this period, miner will reconnect to the pool (Default is off)
"max_difficulty" : number, if pool difficulty is above this value miner will reconnect to the pool
"cryptonight_type" : if defined, and --usealgoswitching is used, it indicates the algo for the pool.

A short explanation of "cryptonight_type" and how to use it :

{
"pools" :
[
	{"pool" : "pool_1_address", "wallet" : "pool_1_wallet", "password" : "x", "cryptonight_type" : "haven"},
	{"pool" : "pool_2_address", "wallet" : "pool_2_wallet", "password" : "x", "cryptonight_type" : "normalv4"},
	{"pool" : "pool_3_address", "wallet" : "pool_3_wallet", "password" : "x", "cryptonight_type" : "heavy"},
	{"pool" : "pool_4_address", "wallet" : "pool_4_wallet", "password" : "x", "cryptonight_type" : "fast"}
]
}

Set the algo used on the pool with the "cryptonight_type" parameter.
Set the --usealgoswitching parameter in start.bat

If needed, miner will re-init with the pool algo before switching to the pool.
For example if you are mining on pool_1 using haven algo, and you manually switch to the next pool, miner will re-init with normalv4 algo
before connecting to pool_2. Also if there are connection errors to pool_1, and miner automatically switches to the next pool, if the algos
of pools aren't the same, miner will re-init with the defined cryptonight_type for pool_2.

IMPORTANT: 
Do not set 'cryptonight_type' parameter for pools if you are using a proxy for coin switching, or a pool with algo switching capabilities! (example Monero Ocean)


--------------------------------------------------------------------------
START.BAT
--------------------------------------------------------------------------

There are some options that must be set in start.bat, and not in config.txt or pools.txt.

Parameters:

--adldisable (disable ADL)
--algos filename (use algos file other than algos.txt)
--algoswitchmintime value (minimum time to mine using same algo, in seconds, min is 3 minutes, def. is 10 min. - pool must support it)
--apienable (enable statistics API)
--apiminerrestarturl value (user defined url which accessed in browser triggers miner restart)
--apiport value (port where statistics API is reachable - default 21555)
--apirigname value (identifier name for your rig in statistics API)
--apirigrestarturl value (user defined url which accessed in browser triggers computer restart)
--apirigshutdownurl value (user defined url which accessed in browser triggers computer shutdown)
--benchmarkalgos (benchmark all algos set in algos.txt and save results)
--config filename (use config file other than config.txt)
--disablegpuwatchdog (disable gpu crash detection watchdog)
--disablestartupmonitor (disable watchdog for miner startup interval)
--disabletweaking (disable gpu tweaking options, which are enabled by default)
--enablecoinforking (allows auto algo switch on coin fork)
--enableduplicategpuid (allows usage of same gpu id multiple times in gpu_conf)
--forcedshutdown (never try to free resources on restart/shutdown)
--giveuplimit value (how many times to try connecting to a pool before switching to the next pool)
--gpuerrorsalert value(notify when number of compute errors for any GPU reaches this value)
--gpuwatchdogdisablemode (if enabled, watchdog will try to disable crashed gpu, instead of restarting miner)
--listdevices (list available devices ordered by busid)
--logfile filename (enable logging to file)
--mainpoolreconnect value (how often to try to reconnect back to the main pool. def. is 10 minutes)
--maxnosharesent value(if no share is sent to the pool for x time, restarts miner. def. is 30 min)
--maxstartuptime value(max time to init gpu's and start mining, min 10 sec, max 10 min, def. 2 min)
--maxstartuptimescript filename(run this script if maxstartuptime is exceeded)
--minrigspeed value (the minimum rig hashing speed we want to maintain. If average hashing speed is less, miner restarts)
--nobinaries (don't use embedded binaries)
--nocache (don't save compiled binaries to disk)
--preparedata(do some precalculations that *may* increase hashing speed a little bit on weak gpu's)
--pools filename (use pools file other than pools.txt)
--randomnonce (create random nonce for every GPU thread)
--rebootscriptgpuwatchdog (filename, if set it turns off built in restart procedure on gpu failure, and instead runs this script)
--rebootscriptminrigspeed (filename, if set it turns off built in restart procedure on min_rig_speed trigger, and instead runs this script)
--retrytime value (how much to wait before trying to reconnect to a pool)
--resetfans (reset fans back to default settings on miner exit)
--resetmineronalgoswitch (if used miner will restart on every algo switch)
--resetvega (disable/enable Vega video cards before mining)
--setcomputemode (sets AMD gpu's to compute mode & disables crossfire - run as admin)
--showstales (display shares that were sent after a new job arrived)
--setup (guided setup mode to create basic configuration files)
--startupscript filename(run custom batch script before mining)
--shutdowntemperature value (if this temperature is reached, miner will shutdown system (ADL must be enabled))
--usealgomapping value (type of algo mappings to use: 1-short, 2-long, def. is short)
--usealgoswitching (use miner in algo switching mode - pool must support it)
--watchdogrounds value (after how many rounds (gpu watchdog round is 30 sec, min_rig_speed round is 1 min.) to trigger watchdog. def. is 5)


When setting any of the parameters, don't use " or ' around the value!
Parameters that take a time value must be set in seconds


How to access GUI HTTP statistics page?
----------------------------------
First you must enable API, by using the --apienable parameter in start.bat
Set your rig (computer) name with --apirigname myrigname also in start.bat

After you have started the miner, you can access the stats page in your browser :
http://127.0.0.1:21555/stats

There are also three other parameters that can help you to restart miner, reboot or shutdown your machine remotely :

--apirigrestarturl
This should be a unique string, which accessed in browser results in a computer restart. Miner needs to have admin privileges.

--apirigshutdownurl
This should be a unique string, which accessed in browser results in a computer shutdown. Miner needs to have admin privileges.

--apiminerrestarturl
This should be a unique string, which accessed in browser restart SRBMiner.



Example start.bat:
SRBMiner-CN.exe --config Config\config-normalv4.txt --pools Pools\pools-normalv4.txt --apienable --apirigrestarturl 12345fff --apirigshutdownurl 54321fff --apiminerrestarturl restart_my_srb

Visiting this url restarts your machine:
http://127.0.0.1:21555/12345fff

Visiting this url turns off your machine:
http://127.0.0.1:21555/54321fff

Visiting this url restarts SRBMiner:
http://127.0.0.1:21555/restart_my_srb


Of course if you are going to 'open' this HTTP interface to the world, you better use complex urls so nobody can guess them easilly and turn off your rig :)




To setup your video cards in cmd line :

These settings override the settings in config.txt, so you can't use both/mixed styles.
First list devices (--listdevices or --listdevicesreordered if you are going to use --gpureorder), then you know the GPU id's and can set them up easy.

Parameters:

--ccryptonighttype value (algo to use)
--cgpuid value (gpu id, comma separated values, use --listdevices to see available)
--cgpuintensity value (gpu intensity, comma separated values)
--cgputhreads value (number of gpu threads, comma separated values)
--cgpuworksize value (gpu worksize, comma separated values)
--cgpufragments value(can be 0,1,2,4,8,16,32,64,128, comma separated values)
--cgpuheavymode value (mode for heavy algos (1, 2, 3), comma separated values)
--cgputhreaddelay value (delay to maintain between same gpu threads, 1 - 1000, comma separated values)
--cgputargettemperature value (gpu temperature, comma separated values)
--cgputargetfanspeed value (gpu fan speed in RPM, comma separated values)
--cgpuofftemperature value (gpu turn off temperature, comma separated values)
--cgpuadltype value (gpu adl to use (1 or 2), comma separated values)
--cgpuoldmode value (old kernel creation mode - true or false, comma separated values)
--cgputweakprofile value(number from 0-10, where 0 means don't use tweaking)


To setup your main pool in cmd line :

If set here, this will be the MAIN pool, you can also add more pools in pools.txt config file.

Parameters:

--cworker value (worker name or rig id - pool must support it)
--cpool url:port (pool address:port without stratum prefix)
--cwallet address (user wallet address)
--cpassword value (pool password)
--ctls value (use SSL/TLS, true or false)
--cnicehash value (force nicehash, true or false)
--cjobtimeout value (number in seconds, if no job is received for this period, miner will reconnect to the pool)
--cmaxdifficulty value (number, if pool difficulty is above this value miner will reconnect to the pool)
--ckeepalive value (true or false, not every pool supports this)


EXAMPLES:

Use all GPU's with auto settings, and mine monero on nanopool :
SRBMiner-CN.exe --ccryptonighttype normalv4 --cpool xmr-eu1.nanopool.org:14444 --cwallet 4A5hJyu2FvuM2azexYssHW2odrNCNWVqLLmzCowrA57xGJLNufXfzVgcMpAy3YWpzZSAPALhVH4Ed7xo6RZYyw2bUtbm12g.donation

Use 1 GPU with id 0 , intensity 120, 2 threads on algo cryptonight v4 on nanopool:
SRBMiner-CN.exe --ccryptonighttype normalv4 --cgpuid 0 --cgpuintensity 112 --cgpuworksize 16 --cgputhreads 2 --cpool xmr-eu1.nanopool.org:14444 --cwallet 4A5hJyu2FvuM2azexYssHW2odrNCNWVqLLmzCowrA57xGJLNufXfzVgcMpAy3YWpzZSAPALhVH4Ed7xo6RZYyw2bUtbm12g.donation

Use 5 GPUS with id 0,1,2,3,4 , intensities 56,56,55,58,55, 2 threads for each GPU, on algo cryptonight v4 on nanopool:
SRBMiner-CN.exe --ccryptonighttype normalv4 --cgpuid 0,1,2,3,4 --cgpuworksize 16,16,16,16,16 --cgpuintensity 56,56,55,58,55 --cgputhreads 2,2,2,2,2 --cpool xmr-eu1.nanopool.org:14444 --cwallet 4A5hJyu2FvuM2azexYssHW2odrNCNWVqLLmzCowrA57xGJLNufXfzVgcMpAy3YWpzZSAPALhVH4Ed7xo6RZYyw2bUtbm12g.donation





--------------------------------------------------------------------------
ALGO SWITCHING MODE
--------------------------------------------------------------------------

There are 2 types of algo switching supported :

Proxy or pool controlled 

This means the mining pool or proxy (software between your miner and the pool) decides what algo should be mined.
For example Monero Ocean pools support auto algo switching based on profitability. You can read everything about it on their FAQ page.

You can also use software which allows you to change the mining algorithm with a simple mouse click. One example is Hesido's Cryptonote Proxy tool.


Manually controlled 

In this mode the user sets the algorithm (cryptonight_type) for every pool he has in pools.txt.
When manually changing pools (by pressing 'o' or 'p'), miner will automatically re-init with the algo he set in pools.txt for that particular pool.
This also means you can set failover pools using any of the supported algorithms.


To use the algo switching mode (--usealgoswitching parameter in .bat) with pool controlled method the pool must support the XMRig Proxy Stratum protocol extensions(https://github.com/xmrig/xmrig-proxy/blob/master/doc/STRATUM_EXT.md)


Steps that are common for both switching types: 

1.] You can edit the file 'algos.txt', which contains entries for every algo you want the pool to send you jobs for (if using pool controlled switching). 
If you are using the manually controlled algo switching, setting up this file is important because of the various configurations that every algo has. 
The 'hashrate' parameter in this case isn't used.

You need to edit the 'hashrate' value if using pool controlled switching, and 'startup_script' if you are going to use it. (you can read what it is for under this text)
Also if you want, you can fully customize configs for every algo. You can find them in the 'Config' folder.


Short explanation of parameters in algos.txt:

+ "config" expects a filename to a config file where you set up everything for a specific algo (config-normalv4.txt is for CNV4 as you can guess).
So on every new algo switch SRBMiner will initialize settings specific for that algo.

+ "hashrate" is a parameter where you should write the total hashrate of your rig (in H/S) for that specific algo (you can easily find it out by running a benchmark for example).
This field is used by the pool in calculating what is the most profitable algo for you to mine.

+ "startup_script" can be used to run a script (batch file) before miner starts.
You can for example set clocks, voltages, etc. specific to that algo

You can easily benchmark the hashrates by running benchmark-algos.bat or by typing this into a command prompt:
SRBMiner-CN.exe --benchmarkalgos

Notice: Tweaking parameters can not be used in benchmarking mode!

This process will take a few minutes, and when finished it will create a file benchmark_results.txt with the hashrate for every algo you benchmarked.
Now you need to take and copy-paste those values into algos.txt under the right algorithm section.
The benchmarking procedure should be used only if using pool controlled algo switching. 


2.] There is a 'pools-algoswitch.txt' file where i predefined a few pools on Monero Ocean (algo switching pool), where i put my donation address as an example wallet,
so you should change this wallett address to your own.

If you are using the manually controlled pool switching, then you would define your own pools here.
You just need to add one new parameter (cryptonight_type) to each pool entry.

Here is an example how 'pools-algoswitch.txt' could look like:

{
"pools" :
[
{"pool" : "pool_1_address", "wallet" : "pool_1_wallet", "password" : "x", "cryptonight_type" : "haven"},
{"pool" : "pool_2_address", "wallet" : "pool_2_wallet", "password" : "x", "cryptonight_type" : "normalv4"},
{"pool" : "pool_3_address", "wallet" : "pool_3_wallet", "password" : "x", "cryptonight_type" : "heavy"},
{"pool" : "pool_4_address", "wallet" : "pool_4_wallet", "password" : "x", "cryptonight_type" : "fast"}
]
}

Now that you have algos.txt and pools-algoswitch.txt set up, start the miner by running start-algo-switching.bat

If you are using the pool controlled algo switching, DON'T set cryptonight_type for that pool, because you could get into an endless loop of algo switching :)


NOTICE:
If 'tweak_profile' is not specified it defaults to 0 after algo switch, so make sure you set it in every config file where you want to use it!


TIPS:
There are two algo switching modes available : with or without miner restart.

The 'miner restart' mode is recommended for Vega video cards, because they tend to drop mining speed on algo switching sometimes.
Using the 'miner restart mode' with the --resetvega parameter you are ensuring always maximum mining speeds.

Example:

SRBMiner-CN.exe --config Config\config-normalv4.txt --pools pools-algoswitch.txt --usealgoswitching --resetmineronalgoswitch --resetvega


--------------------------------------------------------------------------
KEYBOARD SHORTCUTS
--------------------------------------------------------------------------


- Press 's' to see some basic stats
- Press 'h' to see hashing speed
- Press 'r' to reload pools
- Press 'p' to switch to the next pool
- Press 'o' to switch to the previous pool
- Press number from 0-9 to disable/enable from gpu0-gpu9, then shift+0 for gpu10, shift+1 for gpu11..etc. until gpu19 max (use US keyboard where SHIFT+1 = !, SHIFT+2 = @ ..etc..)
- Press + or - to change tweak profile


--------------------------------------------------------------------------
INFORMATIONS, NOTES AND LICENSES
--------------------------------------------------------------------------

You have to change the wallet address in sample config.txt file to your own, or you will donate some hashing power to me. (Thanks)

This software uses part of WinIO library from Yariv Kaplan. Copyright and all rights on this file (WinIo64.sys)
Special thanks goes to ElioVP for sharing informations that helped me in developing the tweaking profiles.








WinIO LICENSE
==============

END USER LICENSE AGREEMENT

Software License Agreement for WinIo
The following terms apply to all files associated with the software unless
explicitly disclaimed in individual files.

IMPORTANT- PLEASE READ CAREFULLY: BY INSTALLING THE SOFTWARE (AS DEFINED BELOW),
OR COPYING THE SOFTWARE, YOU (EITHER ON BEHALF OF YOURSELF AS AN INDIVIDUAL OR
ON BEHALF OF AN ENTITY AS ITS AUTHORIZED REPRESENTATIVE) AGREE TO ALL OF THE
TERMS OF THIS END USER LICENSE AGREEMENT ("AGREEMENT") REGARDING YOUR USE OF
THE SOFTWARE. IF YOU DO NOT AGREE WITH ALL OF THE TERMS OF THIS AGREEMENT, DO
NOT INSTALL, COPY OR OTHERWISE USE THE SOFTWARE.

1. GRANT OF LICENSE: Subject to the terms below, Yariv Kaplan ("AUTHOR") hereby
grants you a non-exclusive, non-transferable, non-assignable license to install
and to use the downloadable version of WinIo ("SOFTWARE").

a. Redistributable Code. You may reproduce and distribute the object code form
of the SOFTWARE solely in conjunction with, and as part of, your application
("Permitted Application"); provided that you comply with the following:

If you redistribute any portion of the Redistributable Code, you agree that:

(i) you will only distribute the Redistributable Code in conjunction with, and
as part of, your Permitted Application which adds significant functionality to
the Redistributable Code and that distribution of the Permitted Application does
not compete with the AUTHOR's distribution of the SOFTWARE;

(ii) you will include a valid copyright notice on your Permitted Application;

(iii) you will not permit further redistribution of the Redistributable Code;

(iv) you will indemnify, hold harmless, and defend the AUTHOR from and against
any claims or lawsuits, including attorneys' fees, that arise or result from
the use or distribution of your Permitted Application.

b. License to use Source Code. You may not sell, lease, rent, transfer or
sublicense the source code of this SOFTWARE.

2. MODIFICATION: SOFTWARE Source Code may be modified without the prior written
permission of the AUTHOR. Any modifications made to the SOFTWARE will continue
to be subject to the terms and conditions of this AGREEMENT.

3. COPYRIGHT: All rights, title, and copyrights in and to the SOFTWARE and any
copies of the SOFTWARE are owned by the AUTHOR. The SOFTWARE is protected by
copyright laws and international treaty provisions. Therefore, you must treat
the SOFTWARE like any other copyrighted material.

4. TITLE: You acknowledge that no title to the intellectual property in the
SOFTWARE is transferred to you. Title, ownership, rights, and intellectual
property rights in and to the SOFTWARE shall remain the exclusive property of
the AUTHOR. The SOFTWARE is protected by copyright laws of the United States
and international treaties.

5. LIMITATION OF LIABILITY: You must assume the entire risk of using the
SOFTWARE.

IN NO EVENT SHALL THE AUTHOR BE LIABLE TO ANY PARTY FOR DIRECT, INDIRECT,
SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES ARISING OUT OF THE USE OF THIS
SOFTWARE, ITS DOCUMENTATION, OR ANY DERIVATIVES THEREOF, EVEN IF THE AUTHOR
HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

THE AUTHOR SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING, BUT NOT LIMITED
TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE, AND NON-INFRINGEMENT. THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS,
AND THE AUTHOR HAS NO OBLIGATION TO PROVIDE MAINTENANCE, SUPPORT, UPDATES,
ENHANCEMENTS, OR MODIFICATIONS.
