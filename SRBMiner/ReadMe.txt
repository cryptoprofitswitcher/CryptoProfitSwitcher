SRBMiner Cryptonight AMD GPU Miner V1.6.0
-------------------------------------------

Download:
https://mega.nz/#F!qVIgxAwB!kKmgCDICmQwbdVvMb-tAag

Supports:

- Cryptonight
- Cryptonight V7
- Cryptonight Lite
- Cryptonight Lite V7
- Cryptonight Heavy
- Cryptonight IPBC
- Cryptonight ArtoCash
- Cryptonight Alloy
- Cryptonight MarketCash
- Cryptonight B2N
- Cryptonight StelliteV4
- Cryptonight Fast
- Cryptonight Haven


Supports Nicehash & SSL/TLS connections

For best results use Blockchain compute drivers (https://support.amd.com/en-us/kb-articles/Pages/Radeon-Software-Crimson-ReLive-Edition-Beta-for-Blockchain-Compute-Release-Notes.aspx)
or Radeon Adrenalin 17 or newer drivers.
ADL tested & working with Radeon Software Crimson ReLive Edition Beta for Blockchain Compute Driver Version 17.30.1029


What can this miner offer you beside usual mining functionalities:


DevFee:
- Low DevFee (0.85%) -> every ~2 hours 1 minute mining for the dev
- Non-agressive DevFee mining -> if miner can't connect to DevFee pool, no problem, switching back to user pool ASAP


Performance:
- Only one parameter you have to play with : intensity (0 - 300)
- Leave intensity on 0, and miner will try to set intensity automatically for your GPU
- To get even better results, there is an option to use double threads


Extra:
- Watchdog that monitors your GPU threads, if they stop hashing for a few minutes, miner restarts itself
- Hash monitor, if 5 minute average hash falls under the value you define, miner restarts itself
- Set system shutdown temperature, to protect your GPU's from overheating
- Restart (disable/enable) Vega gpu's with devcon before mining
- API for rig monitoring

 
Tips:
- If you leave intensity on 0 it will play safe, so in many cases you can increase that value to get better results
- For better results set double_threads to true, and leave intensity on 0
- If you get an error that says it can't create scratchpad buffer, you have to lower intensity
- Largest intensity setting won't always give you the best hashrate. Experiment and find the best setting for your GPU.


How to set it up ?

The config file :

"cryptonight_type" : "NORMAL, NORMALV7, LITE, LITEV7, HEAVY, IPBC, ARTOCASH, ALLOY, MARKETCASH, B2N OR STELLITEV4"
"intensity" : A NUMBER BETWEEN 0-300
"double_threads" : TRUE OR FALSE


Optional parameters :

"giveup_limit" : HOW MANY TIMES TO TRY CONNECTING TO A POOL BEFORE SWITCHING TO NEXT POOL
"timeout" : WHEN IS A CONNECTION TO POOL TREATED AS TIMED OUT , IN SECONDS
"retry_time" : HOW MUCH TO WAIT TILL RECONNECTING WHEN DISCONNECTED FROM POOL, IN SECONDS
"reboot_script" : FILENAME, TURN OFF BUILT IN WATCHDOG AND INSTEAD RUN A USER DEFINED .BAT FILE ON GPU FAILURE
"restart_devices_on_startup" : IF TRUE IT WILL USE DEVCON TO DISABLE/ENABLE EVERY VEGA GPU IN YOUR MACHINE BEFORE MINING STARTS
"restart_devices_on_startup_script" : FILENAME, THIS SCRIPT RUNS AFTER VEGA GPU ENABLE/DISABLE PROCESS, GOOD FOR SETTING UP OVERCLOCKING
"main_pool_reconnect" : IN SECONDS (MINIMUM 3 MIN, 180 SEC), HOW OFTEN TO TRY TO RECONNECT TO MAIN POOL, WHEN MINING ON A POOL OTHER THAN MAIN POOL, DEFAULT IS 10 MINUTES (600 SEC)
"min_rig_speed" : IN H/S, IT DEFINES THE MINIMUM HASHING SPEED WE WOULD LIKE TO MAINTAIN. IF 5 MIN AVERAGE HASHING SPEED IS LESS THAN WHAT WE DEFINED, MINER RESTARTS


API:
"api_enabled" : TRUE OR FALSE
"api_rig_name" : IDENTIFIER FOR YOUR RIG
"api_port" : PORT ON WHICH THE REST API RUNS (DEFAULT IS 21555 IF NOT SET)


AMD Overdrive API supported GPUs can use :
"target_temperature" : A NUMBER BETWEEN 0-99, MINER WILL TRY TO MAINTAIN THIS TEMPERATURE FOR GPUS (ADL TYPE 1 (OVERDRIVEN) ONLY)
"shutdown_temperature" : A NUMBER BETWEEN 0-100, IF THIS TEMPERATURE IS REACHED, MINER WILL SHUTDOWN SYSTEM (ADL MUST BE ENABLED)



#SET GPU'S MANUALLY
#This example uses GPU devices with ID 0,1,3,4 and every GPU has it's own setting

"gpu_conf" : 
[ 
	{ "id" : 0, "intensity" : 80, "worksize" : 8, "threads" : 1},
	{ "id" : 1, "intensity" : 40, "worksize" : 8, "threads" : 2},
	{ "id" : 3, "intensity" : 30, "worksize" : 8, "threads" : 2},
	{ "id" : 4, "intensity" : 90, "worksize" : 8, "threads" : 1}
]

Some additional parameters you can use in gpu_conf:

"kernel" : 0-4 , IF 0, MINER WILL SELECT MOST SUITABLE KERNEL, OTHERS ARE : 1-FOR GCN CARDS, 2-FOR PRE-GCN CARDS, 3-FOR PRE-GCN EXPERIMENTAL 1, 4-FOR PRE-GCN EXPERIMENTAL 2
"target_temperature" : A NUMBER BETWEEN 0-99, MINER WILL TRY TO MAINTAIN THIS TEMPERATURE FOR GPU. IF TARGET_TEMPERATURE OPTION NOT SET TO ZERO ON CONFIG TOP, THIS SETTING IS IGNORED (ADL TYPE 1 (OVERDRIVEN) ONLY)
"target_fan_speed" : A NUMBER BETWEEN 0-6000, THE RPM (ROUNDS PER MINUTE) SPEED FOR FAN. (ADL MUST BE ENABLED)
"adl_type" : 1 OR 2 , 1 - USE OVERDRIVEN , 2 - USE OVERDRIVE 5, DEFAULT IS 1 IF NOT SET, IF YOU DON'T HAVE TEMP OR CLOCKS DISPLAYED, TRY USING 2 (MOSTLY FOR OLDER CARDS)
"persistent_memory" : TRUE OR FALSE, IF TRUE TRIES TO ALLOCATE EXTRA MEMORY FOR THE GPU IF AVAILABLE - CAUTION, MINER CAN BECOME UNSTABLE AND CRASH IN SOME OCCASIONS  


#IF USING --usecpuopencl YOU MUST SET CPU_CONF IN CONFIG FILE#
#ALSO IF NOT SET OPTIMALLY, CPU OpenCL MINING DECREASES GPU'S PERFORMANCE#
#I FOUND ON MY BUILTIN INTEL 3000 GPU THAT 1/7/1 IS THE BEST SETTING, GIVING FASCINATING 30 H/S, MAKING CPU WORK ON 75% AND NOT DECREASING GPU PERFORMANCE#

"cpu_conf":
[
	{ "intensity" : 1, "worksize" : 1, "threads" : 1}
]

Some additional parameters you can use in cpu_conf:

"platform" : IF AUTO PLATFORM SELECTOR FAILS, YOU CAN ENTER PLATFORM NUMBER BY HAND



The pools file:

{
"pools" :
[
	{"pool" : "pool1address", "wallet" : "pool1wallet", "password" : "x"},
	{"pool" : "pool2address", "wallet" : "pool2wallet", "password" : "x"},
	{"pool" : "pool3address", "wallet" : "pool3wallet", "password" : "x"}
]
}

Some additional parameters you can use in pools config:

"worker" : STRING, WORKER NAME OR RIG ID, POOL MUST SUPPORT IT
"nicehash" : TRUE OR FALSE, IF TRUE IT FORCES NICEHASH. USE IF AUTO DETECTION FAILS OR YOU USE A PROXY TO CONNECT TO NICEHASH POOLS
"keepalive" : TRUE OR FALSE, POOL MUST SUPPORT KEEPALIVE COMMAND
"pool_use_tls": TRUE OR FALSE, SET TO TRUE IF CONNECTING TO A SSL/TLS POOL
"job_timeout" : NUMBER IN SECONDS, IF NO NEW JOB RECEIVED FOR 'job_timeout' SECONDS, MINER RECONNECTS TO THE POOL TO GET NEW JOB, IF NOT SET DEFAULT IS 900 (15 MIN)
"max_difficulty" : NUMBER, IF POOL DIFFICULTY IS ABOVE THIS VALUE, MINER WILL DISCONNECT AND RECONNECT TO THE POOL


If you want to create a separate config and pools for a different coin, use --config and --pools parameter.


Parameters (go in .bat):

--config filename (use config file other than config.txt)
--pools filename (use pools file other than pools.txt)
--logfile filename (enable logging to file)
--listdevices (list available devices)
--listdevicesreordered (list available devices ordered by busid)
--gpureorder (order devices by busid)
--adldisable (disable ADL)
--disablegpuwatchdog (disable gpu crash detection watchdog)
--listcpudevices (list available CPU OpenCL device)
--usecpuopencl (use CPU OpenCL if available - not efficient)
--resetfans (reset fans back to default settings on miner exit)



Pool related parameters:

--cworker value (worker name or rig id - pool must support it)
--cpool url:port (pool address:port without stratum prefix)
--cwallet address (user wallet address)
--cpassword value (pool password)
--ctls value (use SSL/TLS, true or false)
--cnicehash value (force nicehash, true or false)


Options:

- Press 's' to see some basic stats
- Press 'h' to see hashing speed
- Press 'p' to fast switch to next pool from pools config file
- Press 'r' to reload pools, if you add a pool to pools config, no need to restart miner to use new pool
- Press number from 0-9 to disable/enable from gpu0-gpu9, then shift+0 for gpu10, shift+1 for gpu11..etc. until gpu19 max (use US keyboard where SHIFT+1 = !, SHIFT+2 = @ ..etc..)


Info:
You have to change the wallet address in sample config.txt file, if you leave it, you will donate some hashing power to me.