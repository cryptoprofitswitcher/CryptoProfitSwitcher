@echo off
:: BatchGotAdmin
:: https://stackoverflow.com/questions/1894967/how-to-request-administrator-access-inside-a-batch-file
::-------------------------------------
REM  --> Check for permissions
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"

REM --> If error flag set, we do not have admin.
if '%errorlevel%' NEQ '0' (
    echo Requesting administrative privileges...
    goto UACPrompt
) else (
    goto gotAdmin
)

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    set params = %*:"="
    echo UAC.ShellExecute "cmd.exe", "/c %~s0 %params%", "", "runas", 1 >> "%temp%\getadmin.vbs"

    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
	pushd %~DP0
::--------------------------------------
TITLE JJs Reset and Run Tool for Vega
::
:: Author: TheJerichoJones on GitHub
::
:: Resets the Vega driver
:: Runs whatever Miner you like with the options you define
:: Run whatever VidTool you like with the options you define
:: Waits for input
:: Repeats the process
::
:: #### Begin Variables ####
::
:: Needs trailing backslash for MinerPath and VidTool1Path.
:: Don't quote the path even if it has spaces. That is done at runtime.
:: Actually don't quote any of these settings.
::
 
:: Other Tools. Delete or remark if not used.
SET VidTool1Path=.\
SET VidTool1=OverdriveNTool.exe -r1 -r2 -r3 -p1RX580 -p2BEAMvega56 -p3BEAMvega56
::
:: #### End Variables ####
::
:Start
START /W PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& {Get-PnpDevice| where {$_.friendlyname -like 'Radeon RX Vega'} | Disable-PnpDevice -ErrorAction Ignore -Confirm:$false | Out-Null}"
TIMEOUT /t 5
START /W PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& {Get-PnpDevice| where {$_.friendlyname -like 'Radeon RX Vega'} | Enable-PnpDevice -ErrorAction Ignore -Confirm:$false | Out-Null}"
IF DEFINED VidTool1 (
	START /W %VidTool1Path%%VidTool1%
)
CLS
echo.