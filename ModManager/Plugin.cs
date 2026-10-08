using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using HarmonyLib;

namespace Polaris
{
    [BepInDependency("Polaris.Core")]
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static Plugin Instance;

        private Harmony harmony;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            // 诊断先于一切安装：配置宿主信息、心跳、会话哨兵与看门狗，再接上 Unity/AppDomain/BepInEx 三条错误通道。
            Diagnostics.DiagnosticsHost.Install();
            Diagnostics.CoreErrorCapture.Install();

            // 把库的 Errors 接到诊断引擎；此前库报的错只进了日志。
            PolarisAPI.Errors.SetBackend(new Diagnostics.DiagnosticsBackend());

            // 目录建不出来不该把整个 Awake 掀掉，否则 Unity 不会再调 Start，子系统全部起不来。
            PolarisAPI.Errors.Guard(
                PolarisAPI.Paths.EnsureDirectories,
                "creating the Polaris directory structure");

            PolarisAPI.Errors.Guard(ReportLastSession, "reading how the previous session ended");

            // 内置文案表必须早于 Start 阶段的设置项扫描，绑定配置时要用说明文字查表。
            Localization.PolarisStrings.Register();

            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            PolarisAPI.Patching.ApplyAll(harmony, typeof(Plugin).Assembly, Logger);

            Logger.LogMessage(Logo);
        }

        /// <summary>上一局非正常结束时，把结论摊到控制台、写进本局报告、给告知页上膛；正常退出时不吭声。</summary>
        private static void ReportLastSession()
        {
            Diagnostics.LastSessionInfo last = PolarisDiagnostics.Health.LastSession;
            if (last == null)
            {
                return;
            }

            Logger.LogWarning($"[Polaris] {last.OneLine()}");
            Logger.LogWarning($"[Polaris] Stalled at: {last.Where()}");

            PolarisErrorNotice.AdoptLastSession(last);
        }

        /// <summary>注册设置与管理入口，启动更新检查。</summary>
        private void Start()
        {
            // 先扫描设置项（读出玩家存的值），再启动依赖这些值的功能。库的 Start 也会扫，扫描幂等，这里只为不依赖两个插件 Start 的先后。
            PolarisAPI.Errors.Guard(PolarisAPI.Settings.EnsureLoaded, "registering the settings");

            // 必须在其它模组注册按钮之前占住标题菜单"设置"后面的位置。
            PolarisManagementUI.RegisterButton();

            // 后台检查 GitHub 上有没有新版本；只在玩家点“更新”后才会下载。
            PolarisAPI.Errors.Guard(() => SelfUpdate.UpdateChecker.Begin(this), "starting the update check");
        }

        private void Update()
        {
            // 心跳放在第一行：后面的任何东西卡住，这一帧的心跳也要算已打过。
            Diagnostics.DiagnosticsHost.Beat(UnityEngine.Time.frameCount);

            Diagnostics.InGameAlert.Update();

        }

        private void OnGUI()
        {
            Diagnostics.InGameAlert.OnGUI();
        }

        /// <summary>窗口失焦/回到前台；失焦时 Unity 不再调 Update，须暂停看门狗以免误报卡死。</summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            Diagnostics.DiagnosticsHost.SetPaused(!hasFocus);
        }

        /// <summary>被系统挂起/恢复；对看门狗的意义与失焦相同，这段时间不推进帧属于正常。</summary>
        private void OnApplicationPause(bool isPaused)
        {
            Diagnostics.DiagnosticsHost.SetPaused(isPaused);
        }


        /// <summary>进程退出前的收尾：落一份"上一局摘要"供下次启动读取，控制台补一行汇总（无错误时不吭声）。</summary>
        private void OnApplicationQuit()
        {
            try
            {
                // 先停看门狗：退出过程还要活一会儿（存档、淡出），不停会把它误判成卡死。
                Diagnostics.DiagnosticsHost.Stop();

                string summary = Diagnostics.DiagnosticsHost.Summary();
                if (summary != null)
                {
                    Logger.LogMessage(summary);
                }

                PolarisAPI.Errors.Guard(PolarisErrorNotice.PersistPending, "saving the previous session's error summary");
            }
            finally
            {
                // 最后删掉会话哨兵，这是"正常结束"的唯一表达方式；须排在 PersistPending 之后。
                // 放在 finally：前面任何一步抛异常都不能让正常退出被 Watcher 当成异常退出。
                Diagnostics.DiagnosticsHost.CloseSession();
                Diagnostics.CoreErrorCapture.Uninstall();
            }
        }

        private const string Logo = """

                :=.                                   ..              .-:                                
                                         .            :.                                                 
                                                     .--.                                                
                                                     :++.                                        ..      
                                                    .:*+..                                      .-:      
                                                   ..:**:.                                               
                              ...                  .-=**--.                  ..                          
                              .--+=                .-=#*--.              ..-=::                          
                 ..            .:+++*:..           :-=##--.           ..-*+==.                           
                 ::              :-##+==..       ..:-=##=-.         .:==*##:.                            
                                  .-:*%%==:..    .:-++%#+=-:.     .-=+%%*::                              
                                   . -==%%*=-..  ::=+*%%++-:.  ..-=*%#-=:                                
                                     .::++#@*=-:.::=+*@%++-::..=+#@*+=::.                                
                                        --++#@#++--=**@%++=:-++#@#+=-:                                   
                                        ..:-+*#%#+=+*#@%*++++%%#*=-:..                                   
                                        ..::--+#####*#@@**#%###=::::..                                   
                                     ...---:---==%@%#%@@##%@%==-:::--:..                                 
                        .......::::-----==++++*****#%%@@%%#**++++++===-----::::...                       
                 .. :===-===+++****#####%%%%%%%%%%%@@@@@@@%%%%%%%%%%%######****+==----===. .             
                    .-------==+++++********########%@@@@@@%########********++++===-------.               
                        .......:::::::::---====++###%%@@%%##*++======----::::::.......                   
                               .........:--::-=++%@%*#@@#*%@%++=:::--:.......                            
                                        ..:-==*@%*+**#@@***+*%%*=--:..                                   
                                        ::-=*%#**=-+*#@@**=-=**#%*=-::                                   
                                       .==*%##+--::=+*@%*+-::-=*###+=-                                   
                -=:                  .:-*###+-:..::=+*%%++-:..:--*%#*+::.                 .:             
                 .                 . -++%%+-:..  ::=+*%%++-:.  .:--*@#++- .               :-.            
                                   -:*@%--:.     .:-++##++-:.     .--=%@*:-                              
                                 .-##+==...      ..:-=##=-:.       ...==+##:.                            
                                .==+*:..           :-=**--.           ..-*+==.                           
                               .:=-                .--**--.              ..-=::                          
                               ....                .--**--.                  :-.                         
                                                   .:-++-:.                                              
                                                    .:++..                                               
                                                     .+=.                                                
                                                     .--                                                 
                                                      ..                                                 
                                                 
                                                  AIC-Polaris
                                       by Alon_ · github.com/AAAA9731
                """;
    }
}
