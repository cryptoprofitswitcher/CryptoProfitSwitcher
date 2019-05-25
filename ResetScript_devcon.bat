
REM You could for example reset your GPUs using devcon.exe (Useful for hashdrops that can happen with Vega GPUs)

REM devcon.exe disable "PCI\VEN_1002&DEV_687F&SUBSYS_2388148C&REV_C3"
REM timeout /t 2
REM devcon.exe enable "PCI\VEN_1002&DEV_687F&SUBSYS_2388148C&REV_C3"
REM timeout /t 2
REM devcon.exe disable "PCI\VEN_1002&DEV_687F&SUBSYS_0B361002&REV_C1"
REM timeout /t 2
REM devcon.exe enable "PCI\VEN_1002&DEV_687F&SUBSYS_0B361002&REV_C1"
