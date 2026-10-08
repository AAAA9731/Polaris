# 安装器冒烟测试：在一个假的游戏目录上跑 静默安装 → 再装 → 卸载 → 连 BepInEx 卸载，检查文件结果。
# CI 与本地都用：  ./tools/ci-smoke.ps1 -Installer Installer\publish\PolarisInstaller.exe
param([Parameter(Mandatory = $true)][string]$Installer)

$ErrorActionPreference = "Stop"
$exe = (Resolve-Path $Installer).Path
$fake = Join-Path ([System.IO.Path]::GetTempPath()) ("aic-smoke-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force "$fake\AliceInCradle_Data", "$fake\BepInEx\plugins" | Out-Null
Set-Content "$fake\AliceInCradle.exe" "exe"
Set-Content "$fake\winhttp.dll" "original"           # 玩家自己的文件，安装应备份、卸载应还原
Set-Content "$fake\BepInEx\plugins\OtherMod.dll" "x"  # 别的模组，任何时候都不能被碰

function Run($action, [string[]]$extra = @()) {
    $log = Join-Path (Split-Path $exe) "polaris-installer.log"
    if (Test-Path $log) { Remove-Item $log }
    $argList = @("--silent", $action, "--game", "`"$fake`"")
    if ($extra) { $argList += $extra }
    $p = Start-Process $exe -ArgumentList $argList -WindowStyle Hidden -Wait -PassThru
    if ($p.ExitCode -ne 0) { throw "$action 失败，退出码 $($p.ExitCode)：" + (Get-Content $log -Raw) }
}
function Assert($cond, $msg) { if (-not $cond) { throw "断言失败：$msg" } }

Run "install"
Assert (Test-Path "$fake\BepInEx\plugins\PolarisCore.dll") "PolarisCore.dll 已安装"
Assert (Test-Path "$fake\BepInEx\plugins\PolarisLib.dll") "PolarisLib.dll 已安装（plugins 根目录）"
Assert (Test-Path "$fake\BepInEx\plugins\Polaris\PolarisWatcher.exe") "PolarisWatcher.exe 已安装"
$dependencies = @('NVorbis.dll', 'System.Buffers.dll', 'System.Memory.dll', 'System.Numerics.Vectors.dll', 'System.Runtime.CompilerServices.Unsafe.dll')
foreach ($dependency in $dependencies) {
    Assert (Test-Path "$fake\BepInEx\plugins\Polaris\$dependency") "$dependency 已安装"
}
Assert (Test-Path "$fake\BepInEx\plugins\Polaris\polaris_star.png") "polaris_star.png 已安装"
Assert (Test-Path "$fake\BepInEx\core\BepInEx.Unity.Mono.dll") "BepInEx 已安装"
Assert ((Get-Content "$fake\winhttp.dll" -Raw) -ne "original`r`n") "winhttp.dll 已被替换"

Run "install"   # 重复安装应幂等
$backups = @(Get-ChildItem "$fake\BepInEx\Polaris\installer-backup" -Directory)
Assert ($backups.Count -eq 1) "重复安装不应再产生备份（实际 $($backups.Count) 份）"

Run "uninstall"
Assert (-not (Test-Path "$fake\BepInEx\plugins\PolarisCore.dll")) "Polaris 已卸载"
Assert (-not (Test-Path "$fake\BepInEx\plugins\PolarisLib.dll")) "PolarisLib.dll 已卸载"
foreach ($dependency in $dependencies) {
    Assert (-not (Test-Path "$fake\BepInEx\plugins\Polaris\$dependency")) "$dependency 已卸载"
}
Assert (Test-Path "$fake\BepInEx\core\BepInEx.Unity.Mono.dll") "只卸 Polaris 时不动 BepInEx"

Run "uninstall" @("--remove-bepinex")
Assert ((Get-Content "$fake\winhttp.dll" -Raw) -eq "original`r`n") "原来的 winhttp.dll 已还原"
Assert (Test-Path "$fake\BepInEx\plugins\OtherMod.dll") "其它模组没被碰"
Assert (-not (Test-Path "$fake\BepInEx\core")) "BepInEx 已卸干净"

$resolvedFake = (Resolve-Path -LiteralPath $fake).Path
$temporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\') + '\'
if (-not $resolvedFake.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "冒烟测试目录不在临时目录下：$resolvedFake"
}
Remove-Item -LiteralPath $resolvedFake -Recurse -Force
Write-Host "安装器冒烟测试通过。"
