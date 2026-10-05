using System;
using System.Diagnostics;
using System.IO;

namespace Polaris.Diagnostics
{
    /// <summary>
    /// 游戏启动时拉起独立进程 <c>PolarisWatcher.exe</c>：游戏崩溃后本进程已死，没法自己弹窗，
    /// 只能由外部进程等它退出，再读退出码、系统事件日志、Player.log 和哨兵文件，弹窗告诉玩家原因。
    /// 文件缺失、非 Windows 或被配置关掉时静默跳过，不影响游戏。
    /// </summary>
    internal static class CrashWatcherLauncher
    {
        const string ExeName = "PolarisWatcher.exe";

        internal static void Launch()
        {
            if (!DiagnosticsConfig.CrashWindow || Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return;
            }

            string exe = Path.Combine(DiagnosticsRuntime.PolarisRoot, ExeName);
            if (!File.Exists(exe))
            {
                DiagnosticsRuntime.Logger.LogInfo($"[Polaris] {ExeName} not found in {DiagnosticsRuntime.PolarisRoot}; no crash window will be shown.");
                return;
            }

            try
            {
                string playerLog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AppData", "LocalLow", UnityEngine.Application.companyName, UnityEngine.Application.productName, "Player.log");

                string args =
                    $"--pid {Process.GetCurrentProcess().Id}"
                    + $" --state {Quote(DiagnosticsRuntime.StateDir)}"
                    + $" --plugins {Quote(DiagnosticsRuntime.PluginsRoot)}"
                    + $" --player-log {Quote(playerLog)}"
                    + $" --bep-log {Quote(Path.Combine(BepInEx.Paths.BepInExRootPath, "LogOutput.log"))}";

                Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true });
            }
            catch (Exception e)
            {
                DiagnosticsRuntime.Logger.LogWarning($"[Polaris] Failed to start {ExeName}: {e.Message}");
            }
        }

        static string Quote(string value) => "\"" + value.TrimEnd('\\') + "\"";
    }
}
