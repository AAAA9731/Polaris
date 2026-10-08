using BepInEx;
using Polaris;

namespace MinimalMod
{
    // 在管理器页面里显示的作者和说明；Version 缺省取下面 BepInPlugin 的版本。
    [PolarisModInfo("你的名字", "一句话说明这个模组做什么。")]
    // 先 BepInDependency 后 BepInPlugin 都行；它保证 Polaris Core 在你之前加载。
    [BepInDependency("Polaris.Core")]
    // GUID 全局唯一，建议反向域名写法；设置文件名、资源目录都以它为基础。
    [BepInPlugin("com.example.minimalmod", "Minimal Mod", "0.1.0")]
    public class Plugin : PolarisMod
    {
        // 继承 PolarisMod 之后：
        //  - 本程序集里所有 [HarmonyPatch] 类会被逐个安全应用（坏一个只报错跳过，不拖垮整个模组）；
        //  - OnLoad / OnReady / OnUnload 抛出的异常会被接住并上报，不会让插件加载失败；
        //  - [PolarisSettingGroup] 类和 [PolarisSubscribe] 方法由 Core 自动扫描，不用手动注册。

        /// <summary>插件加载（Awake）时：做自己的初始化，注册按钮、热键、事件之类。</summary>
        protected override void OnLoad()
        {
            // 热键：键从设置里取，玩家在设置页改了立刻生效；在输入框里打字时不会触发。
            PolarisAPI.Game.Input.Bind(() => MySettings.Hotkey, SayHello);
        }

        /// <summary>所有模组都加载完之后（Start）：设置值已经读入，可以读取玩家保存的选项了。</summary>
        protected override void OnReady()
        {
            Logger.LogInfo($"Minimal Mod ready; greeting enabled = {MySettings.Greeting}");
        }

        static void SayHello()
        {
            // 以 & 开头的文字是本地化键；没有对应文案时原样显示键名，所以先写字面量也行。
            if (MySettings.Greeting)
            {
                PolarisAPI.Game.UI.Notify("Hello from Minimal Mod!");
            }
        }
    }
}
