using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Polaris.Watcher
{
    /// <summary>游戏进程退出后能拿到的全部线索，以及据此得出的结论。</summary>
    internal sealed class CrashFindings
    {
        internal string Headline;
        internal readonly List<string> Causes = new List<string>();
        internal readonly StringBuilder Details = new StringBuilder();
        internal string ReportPath;
        internal string PlayerLogPath;
        internal string BepInExLogPath;
    }

    /// <summary>
    /// 游戏进程在 Polaris 的哨兵文件还在时退出，即为非正常结束。这里收集四类线索：
    /// 进程退出码、Windows 事件日志里的崩溃记录（故障模块）、Unity Player.log 里的崩溃栈、哨兵里的最后状态与错误摘要。
    /// </summary>
    internal static class CrashAnalysis
    {
        const int LogTailBytes = 400 * 1024;

        internal static bool SentinelExists(string stateDir, int pid) => File.Exists(Path.Combine(stateDir, "_session_" + pid + ".txt"));

        internal static CrashFindings Analyze(string stateDir, string pluginsDir, string playerLog, string bepLog, int pid, DateTime started, int exitCode)
        {
            var f = new CrashFindings { PlayerLogPath = playerLog, BepInExLogPath = bepLog };
            Dictionary<string, string> sentinel = ReadSentinel(Path.Combine(stateDir, "_session_" + pid + ".txt"));
            string report = Get(sentinel, "report");
            f.ReportPath = report != null && File.Exists(report) ? report : null;

            string exitName = DescribeExit(exitCode);
            string faultModule = null;
            string faultCode = null;
            ReadEventLog(started, ref faultModule, ref faultCode);
            List<string> stack = ReadPlayerLogCrash(playerLog, out string logReason);
            string modOwner = BlameMod(faultModule, stack, pluginsDir);

            if (Get(sentinel, "kind") == "fatal")
            {
                f.Headline = T.Terminated;
                f.Causes.Add(Get(sentinel, "fatal_reason") ?? "");
                if (Get(sentinel, "fatal_culprit") != null)
                {
                    f.Causes.Add(T.Responsible(Get(sentinel, "fatal_culprit")));
                }
            }
            else if (Get(sentinel, "kind") == "hung")
            {
                f.Headline = T.Hung;
                f.Causes.Add(T.HungDetail(Get(sentinel, "stall"), Get(sentinel, "activity")));
            }
            else if (logReason == "oom")
            {
                f.Headline = T.Oom;
                f.Causes.Add(T.OomDetail);
            }
            else if (logReason == "stackoverflow" || exitCode == unchecked((int)0xC00000FD))
            {
                f.Headline = T.StackOverflow;
                f.Causes.Add(T.StackOverflowDetail);
            }
            else if (exitName != null || faultModule != null || logReason != null)
            {
                f.Headline = T.NativeCrash;
                if (exitName != null)
                {
                    f.Causes.Add(T.ExitCodeCause(exitName));
                }
            }
            else
            {
                f.Headline = T.Vanished;
                f.Causes.Add(T.VanishedDetail(exitCode));
            }

            if (modOwner != null)
            {
                f.Causes.Insert(0, T.BlameMod(modOwner));
            }
            else if (faultModule != null)
            {
                f.Causes.Add(T.FaultModuleCause(faultModule));
            }

            string errors = string.Join("\n", sentinel
                .Where(kv => kv.Key.Length > 5 && kv.Key.StartsWith("error", StringComparison.Ordinal) && char.IsDigit(kv.Key[5]))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => "  · " + kv.Value));
            if (errors.Length > 0)
            {
                f.Causes.Add(T.EarlierErrors.TrimEnd('：', ':'));
            }

            f.Details.AppendLine(T.Basics);
            foreach (string line in new[]
            {
                T.Line("Exit code", exitName != null ? exitName + " (0x" + exitCode.ToString("X8") + ")" : exitCode.ToString(CultureInfo.InvariantCulture)),
                T.Line("Scene", Get(sentinel, "scene")),
                T.Line("Frame", Get(sentinel, "frame")),
                T.Line("Started", Get(sentinel, "started")),
                T.Line("Last alive", Get(sentinel, "alive")),
                T.Line("Last activity", Get(sentinel, "activity")),
                T.Line("Faulting module", faultModule),
                T.Line("Exception code", faultCode),
            })
            {
                if (line != null)
                {
                    f.Details.AppendLine("  " + line);
                }
            }
            f.Details.AppendLine();

            if (errors.Length > 0)
            {
                f.Details.AppendLine(T.EarlierErrors).AppendLine(errors).AppendLine();
            }

            if (stack.Count > 0)
            {
                f.Details.AppendLine(T.NativeStack);
                foreach (string line in stack.Take(25))
                {
                    f.Details.AppendLine("  " + line);
                }
                f.Details.AppendLine();
            }

            List<string> bepErrors = ReadBepInExErrors(bepLog);
            if (bepErrors.Count > 0)
            {
                f.Details.AppendLine(T.BepErrors);
                foreach (string line in bepErrors)
                {
                    f.Details.AppendLine("  " + line);
                }
            }

            return f;
        }

        static string DescribeExit(int code)
        {
            switch (unchecked((uint)code))
            {
                case 0xC0000005: return "ACCESS_VIOLATION";
                case 0xC00000FD: return "STACK_OVERFLOW";
                case 0xC0000409: return "STACK_BUFFER_OVERRUN / fail-fast";
                case 0xC0000374: return "HEAP_CORRUPTION";
                case 0xC000001D: return "ILLEGAL_INSTRUCTION";
                case 0xC0000094: return "INT_DIVIDE_BY_ZERO";
                case 0xC0000096: return "PRIVILEGED_INSTRUCTION";
                case 0x80000003: return "BREAKPOINT";
                case 0xE0434352: return "Unhandled .NET exception";
                case 0xE06D7363: return "Unhandled C++ exception";
                case 0xC0000017: return "NO_MEMORY";
                case 0xC000012D: return "COMMITMENT_LIMIT (out of virtual memory)";
                default: return null;
            }
        }

        /// <summary>Windows 事件日志里"应用程序错误"(ID 1000) 要晚几秒才写入，所以轮询等一会儿。</summary>
        static void ReadEventLog(DateTime started, ref string module, ref string code)
        {
            DateTime since = started.ToUniversalTime().AddSeconds(-5);
            string query = "*[System[(EventID=1000) and TimeCreated[@SystemTime>='"
                + since.ToString("yyyy-MM-ddTHH:mm:ss.000Z", CultureInfo.InvariantCulture) + "']]]";

            for (int attempt = 0; attempt < 8; attempt++)
            {
                try
                {
                    using (var reader = new EventLogReader(new EventLogQuery("Application", PathType.LogName, query) { ReverseDirection = true }))
                    {
                        for (EventRecord record = reader.ReadEvent(); record != null; record = reader.ReadEvent())
                        {
                            using (record)
                            {
                                // ID 1000 的属性顺序：应用名、版本、时间戳、故障模块名、模块版本、模块时间戳、异常码……
                                if (record.Properties.Count < 7
                                    || !string.Equals(Convert.ToString(record.Properties[0].Value), "AliceInCradle.exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                module = Convert.ToString(record.Properties[3].Value);
                                code = Convert.ToString(record.Properties[6].Value);
                                return;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return;
                }

                Thread.Sleep(1000);
            }
        }

        static List<string> ReadPlayerLogCrash(string path, out string reason)
        {
            reason = null;
            var stack = new List<string>();
            string text = ReadTail(path);
            if (text == null)
            {
                return stack;
            }

            string[] lines = text.Split('\n');
            int crashAt = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("Crash!!!", StringComparison.Ordinal) || lines[i].Contains("OUTPUTTING STACK TRACE"))
                {
                    crashAt = i;
                }
            }

            if (Regex.IsMatch(text, "Out of memory|OutOfMemory|Could not allocate memory|Failed to allocate", RegexOptions.IgnoreCase))
            {
                reason = "oom";
            }
            else if (Regex.IsMatch(text, "Stack overflow|StackOverflow", RegexOptions.IgnoreCase))
            {
                reason = "stackoverflow";
            }
            else if (crashAt >= 0)
            {
                reason = "crash";
            }

            if (crashAt >= 0)
            {
                for (int i = crashAt; i < lines.Length && stack.Count < 60; i++)
                {
                    string l = lines[i].Trim();
                    if (l.StartsWith("0x", StringComparison.Ordinal) || l.StartsWith("at ", StringComparison.Ordinal)
                        || l.Contains("Fatal") || l.StartsWith("Crash!!!", StringComparison.Ordinal))
                    {
                        stack.Add(l);
                    }
                }
            }

            return stack;
        }

        static List<string> ReadBepInExErrors(string path)
        {
            var result = new List<string>();
            string text = ReadTail(path);
            if (text == null)
            {
                return result;
            }

            foreach (string raw in text.Split('\n'))
            {
                string l = raw.TrimEnd();
                if (l.StartsWith("[Error", StringComparison.Ordinal) || l.StartsWith("[Fatal", StringComparison.Ordinal))
                {
                    result.Add(l.Length > 260 ? l.Substring(0, 260) + "…" : l);
                }
            }

            return result.Skip(Math.Max(0, result.Count - 6)).ToList();
        }

        static string ReadTail(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Seek(Math.Max(0, fs.Length - LogTailBytes), SeekOrigin.Begin);
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>故障模块或原生栈里出现了位于 plugins 下的非 Polaris dll，就判定是它。</summary>
        static string BlameMod(string faultModule, List<string> stack, string pluginsDir)
        {
            var names = new List<string>();
            if (!string.IsNullOrEmpty(faultModule))
            {
                names.Add(faultModule);
            }

            foreach (string line in stack)
            {
                foreach (Match m in Regex.Matches(line, @"\(([^()\s]+)\)"))
                {
                    names.Add(m.Groups[1].Value);
                }
            }

            try
            {
                foreach (string file in Directory.GetFiles(pluginsDir, "*.dll", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (name.StartsWith("Polaris", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (names.Any(n => string.Equals(Path.GetFileNameWithoutExtension(n), name, StringComparison.OrdinalIgnoreCase)))
                    {
                        return Path.GetFileName(file);
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        static Dictionary<string, string> ReadSentinel(string path)
        {
            var d = new Dictionary<string, string>();
            try
            {
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    int eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        d[line.Substring(0, eq)] = line.Substring(eq + 1);
                    }
                }
            }
            catch (Exception)
            {
            }

            return d;
        }

        static string Get(Dictionary<string, string> d, string key) => d.TryGetValue(key, out string v) && v.Length > 0 ? v : null;
    }
}
