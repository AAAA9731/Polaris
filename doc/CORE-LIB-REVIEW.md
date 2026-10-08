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
| PolarisSave | `3ab25cb73ce7d701d0056c40e341a7a93277f861` | `Format/` 七个容器编解码文件与异常类型 → `Save/`；精简原 `SaveAPI/SaveHandle/SaveRuntime/SavePartitionId`，将六个 Integration 文件合并为 `Patch_GameSave.cs` |

游戏与资源部分保留 23 个 C# 文件；旧 Res 的 46 个文件删到 20 个。Save 共 12 个文件、936 行（含注释和空行），原模块为 32 个文件、3,033 行。原自定义 JSON／Archive 序列化平台由游戏已有的 Newtonsoft.Json 库替代，仅增加普通数据类与现有容器的转换接线。没有拷入旧 Lang 和 UI 的实现。当前 Core 中没有调用者的两个 `Infra/Callbacks/` 文件也已删除。

相对当前 `main`，仓库 C# 总行数从 16,830 增至 19,153，Core 部分约为 17,642 行；增加的是已认可的少量旧库能力。此次“做减法”的比较对象是固定旧框架，不应把它描述成当前管理与诊断 Core 的总代码量下降。

审核命令：

```powershell
# 当前管理与诊断 Core 到本次功能分支的差异
git diff main..feat/core-library-slim-030i

# 单独审核存档恢复和扩展机制清理（首轮精简提交为 b9738e5）
git diff b9738e5..feat/core-library-slim-030i

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
| Save | 一次显式注册普通数据类，随原版存档保存／加载，新游戏重置；复用尾部容器、CRC 和失败写入拦截 | 自定义 Archive／JSON 解析器、迁移回调平台、文件自动发现与注册、独立宿主、恢复界面 |

Particles、Addons、Magic 均不带入。Event、AI、Map、Network 与广泛的内容 API 也不带入。这里没有替它们创建空模块或占位接口。

扩展资源文件（`.plang`、`.pui`、`import.json`、`.pai/.pnpc/.pmap` 等）的发现、注册、热重载和生成机制全部不带入，现有代码也无相应扫描入口。旧 Content 扩展注册框架说明已从当前分支删除，固定历史基线仍可查阅。声明式设置属性扫描与管理器识别模组 DLL 按要求保留；直接加载一个指定资源文件不涉及扩展内容注册。

存档挂接只恢复旧模块的四个原版方法补丁：序列化后追加容器、成功读档后加载容器、新游戏重置数据、落盘前拒绝失败字节流。它们直接由现有 Core 的补丁安装流程应用，不增加初始化宿主或独立 Save 插件。

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

存档的主要入口是 `Polaris.Save.SaveAPI.Register<T>(id, version: 1)`，在模组 `Awake` 中显式调用一次。T 是有公共无参构造函数的普通数据类，无须实现接口或逐字段写读写方法：

```csharp
public sealed class WorldData
{
    public int Counter;
    public System.Collections.Generic.List<string> Unlocked = new();
}

private Polaris.Save.SaveHandle<WorldData> world;

private void Awake()
{
    world = Polaris.Save.SaveAPI.Register<WorldData>("your.mod.guid/world");
}

// 修改本局数据；原版手动存档和自动存档都会保存它，读档时自动还原。
// world.Current.Counter++;
```

公共字段和可序列化属性使用游戏已有的 Newtonsoft.Json 保存为 UTF-8 JSON，集合按存档内容替换，不与构造函数的默认集合叠加；存档缺少的字段保留构造默认值。JSON 配置不采用其它模组设置的 `JsonConvert.DefaultSettings`。按该库的规则可用 `[JsonIgnore]` 排除运行期字段；数据对象应只装普通数据，不要放 Unity／游戏对象或委托。

新游戏与每次读档都会替换 `Current` 实例，模组持有句柄，每次从 `Current` 取当前实例。没有模组数据的老存档使用默认值；`WasLoaded` 表示是否实际读到了该分区。注册在第一次新游戏、读档或保存时冻结，因此不要等到进地图后才注册。所有入口和数据修改在游戏主线程使用。

原版序列化完成后把容器追加在 `.aicsave` 尾部，成功读档后解析同一容器；原版存档前缀保持原样，格式和 CRC 算法不变。未安装模组的分区原样保留。容器损坏、数据不可读、分区版本过新或不支持的标志会进入只读恢复状态；序列化失败会标记具体失败字节流，反复重试同一字节流也不会放行到原版删除／替换文件的落盘方法。重新成功序列化才清掉该字节流的失败标记；换读完好档或成功初始化新游戏可退出只读恢复状态。没有增加丢弃存档数据的接口或恢复界面。

`Game.Save.RequestAutosave` 请求原版自动存档，也会经过上述挂接。底层 `SaveContainerWriter/Reader` 仍可直接处理字节容器。旧 `IPolarisSaveData/SaveArchive` 及独立 Save DLL 不保持二进制兼容，旧模组需改成普通数据类并重新编译；旧 payload 的字段键和类型只有与新数据类相符时才能直接读取，没有猜测字段映射的兼容层。

## ver030i 验证

构建直接使用本机 ver030i 的 Managed 程序集，dnSpy／dnlib 只读检查原版程序集与 IL，没有改写游戏文件。

已完成：

- Release 构建 Core、Watcher 与单文件 Installer，零警告、零错误。
- 检查 Core 对 `Assembly-CSharp`、`unsafeAssem`、`better`、`pixelliner` 的全部 227 处成员引用，均能解析；另外检查 Unity、`System.HashCode` 与 NVorbis 的 141 处引用，以及新增的 13 处游戏自带 JSON 库引用，全部通过。
- 检查现有标题与设置界面的 14 个补丁目标方法；标题菜单转译器的五段 IL 模式按顺序命中，`UiCFG` 八参数构造函数中“7 页 Designer 数组”的目标模式恰好一处。
- 核对 30i 的 `MTRX.loaded/prepared`、PXLS 异步解析／图像关联／释放和 `COOK.autoSave` 签名；额外核对恢复的四个存档补丁目标的完整签名、Harmony 参数绑定和原版读写行为。
- 存档容器 11 项检查通过：往返、元数据、空 payload、原版前缀与有效数组长度、空容器、无容器、分区损坏定位、损坏头部、未知版本、非法长度、重复 ID／payload 上限，以及固定旧 writer 的逐字节对照（部分检查在同一项内）。
- 临时回归程序直接编译当前 Save 源码，使用游戏自带 Newtonsoft.Json 和内存字节流，48 项检查通过：一次注册、保存／加载、新游戏重置、集合替换、缺失字段默认值、未知模组分区保留、损坏数据／更高版本拒绝覆盖、同一失败字节流反复重试拦截、重序列化清除失败标记、默认构造失败保护、非法／晚注册和容量上限。仅游戏、Harmony 与诊断表面用测试替身替代，没有写真实存档或添加生产测试依赖。
- 临时假游戏目录的静默安装、重复安装、卸载 Polaris、连同 BepInEx 卸载全部通过；验证全部五个 OGG 依赖 DLL 的安装与删除、原文件备份恢复和其它模组文件保留。

未在真实游戏中运行新分支，因而 Unity 实际渲染、PXLS 实际素材加载、声音播放、界面交互及原版自动存档仍需游戏内验证。静态匹配和构建成功不能代替这些运行检查。本次没有安装到真实游戏目录，也没有读写玩家存档；临时测试安装器与默认分支原发布包分开保存。
