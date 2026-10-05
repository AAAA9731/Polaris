using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Polaris.Watcher
{
    /// <summary>
    /// 用法：<c>PolarisWatcher.exe --pid N --state DIR --plugins DIR --player-log FILE --bep-log FILE</c>。
    /// 等待游戏进程退出；若退出时 Polaris 的会话哨兵还在（说明没走正常关闭流程），分析原因并弹窗；否则静默退出。
    /// </summary>
    internal static class Program
    {
        static void Log(string dir, string line)
        {
            try
            {
                File.AppendAllText(Path.Combine(dir, "watcher.log"), DateTime.Now.ToString("HH:mm:ss ") + line + Environment.NewLine);
            }
            catch (Exception)
            {
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

        [DllImport("kernel32.dll")]
        static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll")]
        static extern bool GetExitCodeProcess(IntPtr handle, out uint exitCode);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);

        [STAThread]
        static int Main(string[] args)
        {
            string Arg(string name)
            {
                int i = Array.IndexOf(args, name);
                return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
            }

            if (!int.TryParse(Arg("--pid"), out int pid) || Arg("--state") == null)
            {
                return 2;
            }

            string state = Arg("--state");
            string plugins = Arg("--plugins") ?? state;
            string playerLog = Arg("--player-log") ?? "";
            string bepLog = Arg("--bep-log") ?? "";
            bool force = args.Contains("--force");

            int exitCode;
            DateTime started;
            try
            {
                // .NET Framework 的 Process.ExitCode 对"非自己启动的进程"取不到，直接走 Win32：等待句柄 + GetExitCodeProcess。
                using (Process game = Process.GetProcessById(pid))
                {
                    started = game.StartTime;
                }

                IntPtr handle = OpenProcess(0x00100000 | 0x1000, false, pid); // SYNCHRONIZE | QUERY_LIMITED_INFORMATION
                if (handle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("OpenProcess failed: " + Marshal.GetLastWin32Error());
                }

                WaitForSingleObject(handle, 0xFFFFFFFF);
                if (!GetExitCodeProcess(handle, out uint code))
                {
                    code = 0;
                }

                CloseHandle(handle);
                exitCode = unchecked((int)code);
            }
            catch (Exception e)
            {
                Log(state, "wait failed: " + e);
                return 3;
            }

            // 给正常退出路径一点时间删掉哨兵，避免误报。
            Thread.Sleep(1500);
            if (!force && !CrashAnalysis.SentinelExists(state, pid))
            {
                Log(state, "clean exit, pid " + pid);
                return 0;
            }

            Log(state, "abnormal exit, pid " + pid + ", code " + exitCode);

            CrashFindings findings = CrashAnalysis.Analyze(state, plugins, playerLog, bepLog, pid, started, exitCode);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ReportForm(findings, state));
            return 0;
        }
    }

    sealed class ReportForm : Form
    {
        internal ReportForm(CrashFindings f, string stateDir)
        {
            Text = "Polaris · " + T.Title;
            ClientSize = new Size(720, 520);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);
            MinimumSize = new Size(560, 380);

            var head = new Label
            {
                Text = f.Headline,
                Dock = DockStyle.Top,
                Height = 52,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 40, 40),
                Padding = new Padding(14, 14, 14, 0),
            };

            var causes = new Label
            {
                Text = string.Join("\r\n\r\n", f.Causes.Select(c => "• " + c)),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 40 + 34 * f.Causes.Count,
                Padding = new Padding(14, 4, 14, 4),
            };

            var box = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                Text = f.Details.ToString().Replace("\n", "\r\n"),
                BackColor = Color.White,
            };
            box.Select(0, 0);

            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 8, 8, 12) };
            void Make(string text, Action click)
            {
                var b = new Button { Text = text, AutoSize = true, Padding = new Padding(8, 2, 8, 2) };
                b.Click += (s, e) => click();
                bar.Controls.Add(b);
            }

            Make(T.Close, Close);
            Make(T.Copy, () => Clipboard.SetText(f.Headline + "\r\n\r\n" + string.Join("\r\n", f.Causes) + "\r\n\r\n" + box.Text));
            Make(T.OpenLogs, () => Open(Path.GetDirectoryName(f.BepInExLogPath) ?? stateDir));
            if (f.ReportPath != null)
            {
                Make(T.OpenReport, () => Open(f.ReportPath));
            }

            Controls.Add(box);
            Controls.Add(causes);
            Controls.Add(head);
            Controls.Add(bar);
            TopMost = true;
            Shown += (s, e) => TopMost = false;
        }

        static void Open(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception)
            {
            }
        }
    }
}
