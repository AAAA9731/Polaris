# Core 精简合并审核

本次以 GitHub 固定旧框架为来源，将常用部分删减后直接编进当前 `PolarisCore.dll`。管理器、诊断、更新器和当前设置界面继续使用原实现；没有恢复独立 Polaris 子库、添加兼容旧接口的转发层，或另造一套框架。

## 基线与分支

| 用途 | 本地分支 | 固定提交 |
|---|---|---|
| 修改前的当前 Core，也是默认分支 `main` | `codex/baseline-before-library-slim-20261005` | `49639aa1cdada538f14dc722f7582d63fe4fe139` |
| GitHub 聚合框架的完整源码快照 | `codex/baseline-full-framework-20261005` | `8547e1fe7235eab3b1bdec2cb066c104fc907314` |
| 本次可审核修改 | `feat/core-library-slim-030i` | 基于上述当前 Core，不基于完整框架分支 |

仓库实际默认分支叫 `main`，没有 `master`。`main` 保留原 v2.0.4 源码状态；功能分支没有合并或推送到默认分支，也没有发布新版本。

完整源码快照来自 [New-Cradle-of-Stella/Polaris](https://github.com/New-Cradle-of-Stella/Polaris/tree/822cc53289396d5b98d336c32165bf12785f5291) 的聚合提交 `822cc53289396d5b98d336c32165bf12785f5291`。各子模块按该提交指向的版本下载并展开成普通目录，源码未修改，因此审核不依赖子模块能否继续下载。完整的 13 个子模块 SHA 记录在快照分支的 `doc/BASELINE-SNAPSHOT.md`。

本次实际提取的来源：

| 来源 | 固定 SHA | 抽取位置 → 当前位置 |
|---|---|---|
| PolarisCore | `4ddc4d0fcfe7470e6096754c2d6584686e9f9222` | `Api/GameBinding.cs`、`Api/Game/PolarisGameAPI.cs`、`PolarisGamePersistenceAPI.cs` 与 `GameEnums.cs` 的少量成员 → `Api/` 三个游戏 API 文件 |
| PolarisRes | `db0c08e6e9fe8fce875249792330793780b0e9e5` | 原 `Api/Core/Import/Loaders/Pxls/Runtime` 的保留部分 → `Resources/`；`Mounts/PathSandbox.cs` 只留下路径校验，移至 `Resources/Core/` |
| PolarisSave | `3ab25cb73ce7d701d0056c40e341a7a93277f861` | `Format/` 七个容器编解码文件与 `Api/PolarisSaveException.cs` → `Save/` |

从旧框架抽入共 31 个 C# 文件，约 2,000 行（含注释和空行），这是本次保留的旧库代码量，不是整个 Core 的总规模。旧 Res 的 46 个文件删到 20 个；旧 Save 的 32 个文件删到 8 个。没有拷入旧 Lang 和 UI 的实现。当前 Core 中没有调用者的两个 `Infra/Callbacks/` 文件也已删除。

相对当前 `main`，仓库 C# 总行数从 16,830 增至 18,771，Core 部分约为 17,260 行；增加的是已认可的少量旧库能力。此次“做减法”的比较对象是固定旧框架，不应把它描述成当前管理与诊断 Core 的总代码量下降。

审核命令：

```powershell
# 当前管理与诊断 Core 到本次功能分支的差异
git diff main..feat/core-library-slim-030i

# 查看固定旧框架的目录和全部来源 SHA
git ls-tree codex/baseline-full-framework-20261005
git show codex/baseline-full-framework-20261005:doc/BASELINE-SNAPSHOT.md

# 直接查看某个保留文件的旧实现
git show codex/baseline-full-framework-20261005:PolarisRes/Api/ModResources.cs
git show codex/baseline-full-framework-20261005:PolarisSave/Format/SaveContainerWriter.cs
```

## 保留和删除

| 分类 | 合并进 Core 的内容 | 删除或不带入的内容 |
|---|---|---|
| Game | 帧数／焦点／鼠标查询、清按键、素材加载阶段、语言查询与切换、原版地图／玩家引用、原版自动存档请求 | 全套游戏对象包装器、领域系统、回调总线、内容注册平台 |
| Res | 固定目录读取 byte[]、PNG/JPG、MImage、WAV/OGG、PXLS；保留原引用计数、释放和 PXLS 跨帧完成逻辑 | 挂载优先级、扩展名探测、自动字段绑定、`import.json`、热重载、视频、自定义加载器、独立宿主；PXLS 全局帧名注册与原版帧替换也删除 |
| Lang | 复用当前 `PolarisAPI.Localization.Register/Text`、`LocalizedText` 与原版 `TX.Get` 接入 | 独立 Lang 插件、`.plang` 管线、XML 与代码生成 |
| UI | 复用当前 `PolarisAPI.MainMenu`、设置声明与展示，以及已有图片辅助 | 独立 UI 插件、PUI 模板、状态图、生成器与配套运行时 |
| Save | 原尾部容器格式的 byte[] 编码、解码、分区标记与 CRC 校验 | 强类型序列化、Schema/Handle 注册平台、自动游戏存档补丁、恢复界面 |

Particles、Addons、Magic 均不带入。Event、AI、Map、Network 与广泛的内容 API 也不带入。这里没有替它们创建空模块或占位接口。

生产代码改动限于旧代码的删减、文件和命名空间合并、公开保留的参数／容器类型、指定资源根目录，以及在现有 Core `Update` 中推进释放队列和 PXLS 加载。没有增加新的初始化宿主或 Harmony 补丁。

## API 使用边界

模组只引用当前 `PolarisCore.dll`；需要原版地图、玩家、Unity 类型时，同时使用相应游戏／Unity 编译引用。Res 保留 `Polaris.Res` 命名空间，Save 的保留类型合并在 `Polaris.Save`；命名空间不是独立 DLL。

游戏入口例如 `PolarisAPI.Game.World.CurrentMap`、`CurrentPlayer`、`Assets.LoadStage`、`Localization.CurrentLocale`、`Save.CanAutosave` 和 `Save.RequestAutosave(...)`。地图与玩家返回原版 `m2d.Map2d`／`nel.PR`，无额外对象封装；未建好时返回 `null`，调用方不要跨切图缓存这些引用。

资源入口：

```csharp
var resources = Polaris.Res.ResAPI.For("your.mod.guid", resourceDirectory);
var textureLease = resources.Texture("images/icon.png");
var texture = textureLease.Value;
// 保留租约直到最后一个使用者结束，再在 Unity 主线程调用：
textureLease.Dispose();
```

每个 modId 首次调用确定一个固定根目录，后续返回同一个句柄。加载、访问 Unity 对象、订阅 PXLS 完成通知及显式释放都应在 Unity 主线程进行；资源缓存键是 modId／种类／路径，重复加载共用资源，纹理参数以该资源首次加载为准。路径写明扩展名，路径校验不允许通过 `..` 逃出资源根目录。引用计数归零才释放实际资源；没有“挂载后自动注入”的生命周期。

`Pxls(...)` 要在 `PolarisAPI.Game.Assets.LoadStage == 7` 后调用。租约立即返回加载句柄，角色与图像在 `IsReady` 前为 `null`，解析完成后可通过 `Ready/Faulted` 得到结果。保留原有外置 PNG 解析和角色图像关联，这是 PXLS 正确加载所需的步骤；不再写入全局帧名表。取姿势与帧走句柄的原版 `Character`／`GetPose`。解析完成前释放会等待解析结束后清理，期间不要立即重新加载同一个 PXLS。

OGG 仍使用旧 Res 的 NVorbis 解码器及其四个 .NET 运行依赖。安装器和手动包一并携带依赖与许可文本；这些是保留 OGG 支持的成本，没有新增 Polaris 子库。没有 MP3 支持。

文案注册和查询直接使用现有入口：

```csharp
PolarisAPI.Localization.Register("your.mod.title",
    new Polaris.Localization.LocalizedText("Title") { ["zh-cn"] = "标题" });
string title = PolarisAPI.Localization.Text("&your.mod.title");
```

Save 入口是 `SaveContainerWriter.Write(IList<SavePartitionRecord>)` 和 `SaveContainerReader.Read(byte[], effectiveLength)`。格式和 CRC 算法沿用旧代码；只把原内部容器类型公开并合并命名空间。payload 是调用方提供的原始字节，不解释其中的数据结构。

**该接口不会自动保存文件、追加到游戏存档或接管原版读档。** 是否接入游戏存档、如何保存由调用方决定。读取需检查 `Status` 和每个分区的 `PayloadDamaged`。`Game.Save.RequestAutosave` 只请求原版自动存档，与此容器编码接口没有自动关联。旧的强类型 Save API 和独立库 DLL 不保持二进制兼容，旧模组需按保留入口重新编译；没有为旧签名添加转发兼容层。

## ver030i 验证

构建直接使用本机 ver030i 的 Managed 程序集，dnSpy／dnlib 只读检查原版程序集与 IL，没有改写游戏文件。

已完成：

- Release 构建 Core、Watcher 与单文件 Installer，零警告、零错误。
- 检查 Core 对 `Assembly-CSharp`、`unsafeAssem`、`better`、`pixelliner` 的全部 223 处成员引用，均能解析；另外检查 Unity、`System.HashCode` 与 NVorbis 的 141 处引用，全部通过。
- 检查现有标题与设置界面的 14 个补丁目标方法；标题菜单转译器的五段 IL 模式按顺序命中，`UiCFG` 八参数构造函数中“7 页 Designer 数组”的目标模式恰好一处。
- 核对 30i 的 `MTRX.loaded/prepared`、PXLS 异步解析／图像关联／释放和 `COOK.autoSave` 签名；无需恢复旧版对象封装或新增兼容补丁。
- 存档容器 11 项检查通过：往返、元数据、空 payload、原版前缀与有效数组长度、空容器、无容器、分区损坏定位、损坏头部、未知版本、非法长度、重复 ID／payload 上限，以及固定旧 writer 的逐字节对照（部分检查在同一项内）。
- 临时假游戏目录的静默安装、重复安装、卸载 Polaris、连同 BepInEx 卸载全部通过；验证全部五个 OGG 依赖 DLL 的安装与删除、原文件备份恢复和其它模组文件保留。

未在真实游戏中运行新分支，因而 Unity 实际渲染、PXLS 实际素材加载、声音播放、界面交互及原版自动存档仍需游戏内验证。静态匹配和构建成功不能代替这些运行检查。本次没有安装到真实游戏目录，也没有读写玩家存档；临时测试安装器与默认分支原发布包分开保存。
