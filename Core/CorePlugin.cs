using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using HarmonyLib;

namespace Polaris
{
    /// <summary>Polaris 库的 BepInEx 入口：只做库自己的事（补丁、设置扫描、事件订阅扫描、资源泵），不含任何诊断或界面产品逻辑。</summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class CorePlugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private Harmony harmony;

        private void Awake()
        {
            Logger = base.Logger;

            PolarisAPI.Errors.Guard(PolarisAPI.Paths.EnsureDirectories, "creating the Polaris directory structure");
            Localization.CoreStrings.Register();
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            PolarisAPI.Patching.ApplyAll(harmony, typeof(CorePlugin).Assembly, Logger);
        }

        /// <summary>所有模组 Awake 之后：扫描设置项（读出玩家存的值）和属性标注的事件订阅。</summary>
        private void Start()
        {
            PolarisAPI.Errors.Guard(Settings.SettingsAttributeScanner.ScanAll, "registering the settings");
            PolarisAPI.Errors.Guard(Events.EventAttributeScanner.ScanAll, "subscribing attribute event handlers");
        }

        private void Update()
        {
            Res.Runtime.MainThreadDispatcher.Drain();
            Res.Runtime.PxlsPump.Advance();
        }

        private void OnApplicationQuit()
        {
            PolarisAPI.Errors.Guard(() => PolarisAPI.Events.Post(new Events.GameQuitting()), "posting GameQuitting");
        }
    }
}
