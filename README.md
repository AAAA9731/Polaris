# Polaris

《Alice in Cradle》的 **模组管理器 + 诊断工具**，由三部分组成：

| 部分 | 位置 | 作用 |
|---|---|---|
| **PolarisCore**（BepInEx 插件） | 仓库根目录 | 游戏内：捕获并追踪错误、阶梯式提示、标题画面的模组管理页 |
| **PolarisWatcher**（独立小程序） | `Watcher/` | 游戏异常退出后弹窗，说明是什么原因、哪个模组 |
| **PolarisInstaller**（图形安装器） | `Installer/` | 双击安装/更新/卸载 BepInEx + Polaris |

库功能（Game / Drawing / Settings / Content 等 API）已不在 main 中，完整代码保留在 `legacy` 分支与 `pre-slim-v2.0.0` 标签。

## 玩家：怎么用

运行 `PolarisInstaller.exe`（单个文件，无需另装 .NET）→ 点“浏览…”选择游戏的 `AliceInCradle.exe`（或把它/游戏文件夹拖进窗口；把安装器放进游戏目录运行则自动识别）→ 点“安装”。
模组的启用/禁用在游戏内标题画面的 Polaris 页里操作。

## 诊断：阶梯式处置

| 级别 | 触发 | 玩家看到 |
|---|---|---|
| 0 | 原版自身的错误 | 无，只计数 |
| 1 轻微 | 模组相关错误首次出现 | 右上角小提示，点开看详情 |
| 2 持续 | 同类错误每帧反复、或同一模组触发多种错误 | 提示框，可一键禁用该模组（重启生效） |
| 3 严重 | 风暴持续过久 / 多个模组同时风暴 / 卡死过久 / Polaris 核心补丁失效 / 内存耗尽 | 全屏提示后主动退出，Watcher 窗口点名责任方 |

所有阈值在 `BepInEx/config/Polaris/_polaris_diagnostics.cfg`；报告写在 `BepInEx/Polaris/reports/`（每局一个文件，最多保留 20 份）。

## 开发者：构建

1. 在仓库**上一级目录**创建 `aic_path.txt`，单行写游戏根目录（含 `AliceInCradle_Data` 的那一层）。
2. 构建插件：`dotnet build PolarisCore.csproj`
3. 构建崩溃观察者：`dotnet build Watcher/PolarisWatcher.csproj`
4. 发布安装器：`Installer/publish.ps1`（会先构建上面两项并嵌入，产物为 `Installer/publish/PolarisInstaller.exe`）

安装器支持无界面模式：`PolarisInstaller.exe --silent install|uninstall --game "<路径>" [--remove-bepinex]`，结果写入 `polaris-installer.log`。

## 目录

```
Diagnostics/     诊断：捕获、归因、看门狗、会话哨兵、严重度策略、游戏内提示
Contracts/       诊断的公开数据类型
Infra/           错误/健康/路径等基础设施
Patch/           标题画面补丁（模组管理页入口、告知页）
Localization/    内置三语文案
Watcher/         PolarisWatcher.exe（崩溃窗口）
Installer/       PolarisInstaller（WPF 单文件安装器，Payload/ 为随包的 BepInEx 文件）
doc/legacy/      旧库的规格与设计文档
```

第三方组件与许可见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。
