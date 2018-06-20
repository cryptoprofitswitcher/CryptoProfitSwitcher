param (
[string]$TargetDir,
[string]$ConfigurationName
)
Write-Host "MAKING PLATFORM SPECIFIC CHANGES"
Write-Host "Target dir: $TargetDir"
Write-Host "Configuration: $ConfigurationName"
$configFiles = Get-ChildItem $TargetDir *.json -rec
foreach ($file in $configFiles)
{
	If ($ConfigurationName -like "*Linux*")
	{
		(Get-Content $file.PSPath) |
		Foreach-Object { $_ -replace '\$PlatformSpecificXmrStakPath', "XmrStak/xmr-stak" } |
		Set-Content $file.PSPath
	}
	Else
	{
		(Get-Content $file.PSPath) |
		Foreach-Object { $_ -replace '\$PlatformSpecificXmrStakPath', "XmrStak/xmr-stak.exe" } |
		Set-Content $file.PSPath
	}
    
}