# MinimalMod 模板

一个能直接编译、能在游戏里跑的最小模组，演示 Polaris Core 最常用的几样东西：`PolarisMod` 基类、设置项（开关 / 可改键的热键 / 条件显示 / 按钮行）、热键、玩家提示。复制整个文件夹，改名，就是你的新模组。

## 用法

1. 复制 `aic_path.txt.example` 为 `aic_path.txt`，里面写游戏根目录（含 `AliceInCradle_Data` 的那一层）。
2. 游戏里要先装好 Polaris（安装器装的就行）：模板直接引用游戏里 `BepInEx/plugins/PolarisCore.dll`，不用自己构建 Core。
3. `dotnet build`。产物会自动复制到 `BepInEx/plugins/MinimalMod/`（`-p:DeployToGame=false` 可关）。
4. 启动游戏：设置页的 Polaris 标签页里能看到 "Minimal Mod" 分区；按 F8 屏幕边上弹一行提示。

## 改成你自己的

- 重命名：`MinimalMod.csproj`、`AssemblyName`、`RootNamespace`、命名空间、`[BepInPlugin]` 里的 GUID 和名字。GUID 一旦发布就别再改，设置文件名与资源目录都靠它。
- `[PolarisModInfo]` 里写作者和说明，管理器页面里点开你的模组就能看到。
- 要打补丁：直接写 `[HarmonyPatch]` 类，`PolarisMod` 会把它们逐个安全应用。游戏程序集已经 publicize，private/internal 成员能直接访问。

## 文件

| 文件 | 作用 |
| --- | --- |
| `Plugin.cs` | 入口，继承 `PolarisMod`，绑定热键 |
| `MySettings.cs` | 设置项声明（字段 = 值，Core 负责保存和画界面） |
| `Directory.Build.props` | 读取 `aic_path.txt` 得到游戏目录 |
| `MinimalMod.csproj` | 引用与自动部署 |
