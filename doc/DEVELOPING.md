# 开发者文档

这篇文档讲的是怎么构建 Polaris、CI 怎么跑、怎么发布版本，以及仓库的目录结构。玩家向的说明在 [README](../README.md)。

## 项目组成

| 部分 | 位置 | 作用 |
|---|---|---|
| PolarisCore（BepInEx 插件 `PolarisCore.dll`，GUID `Polaris.Core`） | `Core/` | **核心库**：给其它模组用的 API——设置项（含设置页搜索框）、存档分区、事件总线、本地化、主菜单按钮、资源加载（贴图/音频/PXLS）、基础错误上报。不含任何界面产品或诊断逻辑 |
| ModManager（BepInEx 插件 `ModManager.dll`，GUID `Polaris.ModManager`） | `ModManager/` | **模组管理器**：错误捕获与归因、阶梯式提示、标题画面的模组管理页、自动更新。依赖核心库（`[BepInDependency]`），并把自己接成核心库 `Errors` 的后端 |
| PolarisWatcher（独立小程序，.NET Framework 4.8） | `Watcher/` | 游戏退出后：弹出崩溃原因窗口、应用已下载的更新 |
| PolarisInstaller（WPF 单文件安装器） | `Installer/` | 安装、更新、卸载 BepInEx 与 Polaris；嵌入了 BepInEx 文件和刚构建好的插件与 Watcher |

两个插件 dll 都放在 `BepInEx/plugins/` 根目录；其余运行依赖和 Watcher 放在 `plugins/Polaris/`。依赖方向只有一个：ModManager → Core。核心库不引用管理器；需要管理器才有的能力（归因、报告、卡死检测）时，通过 `IErrorBackend` 这类接口由管理器注入。管理器页面会列出核心库，但它是唯一没有启停按钮的一项（禁用它等于禁用管理器自己）。

从 2.0.x 升级：旧版的 `PolarisCore.dll` 是管理器，新版同名文件变成了核心库，管理器改叫 `ModManager.dll`。旧版自动更新下载的包会直接覆盖旧文件并带上 `ModManager.dll`，不会出现同名插件冲突。发布资源名仍叫 `PolarisCore-manual.zip`，旧版更新器靠它找包。

## 目录结构

```
Core/            核心库：Api（精简的游戏查询）、Events、Save、Settings、Resources、Localization、Infra、Patch（设置界面/主菜单）
ModManager/      模组管理器：Diagnostics、Contracts、SelfUpdate、Localization（管理器文案）、Patch（标题画面）、模组管理页等 UI
Watcher/         PolarisWatcher.exe
Installer/       PolarisInstaller（Payload/ 里是随包分发的 BepInEx 文件）
tools/           pack-managed.ps1（打包游戏程序集）、ci-smoke.ps1（安装器冒烟测试）
doc/legacy/      旧库的规格与设计文档
.github/         GitHub Actions 工作流
```

## 本地构建

1. 在仓库**上一级目录**创建 `aic_path.txt`，单行写游戏根目录（含 `AliceInCradle_Data` 的那一层）。
2. 构建插件：`dotnet build ModManager/ModManager.csproj`（会连带构建 `Core/PolarisCore.csproj`）
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

1. 修改 `ModManager/ModManager.csproj` 里的 `<Version>`，提交并推送。
2. 打标签并推送：`git tag v2.0.1 && git push origin v2.0.1`。标签必须与 `<Version>` 一致，否则发布会被拦下；带 `-` 的标签（如 `v2.1.0-rc1`）会标为预发布。
3. 发布完成后，建议手写一下发布说明（`gh release edit <标签> --notes "..."`）。游戏内的更新弹窗会显示说明的前几行。

## 游戏内自动更新

游戏启动约 12 秒后，Polaris 会在后台查一次本仓库最新的 Release（默认每 12 小时最多一次）。发现新版本时，游戏内弹窗让玩家选“更新 / 跳过此版本 / 以后再说”。

- 点“更新”才会下载：先取 `PolarisCore-manual.zip.sha256`，再取 `PolarisCore-manual.zip`，核对 SHA256 一致后解到 `BepInEx/Polaris/update/staging`。
- 游戏运行时 dll 无法被覆盖，所以文件会在玩家退出游戏之后，由 `PolarisWatcher.exe` 换到位；被替换的旧文件备份在 `BepInEx/Polaris/update/backup`。下次启动会提示“已更新到 vX”。
- 只信任配置里指定的仓库，只走 HTTPS，只接受 `BepInEx/plugins/` 下的文件。玩家不确认，什么都不会下载。
- 校验值与更新包在同一个 Release 里，它能防传输损坏，但**防不了发布账号被盗后被换包**。

总开关在游戏“设置”界面的 Polaris 标签页（设置项 `CheckForUpdates`，存在 `BepInEx/config/Polaris/polaris.cfg`）；检查间隔和仓库在 `BepInEx/config/Polaris/_polaris_update.cfg`：`CheckIntervalHours`、`Repository`。

## 诊断配置

阈值都在 `BepInEx/config/Polaris/_polaris_diagnostics.cfg`：

- `[Watchdog]`：卡死检测的警告与报告阈值、心跳写盘间隔 `HeartbeatSeconds`。
- `[Severity]`：各级升级条件（`EscalateKinds`、`CriticalStormSeconds`、`CriticalStormMods`、`CriticalHangSeconds`）和提示冷却时间。游戏内提示的开关、最低级别、停留时间、位置，以及严重错误时是否自动退出，在游戏“设置”界面的 Polaris 标签页里改（存在 `polaris.cfg`）。
- `[CrashWindow]`：是否启动崩溃观察者。

报告写在 `BepInEx/Polaris/reports/`：每局一个文件，同类错误只记一次，最多保留 20 份。

## 设置界面的 Polaris 标签页

在 ver030i 中，现有补丁将 `UiCFG` 构造函数的标签页数量从 7 改为 8，以 `CATEG._MAX` 建出第八页，再填入 Polaris 和模组的设置项。标签图标是 ✴，页眉标题是“Polaris 设置”。实现见 `Core/Patch/Patch_UiCFG_PolarisTab.cs`；本次合并沿用这套实现。构造函数的 IL 匹配未成功时，设置项回退到“常规”页尾部。

## 给模组作者

- 想基于 Core 写模组：复制 [`templates/MinimalMod/`](../templates/MinimalMod/)，它能直接编译、能在游戏里跑，README 里有步骤。
- Core 收什么、不收什么、API 稳定性怎么约定：见 [`Core/CHARTER.md`](../Core/CHARTER.md)。新增 API 前先过一遍里面的准入规则。

## 旧代码

`main` 保持原管理与诊断版本，本次合并在 `feat/core-library-slim-030i` 中审核。当前 Core 基线和 GitHub 聚合框架基线都有独立本地分支，来源与具体差异见 [CORE-LIB-REVIEW.md](CORE-LIB-REVIEW.md)。完整旧 Core 保留在 `legacy` 分支和 `pre-slim-v2.0.0` 标签；当时的设计文档放在 `doc/legacy/`。
