# 把编译 PolarisCore 所需的游戏程序集打成一个 zip，供 CI 下载（密钥 AIC_MANAGED_URL 指向它）。
# 这些是游戏的版权文件：请只放在你自己控制的私有位置，不要提交进仓库、不要放到公开地址。
#
# 用法：  ./tools/pack-managed.ps1 -GameDir "D:\Games\AliceInCradle" [-Out aic-managed.zip]
param(
    [Parameter(Mandatory = $true)][string]$GameDir,
    [string]$Out = "aic-managed.zip"
)

$ErrorActionPreference = "Stop"
$managed = Join-Path $GameDir "AliceInCradle_Data\Managed"
if (-not (Test-Path $managed)) { throw "找不到 $managed，请确认 -GameDir 指向含 AliceInCradle_Data 的目录。" }

# 与 build/Polaris.Runtime.props 里引用的游戏程序集保持一致。
$names = @("Assembly-CSharp", "unsafeAssem", "better", "pixelliner", "CriMw.CriWare.Runtime", "Newtonsoft.Json")
$files = foreach ($n in $names) {
    $p = Join-Path $managed "$n.dll"
    if (-not (Test-Path $p)) { throw "缺少 $p" }
    $p
}

if (Test-Path $Out) { Remove-Item $Out }
Compress-Archive -Path $files -DestinationPath $Out
$hash = (Get-FileHash $Out -Algorithm SHA256).Hash
Write-Host ("已生成 {0}（{1:N1} MB），SHA256 {2}" -f $Out, ((Get-Item $Out).Length / 1MB), $hash)
