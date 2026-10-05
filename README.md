# Polaris

《Alice in Cradle》的模组管理与诊断工具。

装好之后，它会替你盯着游戏：哪个模组出了错、错到什么程度，在游戏里就能直接看到；游戏崩了，会弹窗告诉你是为什么；有新版本，会提醒你并帮你更新。

[English](README.en.md)

## 它能做什么

**一键安装。** 双击安装器，选好游戏，点一下“安装”，BepInEx 和 Polaris 就一起装好了，不用手动复制文件。

**模组管理。** 在游戏标题画面的“Polaris”页里勾选启用或禁用模组，重启游戏后生效。

**阶梯式错误诊断。** 小错误在后台悄悄记录，严重到影响游戏时才会打断你：

| 级别 | 什么时候 | 你会看到 |
|---|---|---|
| 轻微 | 某个模组第一次出错 | 右上角一条小提示，点开能看详情 |
| 持续 | 同一个错误反复出现，或同一个模组出了很多种错误 | 一个提示框，可以一键禁用这个模组（重启生效） |
| 严重 | 错误持续太久、好几个模组同时出错、游戏卡死太久、Polaris 自身的补丁失效、内存耗尽 | 全屏提示，倒计时后自动退出游戏 |

原版游戏自己的错误不会打扰你。出过持续或严重问题的模组，会在模组管理页里被标记出来；如果之后这个模组的文件更新了，标记会变成一次“可能已修复”的提示。

**崩溃窗口。** 游戏意外退出时，会弹出一个窗口，写明可能的原因：退出码、系统记录的故障模块、崩溃前的错误，以及最可疑的模组。

**自动更新。** 游戏启动后，Polaris 会检查有没有新版本。有的话会在游戏里问你，点“更新”才会下载；文件在你退出游戏后自动替换，不会打断当前这局。

## 安装

需要 Windows 和一份《Alice in Cradle》。

1. 到 [Releases](https://github.com/AAAA9731/PolarisCore/releases) 下载 `PolarisInstaller.exe`（单个文件，不需要另装 .NET）。
2. 运行它，点“浏览…”，选中游戏的 `AliceInCradle.exe`。也可以把 `AliceInCradle.exe` 或游戏文件夹直接拖进窗口；如果把安装器放进游戏文件夹里运行，它会自动认出来。
3. 点“安装”。

安装器会先备份被替换的原文件，已经装好的 BepInEx 默认不会动。

## 更新

一般不用操作：游戏里收到提示后点“更新”即可。也可以随时重新运行最新版的安装器。

## 卸载

运行安装器，选好游戏目录，点“卸载”。只会删除 Polaris 自己的文件，并还原被备份的原文件；你的其他模组不会被碰。如果确定不再需要 BepInEx，可以勾选“同时卸载 BepInEx”。

## 常见问题

**Windows 提示“已保护你的电脑”？** 安装器没有做代码签名，Windows 可能会弹出 SmartScreen 提示。可以点“更多信息”再选“仍要运行”。校验值在每个 Release 里都有（`PolarisInstaller.exe.sha256`），可以自行核对。

**报告和日志在哪？** 错误报告在游戏目录的 `BepInEx/Polaris/reports/`，每局游戏一个文件，最多保留最近 20 份；BepInEx 日志在 `BepInEx/LogOutput.log`。

**怎么关掉更新检查？** 编辑 `BepInEx/config/Polaris/_polaris_update.cfg`，把 `CheckForUpdates` 改成 `false`。

**游戏更新之后 Polaris 不能用了？** Polaris 是针对特定游戏版本编译的（目前是 ver030i）。游戏大版本更新后，可能需要等 Polaris 出新版本；如果它的核心补丁失效，会直接提示并退出，不会带着错误继续运行。

**不想要某个提示框了？** 提示的阈值都可以在 `BepInEx/config/Polaris/_polaris_diagnostics.cfg` 里调整，也可以把 `[Severity]` 里的 `ToastEnabled` 设为 `false` 关掉游戏内弹窗（错误报告仍然会写）。

## 面向开发者

构建、CI、发布流程和目录结构见 [doc/DEVELOPING.md](doc/DEVELOPING.md)。

Polaris 之前提供过一整套游戏 API 库，现在已经拆出去另做；完整的旧代码保留在 `legacy` 分支和 `pre-slim-v2.0.0` 标签里。

## 许可

LGPL-2.1，见 [LICENSE.txt](LICENSE.txt)。第三方组件见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。
