using System;
using System.Collections.Generic;
using System.Reflection;

namespace Polaris.Diagnostics
{
    /// <summary>诊断子系统的统一入口：直接转发到 <c>Diagnostics/</c> 下的具体实现，没有可替换的后端。须在 <see cref="Install"/> 之后使用。</summary>
    internal static class DiagnosticsHost
    {
        /// <summary>由 <c>Plugin.Awake</c> 在最开始调一次：配置宿主信息，安装主线程心跳、会话哨兵与看门狗。</summary>
        internal static void Install()
        {
            DiagnosticsRuntime.Configure(
                Plugin.Logger,
                typeof(Plugin).Assembly,
                MyPluginInfo.PLUGIN_GUID,
                MyPluginInfo.PLUGIN_NAME,
                MyPluginInfo.PLUGIN_VERSION,
                PolarisMeta.ReportTarget,
                () => XX.TX.getCurrentFamilyName(),
                () => UserModToggleManager.Scan()
                    .FindAll(record => !record.Enabled)
                    .ConvertAll(record => record.DisplayName));

            MainThreadBeat.Install();
            DiagnosticsConfig.Resolve();
            ErrorReportWriter.PrimeEnvironment();
            PolarisAPI.Errors.Guard(SessionSentinel.Install, "registering this session's sentinel");
            PolarisAPI.Errors.Guard(AppendPreviousSession, "archiving how the previous session ended");
            Watchdog.Install();
            CrashWatcherLauncher.Launch();
        }

        static void AppendPreviousSession()
        {
            if (SessionSentinel.LastSession != null)
            {
                ErrorReportWriter.AppendPreviousSession(SessionSentinel.LastSession);
            }
        }

        internal static void Report(Exception exception, string context, Assembly culprit)
            => ErrorRegistry.Submit(exception, context, culprit);

        internal static void ReportLog(string condition, string stackTrace, string context)
            => ErrorRegistry.Submit(condition, stackTrace, context);

        internal static void CountLoggedError() => ErrorRegistry.CountLoggedErrors(1);
        internal static void RaiseFatal(FatalError fatal) => FatalRegistry.Raise(fatal);
        internal static bool IsFatal => FatalRegistry.Any;
        internal static IReadOnlyList<ErrorIncident> Incidents => ErrorRegistry.Incidents;
        internal static LastSessionInfo LastSession => SessionSentinel.LastSession;
        internal static SessionEndKind LastSessionEnd => SessionSentinel.LastEnd;
        internal static FatalError FirstFatal => FatalRegistry.First;
        internal static int OtherFatalCount => FatalRegistry.OtherCount;
        internal static string FatalReportPath => FatalRegistry.ReportPath;
        internal static string LastWrittenReportPath => ErrorReportWriter.LastWrittenPath;
        internal static double SecondsSinceLastFrame => MainThreadBeat.SecondsSinceBeat;
        internal static int HangCount => Watchdog.HangCount;

        internal static event Action<ErrorIncident> IncidentRecorded
        {
            add => ErrorRegistry.Recorded += value;
            remove => ErrorRegistry.Recorded -= value;
        }

        internal static event Action<HangReport> HangSuspected
        {
            add => Watchdog.HangSuspected += value;
            remove => Watchdog.HangSuspected -= value;
        }

        internal static IDisposable ExpectStall(string reason, double seconds)
            => Watchdog.ExpectStall(reason, seconds);

        internal static IDisposable Activity(string what, Assembly owner = null)
            => MainThreadBeat.Enter(what, owner);

        internal static void Beat(int frameCount) => MainThreadBeat.Beat(frameCount);
        internal static void SetPaused(bool paused) => Watchdog.SetPaused(paused);

        internal static void RecordCallbackInvocation(string ownerGuid, string context, double millis)
            => CallbackDiagnostics.RecordInvocation(ownerGuid, context, millis);

        internal static void RecordCallbackException(string ownerGuid, string context)
            => CallbackDiagnostics.RecordException(ownerGuid, context);

        internal static void Stop() => Watchdog.Uninstall();
        internal static string Summary() => ErrorRegistry.Summary();
        internal static void CloseSession() => SessionSentinel.Close();
    }
}
