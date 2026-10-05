using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Polaris.Diagnostics
{
    internal enum FlagState
    {
        None,

        /// <summary>出过持续/严重问题，且模组文件自那以后没有变化：问题多半还在。</summary>
        Flagged,

        /// <summary>出过问题，但模组文件之后被改动过（更新/替换）：之前的问题可能已修复，只提示这一次。</summary>
        Updated,
    }

    internal sealed class ModFlagInfo
    {
        internal FlagState State;
        internal ErrorSeverity Level;
        internal string Reason;
        internal DateTime When;
    }

    /// <summary>
    /// 出过持续/严重问题的模组的标记簿，写在 <c>BepInEx/Polaris/flagged-mods.txt</c>，跨启动保留，供模组管理页显示警示。
    /// 标记绑定的是"模组文件的指纹"（大小 + 修改时间）：文件没变，标记一直在；文件被更新过，说明作者可能已经修了，
    /// 此时标记换成一条"已更新"的提示，显示过一次之后就清掉。写盘只发生在新增标记/清除标记时，不会频繁。
    /// </summary>
    internal static class ModFlags
    {
        const string FileName = "flagged-mods.txt";
        const string DisabledSuffix = ".disabled";

        sealed class Entry
        {
            internal string Path;
            internal ErrorSeverity Level;
            internal long Size;
            internal long Ticks;
            internal DateTime When;
            internal string Reason;
        }

        static readonly object Gate = new();
        static readonly Dictionary<string, Entry> entries = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>本局已经提示过"已更新"的条目：磁盘上的标记清掉了，但本局内浮窗还要能查到这句话。</summary>
        static readonly Dictionary<string, Entry> shownUpdated = new(StringComparer.OrdinalIgnoreCase);

        static bool loaded;

        static string FilePath => System.IO.Path.Combine(DiagnosticsRuntime.StateDir, FileName);

        /// <summary>记下一个模组出了问题。同一份文件上级别只升不降；文件变了则按新文件重新记。线程安全，绝不抛异常。</summary>
        internal static void Flag(AssemblyOwner owner, ErrorSeverity level, string reason)
        {
            try
            {
                if (owner == null || owner.Kind != OwnerKind.Mod || string.IsNullOrEmpty(owner.FullPath))
                {
                    return;
                }

                if (!TryFingerprint(owner.FullPath, out long size, out long ticks))
                {
                    return;
                }

                lock (Gate)
                {
                    EnsureLoaded();

                    if (entries.TryGetValue(owner.FullPath, out Entry existing)
                        && existing.Size == size && existing.Ticks == ticks && existing.Level >= level)
                    {
                        return;
                    }

                    entries[owner.FullPath] = new Entry
                    {
                        Path = owner.FullPath,
                        Level = level,
                        Size = size,
                        Ticks = ticks,
                        When = DateTime.Now,
                        Reason = Clean(reason),
                    };

                    shownUpdated.Remove(owner.FullPath);
                    Save();
                }
            }
            catch (Exception)
            {
                // 记标记失败不能影响错误处置流程。
            }
        }

        /// <summary>查某个模组当前的标记状态。<paramref name="enabledPath"/> 是启用状态下的 dll 路径。</summary>
        internal static ModFlagInfo Lookup(string enabledPath, string disabledPath)
        {
            try
            {
                lock (Gate)
                {
                    EnsureLoaded();

                    if (shownUpdated.TryGetValue(enabledPath, out Entry shown))
                    {
                        return Info(shown, FlagState.Updated);
                    }

                    if (!entries.TryGetValue(enabledPath, out Entry entry))
                    {
                        return null;
                    }

                    // 指纹要看当前真实存在的那个文件（启用 = .dll，禁用 = .dll.disabled；改名不改大小和修改时间）。
                    string current = File.Exists(enabledPath) ? enabledPath : disabledPath;
                    if (current == null || !TryFingerprint(current, out long size, out long ticks))
                    {
                        return null;
                    }

                    bool same = entry.Size == size && entry.Ticks == ticks;
                    return Info(entry, same ? FlagState.Flagged : FlagState.Updated);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>页面把"已更新"提示显示出来之后调用：把这些条目从磁盘记录里清掉（本局内仍可查到）。</summary>
        internal static void ClearUpdated(IEnumerable<(string Enabled, string Disabled)> mods)
        {
            try
            {
                lock (Gate)
                {
                    EnsureLoaded();
                    bool changed = false;

                    foreach ((string enabled, string disabled) in mods)
                    {
                        if (!entries.TryGetValue(enabled, out Entry entry))
                        {
                            continue;
                        }

                        string current = File.Exists(enabled) ? enabled : disabled;
                        if (current != null && TryFingerprint(current, out long size, out long ticks)
                            && (entry.Size != size || entry.Ticks != ticks))
                        {
                            shownUpdated[enabled] = entry;
                            entries.Remove(enabled);
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        Save();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        static ModFlagInfo Info(Entry entry, FlagState state)
            => new ModFlagInfo { State = state, Level = entry.Level, Reason = entry.Reason, When = entry.When };

        static bool TryFingerprint(string path, out long size, out long ticks)
        {
            size = 0;
            ticks = 0;

            try
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    info = new FileInfo(path + DisabledSuffix);
                    if (!info.Exists)
                    {
                        return false;
                    }
                }

                size = info.Length;
                ticks = info.LastWriteTimeUtc.Ticks;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ================== 持久化（调用方已持锁）==================

        static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;

            try
            {
                if (!File.Exists(FilePath))
                {
                    return;
                }

                foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length < 6
                        || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int level)
                        || !long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long size)
                        || !long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks)
                        || !long.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out long whenTicks))
                    {
                        continue;
                    }

                    entries[parts[4]] = new Entry
                    {
                        Path = parts[4],
                        Level = (ErrorSeverity)level,
                        Size = size,
                        Ticks = ticks,
                        When = new DateTime(whenTicks),
                        Reason = parts[5],
                    };
                }
            }
            catch (Exception)
            {
                // 读不出来就当没有标记，不影响游戏。
            }
        }

        static void Save()
        {
            try
            {
                if (entries.Count == 0)
                {
                    if (File.Exists(FilePath))
                    {
                        File.Delete(FilePath);
                    }

                    return;
                }

                Directory.CreateDirectory(DiagnosticsRuntime.StateDir);
                var b = new StringBuilder();
                foreach (Entry e in entries.Values)
                {
                    b.Append((int)e.Level).Append('\t')
                     .Append(e.Size.ToString(CultureInfo.InvariantCulture)).Append('\t')
                     .Append(e.Ticks.ToString(CultureInfo.InvariantCulture)).Append('\t')
                     .Append(e.When.Ticks.ToString(CultureInfo.InvariantCulture)).Append('\t')
                     .Append(e.Path).Append('\t')
                     .Append(e.Reason).Append('\n');
                }

                File.WriteAllText(FilePath, b.ToString(), Encoding.UTF8);
            }
            catch (Exception)
            {
            }
        }

        static string Clean(string value)
            => string.IsNullOrEmpty(value) ? "" : value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
    }
}
