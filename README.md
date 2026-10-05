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

## CI / 发布（GitHub Actions）

| 工作流 | 触发 | 做什么 |
|---|---|---|
| `ci.yml` | 推送到 main / refactor/ feat/ fix/ 分支、所有 PR | 构建 PolarisWatcher；有游戏程序集时再构建插件、发布安装器并跑安装器冒烟测试，产物上传为 artifact |
| `release.yml` | 推送 `v*` 标签 | 同上（必须有游戏程序集），核对标签与 `<Version>` 一致后创建 GitHub Release，附 `PolarisInstaller.exe`、`.sha256`、`PolarisCore-manual.zip` |

编译插件需要游戏的程序集（版权文件，不能进仓库），所以要在仓库里配一个密钥：

1. 运行 `tools/pack-managed.ps1 -GameDir "<游戏目录>"` 得到 `aic-managed.zip`（约 4 MB，只含 6 个 dll）。
2. 把它放在**你自己控制的私有位置**（私有仓库的 Release 资源、对象存储的带签名链接等，不要公开）。
3. 在仓库 Settings → Secrets and variables → Actions 新建：`AIC_MANAGED_URL`（下载地址）；地址需要鉴权时再加 `AIC_MANAGED_AUTH`（例如 `Bearer <token>`）。

当前托管在维护者个人名下的**私有**仓库 `AAAA9731/aic-managed-refs`（Release `ver030i` 的 `aic-managed.zip`，文件原样复制、未做任何改动）。`AIC_MANAGED_AUTH` 用一个只读该仓库内容的 fine-grained token（`Bearer github_pat_…`）。游戏更新后上传新版本：
`gh release create <新版本号> aic-managed.zip --repo AAAA9731/aic-managed-refs`，并把 `AIC_MANAGED_URL` 改成新资源地址。

没配密钥（例如来自 fork 的 PR）时，CI 只构建不依赖游戏的 PolarisWatcher，插件与安装器步骤会被跳过并给出提示。游戏更新后重新打一份 zip 覆盖即可。

发布一个版本：改 `PolarisCore.csproj` 的 `<Version>` → 提交 → `git tag v2.0.1 && git push origin v2.0.1`。

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
