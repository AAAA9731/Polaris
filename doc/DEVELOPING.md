# 开发者文档

这篇文档讲的是怎么构建 Polaris、CI 怎么跑、怎么发布版本，以及仓库的目录结构。玩家向的说明在 [README](../README.md)。

## 项目组成

| 部分 | 位置 | 作用 |
|---|---|---|
| PolarisCore（BepInEx 插件） | 仓库根目录 | 游戏内：错误捕获与归因、阶梯式提示、标题画面的模组管理页、自动更新 |
| PolarisWatcher（独立小程序，.NET Framework 4.8） | `Watcher/` | 游戏退出后：弹出崩溃原因窗口、应用已下载的更新 |
| PolarisInstaller（WPF 单文件安装器） | `Installer/` | 安装、更新、卸载 BepInEx 与 Polaris；嵌入了 BepInEx 文件和刚构建好的插件与 Watcher |

## 目录结构

```
Diagnostics/     诊断：捕获、归因、看门狗、会话哨兵、严重度策略、游戏内提示、模组标记
SelfUpdate/      游戏内自动更新
Contracts/       诊断的公开数据类型
Infra/           错误、健康、路径等基础设施
Settings/        设置项框架：给静态字段标 `[PolarisSetting]` 即可渲染进原版设置界面并自动保存；`PolarisSettings.cs` 是 Polaris 自己的设置项
Patch/           标题画面补丁（模组管理页入口、告知页）与设置界面（UiCFG）补丁
Localization/    内置三语文案（含设置项文案）
Watcher/         PolarisWatcher.exe
Installer/       PolarisInstaller（Payload/ 里是随包分发的 BepInEx 文件）
tools/           pack-managed.ps1（打包游戏程序集）、ci-smoke.ps1（安装器冒烟测试）
doc/legacy/      旧库的规格与设计文档
.github/         GitHub Actions 工作流
```

## 本地构建

1. 在仓库**上一级目录**创建 `aic_path.txt`，单行写游戏根目录（含 `AliceInCradle_Data` 的那一层）。
2. 构建插件：`dotnet build PolarisCore.csproj`
3. 构建崩溃观察者：`dotnet build Watcher/PolarisWatcher.csproj`
4. 发布安装器：运行 `Installer/publish.ps1`。它会先构建上面两项并嵌入，产物是 `Installer/publish/PolarisInstaller.exe`。

只有 PolarisCore 需要游戏目录；Watcher 和 Installer 可以在没有游戏的机器上构建（安装器需要先有插件，所以完整发布仍然要游戏程序集）。

安装器还支持无界面模式，方便脚本调用：

```
PolarisInstaller.exe --silent install|uninstall --game "<游戏目录>" [--remove-bepinex]
```

结果会写进安装器旁边的 `polaris-installer.log`，退出码 0 表示成功。

## CI 与发布

| 工作流 | 触发 | 做什么 |
|---|---|---|
| `ci.yml` | 推送到 `main`、`refactor/`、`feat/`、`fix/` 分支，以及所有 PR | 构建 PolarisWatcher；有游戏程序集时再构建插件、发布安装器并跑冒烟测试，产物上传为 artifact |
| `release.yml` | 推送 `v*` 标签 | 同上（必须有游戏程序集），核对标签与 `<Version>` 一致后创建 GitHub Release，附上安装器、`PolarisCore-manual.zip` 及各自的 `.sha256` |

### 让 CI 拿到游戏程序集

编译插件需要游戏的程序集（Assembly-CSharp 等）。它们是游戏的版权文件，不能放进本仓库，所以：

1. 运行 `tools/pack-managed.ps1 -GameDir "<游戏目录>"`，得到 `aic-managed.zip`（约 4 MB，只含 6 个原样复制的 dll，不做任何改动）。
2. 把它放进一个**私有**仓库（目前是维护者个人名下的 `AAAA9731/aic-managed-refs`，文件名 `aic-managed.zip`）。**不要公开。**
3. 给这个私有仓库加一个**只读的 deploy key**，私钥存为本仓库的 Actions 密钥 `AIC_MANAGED_KEY`。CI 用它拉取私有仓库，不需要个人访问令牌。

没有 `AIC_MANAGED_KEY`（比如来自 fork 的 PR）时，CI 只构建不依赖游戏的 PolarisWatcher，插件和安装器的步骤会被跳过并给出提示。游戏更新后，把新的 zip 提交进私有仓库即可。

### 发布一个版本

1. 修改 `PolarisCore.csproj` 里的 `<Version>`，提交并推送。
2. 打标签并推送：`git tag v2.0.1 && git push origin v2.0.1`。标签必须与 `<Version>` 一致，否则发布会被拦下；带 `-` 的标签（如 `v2.1.0-rc1`）会标为预发布。
3. 发布完成后，建议手写一下发布说明（`gh release edit <标签> --notes "..."`）。游戏内的更新弹窗会显示说明的前几行。

## 游戏内自动更新

游戏启动约 12 秒后，Polaris 会在后台查一次本仓库最新的 Release（默认每 12 小时最多一次）。发现新版本时，游戏内弹窗让玩家选“更新 / 跳过此版本 / 以后再说”。

- 点“更新”才会下载：先取 `PolarisCore-manual.zip.sha256`，再取 `PolarisCore-manual.zip`，核对 SHA256 一致后解到 `BepInEx/Polaris/update/staging`。
- 游戏运行时 dll 无法被覆盖，所以文件会在玩家退出游戏之后，由 `PolarisWatcher.exe` 换到位；被替换的旧文件备份在 `BepInEx/Polaris/update/backup`。下次启动会提示“已更新到 vX”。
- 只信任配置里指定的仓库，只走 HTTPS，只接受 `BepInEx/plugins/` 下的文件。玩家不确认，什么都不会下载。
- 校验值与更新包在同一个 Release 里，它能防传输损坏，但**防不了发布账号被盗后被换包**。

总开关在游戏“设置”界面的 Polaris 分区（设置项 `CheckForUpdates`，存在 `BepInEx/config/Polaris/polaris.cfg`）；检查间隔和仓库在 `BepInEx/config/Polaris/_polaris_update.cfg`：`CheckIntervalHours`、`Repository`。

## 诊断配置

阈值都在 `BepInEx/config/Polaris/_polaris_diagnostics.cfg`：

- `[Watchdog]`：卡死检测的警告与报告阈值、心跳写盘间隔 `HeartbeatSeconds`。
- `[Severity]`：各级升级条件（`EscalateKinds`、`CriticalStormSeconds`、`CriticalStormMods`、`CriticalHangSeconds`）和提示冷却时间。游戏内提示的开关、最低级别、停留时间、位置，以及严重错误时是否自动退出，在游戏“设置”界面的 Polaris 分区里改（存在 `polaris.cfg`）。
- `[CrashWindow]`：是否启动崩溃观察者。

报告写在 `BepInEx/Polaris/reports/`：每局一个文件，同类错误只记一次，最多保留 20 份。

## 旧代码

原先的整套库功能（Game、Drawing、Settings、Content 等 API）已经拆出去，不在 `main` 里。完整代码保留在 `legacy` 分支和 `pre-slim-v2.0.0` 标签；当时的设计文档放在 `doc/legacy/`。
