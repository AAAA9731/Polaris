using System;
using System.Collections.Generic;

namespace Polaris.Diagnostics
{
    /// <summary>
    /// 严重度阶梯的唯一决策点：错误登记、看门狗、致命登记都把事件交到这里，由这里决定游戏内反馈到哪一级。
    /// 可能从任意线程被调用；对外的副作用只有两种——给 <see cref="InGameAlert"/> 入队、向 <see cref="FatalExit"/> 请求退出，
    /// 两者都是线程安全且不会抛异常的。
    /// </summary>
    internal static class SeverityPolicy
    {
        /// <summary>风暴期间 LastSeen 超过这么久没更新，就认为这场风暴已经平息。</summary>
        const double StormQuietSeconds = 5d;

        sealed class ModState
        {
            internal AssemblyOwner Owner;
            internal int Kinds;
            internal int Occurrences;
            internal bool PersistentShown;
            internal ErrorIncident Storm;
            internal DateTime StormSince;
        }

        static readonly object Gate = new();
        static readonly Dictionary<string, ModState> mods = new(StringComparer.OrdinalIgnoreCase);
        static DateTime lastTick = DateTime.MinValue;

        // ================== 入口 ==================

        /// <summary>新建档了一类错误（已持有 <see cref="ErrorRegistry"/> 的锁，必须快进快出）。</summary>
        internal static void OnNewIncident(ErrorIncident incident)
        {
            try
            {
                AssemblyOwner owner = BlameOf(incident);

                if (IsStructural(incident, owner, out string what))
                {
                    incident.Severity = ErrorSeverity.Critical;
                    FatalExit.Request(AlertStrings.ReasonStructural(what), "Polaris", null);
                    return;
                }

                if (incident.ExceptionType == "System.OutOfMemoryException")
                {
                    incident.Severity = ErrorSeverity.Critical;
                    FatalExit.Request(AlertStrings.ReasonOom, owner?.DisplayName ?? "OutOfMemory", owner);
                    return;
                }

                if (owner == null)
                {
                    incident.Severity = ErrorSeverity.Noted;
                    return;
                }

                incident.Severity = ErrorSeverity.Minor;
                string report = ErrorReportWriter.LastWrittenPath;

                ModState state = StateOf(owner);
                int kinds;
                lock (Gate)
                {
                    state.Kinds++;
                    state.Occurrences += 1;
                    kinds = state.Kinds;
                }

                bool suspect = incident.Verdict.Confidence != ErrorConfidence.High;
                string title = owner.Kind == OwnerKind.Polaris
                    ? AlertStrings.ToastPolaris(kinds)
                    : suspect
                        ? AlertStrings.ToastSuspect(owner.DisplayName ?? owner.FileName, kinds)
                        : AlertStrings.ToastTitle(owner.DisplayName ?? owner.FileName, kinds);

                InGameAlert.Toast(KeyOf(owner), title, DescribeIncident(incident), owner, report);

                if (owner.Kind == OwnerKind.Mod && kinds >= DiagnosticsConfig.EscalateKinds)
                {
                    ShowPersistent(state, AlertStrings.PersistKinds(owner.DisplayName ?? owner.FileName, kinds), incident);
                    incident.Severity = ErrorSeverity.Persistent;
                }
            }
            catch (Exception)
            {
                // 分级本身出问题不能影响登记流程。
            }
        }

        /// <summary>已归档的一类错误被判定为风暴（同样持有登记锁）。</summary>
        internal static void OnStorm(ErrorIncident incident)
        {
            try
            {
                incident.Severity = ErrorSeverity.Persistent;

                AssemblyOwner owner = BlameOf(incident);
                if (owner == null || owner.Kind != OwnerKind.Mod)
                {
                    return;
                }

                ModState state = StateOf(owner);
                lock (Gate)
                {
                    if (state.Storm == null)
                    {
                        state.Storm = incident;
                        state.StormSince = DateTime.Now;
                    }
                }

                ShowPersistent(state, AlertStrings.PersistBody, incident);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>看门狗在后台线程判定了一次卡死。</summary>
        internal static void OnHang(HangReport report)
        {
            try
            {
                AssemblyOwner owner = report.Culprit != null ? AssemblyOwnerIndex.Of(report.Culprit) : null;
                if (owner == null || owner.Kind != OwnerKind.Mod)
                {
                    return;
                }

                ModState state = StateOf(owner);
                ShowPersistent(state, AlertStrings.PersistHang(owner.DisplayName ?? owner.FileName), null);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>某个模块显式报出了致命错误。</summary>
        internal static void OnFatal(FatalError fatal)
        {
            try
            {
                AssemblyOwner owner = null;
                if (fatal.Culprits.Count > 0)
                {
                    owner = AssemblyOwnerIndex.Of(fatal.Culprits[0]);
                }

                string reason = fatal.Reason?.Pick(AlertStrings.Language) ?? "";
                FatalExit.Request(reason, owner?.DisplayName ?? fatal.Source, owner, writeReport: false);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>看门狗发现主线程卡死已超过致命阈值（此时主线程画不出任何东西，直接终止）。</summary>
        internal static void OnHangCritical(HangReport report, double stallSeconds)
        {
            AssemblyOwner owner = null;
            try
            {
                owner = report?.Culprit != null ? AssemblyOwnerIndex.Of(report.Culprit) : null;
            }
            catch (Exception)
            {
            }

            string mod = owner != null && owner.Kind == OwnerKind.Mod ? owner.DisplayName ?? owner.FileName : null;
            string reason = mod != null
                ? AlertStrings.ReasonHangMod((int)stallSeconds, mod)
                : AlertStrings.ReasonHang((int)stallSeconds);
            FatalExit.RequestFromStuckThread(reason, mod ?? "main thread hang", owner);
        }

        /// <summary>主线程每帧调用，内部自己限流到每秒一次：跟踪风暴是否持续、是否同时太多模组在风暴。</summary>
        internal static void Tick()
        {
            DateTime now = DateTime.Now;
            if ((now - lastTick).TotalSeconds < 1d)
            {
                return;
            }

            lastTick = now;

            try
            {
                int storming = 0;
                string worstMod = null;
                int worstSeconds = 0;
                AssemblyOwner worstOwner = null;

                lock (Gate)
                {
                    foreach (ModState state in mods.Values)
                    {
                        if (state.Storm == null)
                        {
                            continue;
                        }

                        if ((now - state.Storm.LastSeen).TotalSeconds > StormQuietSeconds)
                        {
                            // 风暴平息了：允许将来再次判定。
                            state.Storm = null;
                            continue;
                        }

                        storming++;
                        int seconds = (int)(now - state.StormSince).TotalSeconds;
                        if (seconds > worstSeconds)
                        {
                            worstSeconds = seconds;
                            worstMod = state.Owner.DisplayName ?? state.Owner.FileName;
                            worstOwner = state.Owner;
                        }
                    }
                }

                if (storming >= DiagnosticsConfig.CriticalStormMods)
                {
                    FatalExit.Request(AlertStrings.ReasonMods(storming), worstMod, worstOwner);
                }
                else if (worstOwner != null && worstSeconds >= DiagnosticsConfig.CriticalStormSeconds)
                {
                    FatalExit.Request(AlertStrings.ReasonStorm(worstMod, worstSeconds), worstMod, worstOwner);
                }
            }
            catch (Exception)
            {
            }
        }

        // ================== 辅助 ==================

        static void ShowPersistent(ModState state, string body, ErrorIncident incident)
        {
            lock (Gate)
            {
                if (state.PersistentShown)
                {
                    return;
                }

                state.PersistentShown = true;
            }

            AssemblyOwner owner = state.Owner;
            ModFlags.Flag(owner, ErrorSeverity.Persistent, incident != null ? DescribeIncident(incident) : body);
            InGameAlert.Persistent(
                KeyOf(owner),
                AlertStrings.PersistTitle(owner.DisplayName ?? owner.FileName),
                body + (incident != null ? "\n\n" + DescribeIncident(incident) : ""),
                owner,
                ErrorReportWriter.LastWrittenPath);
        }

        static ModState StateOf(AssemblyOwner owner)
        {
            lock (Gate)
            {
                string key = KeyOf(owner);
                if (!mods.TryGetValue(key, out ModState state))
                {
                    state = new ModState { Owner = owner };
                    mods[key] = state;
                }

                return state;
            }
        }

        internal static string KeyOf(AssemblyOwner owner)
            => owner.FullPath ?? owner.DisplayName ?? owner.FileName ?? "?";

        /// <summary>这次错误该由谁担责：直接责任人优先，其次最可疑的嫌疑人；原版/框架/运行时不算。</summary>
        internal static AssemblyOwner BlameOf(ErrorIncident incident)
        {
            ErrorVerdict verdict = incident?.Verdict;
            if (verdict == null)
            {
                return null;
            }

            if (verdict.Culprit != null && verdict.Culprit.IsBlamable)
            {
                return verdict.Culprit;
            }

            foreach (ErrorSuspect suspect in verdict.Suspects)
            {
                if (suspect.Owner != null && suspect.Owner.IsBlamable)
                {
                    return suspect.Owner;
                }
            }

            return null;
        }

        /// <summary>Polaris 自己的补丁/核心因为游戏版本不匹配而坏掉：继续运行只会得到错误的结果。</summary>
        static bool IsStructural(ErrorIncident incident, AssemblyOwner owner, out string what)
        {
            what = null;
            if (owner == null || owner.Kind != OwnerKind.Polaris)
            {
                return false;
            }

            switch (incident.ExceptionType)
            {
                case "System.TypeLoadException":
                case "System.MissingMethodException":
                case "System.MissingFieldException":
                case "System.MissingMemberException":
                case "System.Reflection.ReflectionTypeLoadException":
                    what = incident.ExceptionType.Substring(incident.ExceptionType.LastIndexOf('.') + 1);
                    return true;
                default:
                    return false;
            }
        }

        static string DescribeIncident(ErrorIncident incident)
        {
            string type = incident.ExceptionType ?? "?";
            int dot = type.LastIndexOf('.');
            if (dot >= 0)
            {
                type = type.Substring(dot + 1);
            }

            string message = incident.Message ?? "";
            if (message.Length > 160)
            {
                message = message.Substring(0, 160) + "…";
            }

            return type + (message.Length > 0 ? ": " + message : "");
        }
    }
}
