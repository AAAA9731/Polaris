# 发布单文件自包含安装器：玩家双击 PolarisInstaller.exe 即可，无需另装 .NET。
# 产物：Installer/publish/PolarisInstaller.exe
param([string]$Configuration = "Release")

$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot
try {
    dotnet publish PolarisInstaller.csproj -c $Configuration -o publish
    Get-Item publish\PolarisInstaller.exe | Select-Object Name, @{n = "MB"; e = { [math]::Round($_.Length / 1MB, 1) } }
}
finally {
    Pop-Location
}
