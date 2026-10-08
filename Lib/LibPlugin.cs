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
    public class LibPlugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private Harmony harmony;

        private void Awake()
        {
            Logger = base.Logger;

            PolarisAPI.Errors.Guard(PolarisAPI.Paths.EnsureDirectories, "creating the Polaris directory structure");
            Localization.LibStrings.Register();
            PatchAllIndividually();
        }

        /// <summary>逐个类应用 Harmony 补丁而非一把 <c>PatchAll()</c>：一个补丁坏了只报错跳过，其余照常。</summary>
        private void PatchAllIndividually()
        {
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            int applied = 0;

            Assembly assembly = typeof(LibPlugin).Assembly;
            foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
            {
                try
                {
                    using (PolarisAPI.Errors.Activity($"applying patch {type.Name}", assembly))
                    {
                        // 没标 [HarmonyPatch] 的类型，Patch() 是空操作。
                        if (harmony.CreateClassProcessor(type).Patch() != null)
                        {
                            applied++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    PolarisAPI.Errors.Report(ex, $"applying patch {type.Name}", assembly);
                    Logger.LogError($"[Polaris] The feature owned by patch {type.Name} is unavailable this session.");
                }
            }

            Logger.LogMessage($"[Polaris.Lib] Applied {applied} Harmony patches.");
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
