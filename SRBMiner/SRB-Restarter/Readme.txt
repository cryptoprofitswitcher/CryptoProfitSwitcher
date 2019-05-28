SRB-Restarter is a tool that monitors the main SRBMiner process, and if it's not running for some reason (like an app crash for example), it will run the script you specify.


Only 3 parameters :

--interval value_in_seconds [def. is 10]
Specifies how often to check is the SRBMiner process running

--instance number
Use it if you run multiple SRBMiner instances on the same machine, example is below
You must use the following file naming :
SRBMiner-CN-n.exe where n is the instance number

--run filename [name of .bat file to run]
Name of .bat you want to run if SRBMiner process in not running


Note:
Miner is considered 'not running' after 2 checks!
If for example the interval is 5 seconds, then miner is considered 'not running' after 2*5 seconds = 10 seconds

Examples:

1.
SRB-Restarter.exe
[This will set interval to 10 seconds, and start.bat if SRBMiner process is not running. It will monitor for a process named SRBMiner-CN.exe]

2.
SRB-Restarter.exe --interval 5 --run turtle-coin.bat
[This will set interval to 5 seconds and run turtle-coin.bat if SRBMiner process is not running. It will monitor for a process named SRBMiner-CN.exe]

3.
SRB-Restarter.exe --interval 30 --run start-heavy.bat --instance 1
[This will set interval to 30 seconds and run start-heavy.bat if SRBMiner process is not running. It will monitor for a process named SRBMiner-CN-1.exe]