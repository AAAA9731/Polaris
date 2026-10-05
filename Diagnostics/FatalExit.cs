using System;
using System.Diagnostics;
using System.Threading;

namespace Polaris.Diagnostics
{
    /// <summary>
    /// 严重度最高一级的处置：先把"为什么要结束"写进哨兵和报告，再让玩家看到提示，倒计时后主动结束游戏。
    /// 只会触发一次。哨兵在退出后仍然保留（见 <see cref="SessionSentinel.Close"/>），
    /// 外部的 PolarisWatcher 据此弹出带责任模组的说明窗口，下一局的标题画面也能读到。
    /// </summary>
    internal static class FatalExit
    {
        /// <summary>提示出现后留给主线程正常退出的宽限，超过就由后备线程强杀（防止 Application.Quit 自己也卡住）。</summary>
        const int GraceSeconds = 8;

        static int started;

        /// <summary>已经走上退出流程。哨兵据此保留文件，不当成正常退出清掉。</summary>
        internal static bool IsExiting => Volatile.Read(ref started) != 0;

        /// <summary>请求结束游戏。线程安全，可从任何线程调用；主线程还活着时走"提示 + 倒计时 + Application.Quit"。</summary>
        internal static void Request(string reason, string culpritName, AssemblyOwner culprit, bool writeReport = true)
        {
            if (Interlocked.Exchange(ref started, 1) != 0)
            {
                return;
            }

            try
            {
                Record(reason, culpritName, writeReport);

                if (!DiagnosticsConfig.AutoQuit)
                {
                    // 只提示不退出：重置，允许之后再触发（比如另一个模组也出了事）。
                    InGameAlert.Critical(reason, culpritName, culprit, quit: false);
                    Volatile.Write(ref started, 0);
                    return;
                }

                InGameAlert.Critical(reason, culpritName, culprit, quit: true);
                StartBackstop((int)DiagnosticsConfig.QuitCountdownSeconds + GraceSeconds);
            }
            catch (Exception)
            {
                // 处置本身出错时仍然保证能退出，否则"严重"就变成了"静默继续"。
                StartBackstop(GraceSeconds);
            }
        }

        /// <summary>主线程已卡死时的版本：画不出提示，直接留证据再终止进程，由 Watcher 窗口负责解释。</summary>
        internal static void RequestFromStuckThread(string reason, string culpritName, AssemblyOwner culprit)
        {
            if (!DiagnosticsConfig.AutoQuit)
            {
                return;
            }

            if (Interlocked.Exchange(ref started, 1) != 0)
            {
                return;
            }

            try
            {
                Record(reason, culpritName, true);
                DiagnosticsRuntime.Logger.LogError($"[Polaris] Ending the game: {reason}");
            }
            catch (Exception)
            {
            }

            KillNow();
        }

        /// <summary>由 <see cref="InGameAlert"/> 在倒计时结束或玩家点"立即退出"时调用，在主线程执行。</summary>
        internal static void QuitNow()
        {
            try
            {
                DiagnosticsHost.Stop();
                UnityEngine.Application.Quit(1);
            }
            catch (Exception)
            {
                KillNow();
            }
        }

        static void Record(string reason, string culpritName, bool writeReport)
        {
            SessionSentinel.MarkFatal(culpritName, reason);

            if (writeReport)
            {
                var fatal = new FatalError("Polaris severity policy", new FatalText(reason));
                fatal.Details.Add("Ended by the severity policy (level 3).");
                ErrorReportWriter.AppendFatal(fatal);
            }

            DiagnosticsRuntime.Logger.LogError($"[Polaris] Level 3 error -- the game will exit. Reason: {reason}");
        }

        static void StartBackstop(int seconds)
        {
            try
            {
                var thread = new Thread(() =>
                {
                    Thread.Sleep(seconds * 1000);
                    KillNow();
                })
                {
                    IsBackground = true,
                    Name = "Polaris.FatalExit",
                };
                thread.Start();
            }
            catch (Exception)
            {
            }
        }

        static void KillNow()
        {
            try
            {
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception)
            {
            }
        }
    }
}
