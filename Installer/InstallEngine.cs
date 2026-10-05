using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace PolarisInstaller;

/// <summary>嵌入在安装器里的一个文件，以及它在游戏目录里的落点。</summary>
internal sealed record PayloadFile(string RelativePath, string ResourceName, bool IsPolaris);

/// <summary>当前游戏目录里的安装状况。</summary>
internal sealed class InstallState
{
    internal bool BepInExInstalled;
    internal string BepInExVersion;
    internal bool PolarisInstalled;
    internal bool PolarisUpToDate;
}

/// <summary>安装清单：记录这个安装器放过哪些文件、哪些是覆盖了玩家原有文件（已备份），卸载时按它还原。</summary>
internal sealed class Manifest
{
    public List<Entry> Files { get; set; } = new();

    public sealed class Entry
    {
        public string Path { get; set; }
        public bool IsPolaris { get; set; }

        /// <summary>安装前这个位置本来就有文件（已备份）；为 false 表示是我们新建的。</summary>
        public bool Replaced { get; set; }

        /// <summary>被备份走的原文件相对路径（相对备份目录）。</summary>
        public string Backup { get; set; }
    }
}

internal static class InstallEngine
{
    const string GameProcess = "AliceInCradle";
    static readonly Assembly Self = typeof(InstallEngine).Assembly;

    internal static string PluginsDir(string game) => Path.Combine(game, "BepInEx", "plugins");
    internal static string StateDir(string game) => Path.Combine(game, "BepInEx", "Polaris");
    internal static string ReportsDir(string game) => Path.Combine(StateDir(game), "reports");
    static string ManifestPath(string game) => Path.Combine(StateDir(game), "installer-manifest.json");

    // ================== 随包文件 ==================

    internal static IReadOnlyList<PayloadFile> Payload()
    {
        var list = new List<PayloadFile>();
        foreach (string name in Self.GetManifestResourceNames())
        {
            string normalized = name.Replace('\\', '/');

            if (normalized.StartsWith("game/", StringComparison.Ordinal))
            {
                list.Add(new PayloadFile(normalized.Substring("game/".Length).Replace('/', Path.DirectorySeparatorChar), name, false));
            }
            else if (normalized == "polaris/PolarisCore.dll")
            {
                list.Add(new PayloadFile(Path.Combine("BepInEx", "plugins", "PolarisCore.dll"), name, true));
            }
            else if (normalized.StartsWith("polaris/", StringComparison.Ordinal))
            {
                // Watcher、图片等：都放在 plugins/Polaris/ 下。
                list.Add(new PayloadFile(Path.Combine("BepInEx", "plugins", "Polaris", Path.GetFileName(normalized)), name, true));
            }
        }

        return list;
    }

    static byte[] ReadPayload(PayloadFile file)
    {
        using Stream stream = Self.GetManifestResourceStream(file.ResourceName);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));

    static string HashFile(string path)
    {
        try
        {
            using FileStream stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch (Exception)
        {
            return null;
        }
    }

    // ================== 状态 ==================

    internal static bool IsGameRunning()
    {
        try
        {
            return Process.GetProcessesByName(GameProcess).Length > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal static InstallState GetState(string game)
    {
        var state = new InstallState();

        string mono = Path.Combine(game, "BepInEx", "core", "BepInEx.Unity.Mono.dll");
        state.BepInExInstalled = File.Exists(Path.Combine(game, "winhttp.dll")) && File.Exists(mono);
        if (state.BepInExInstalled)
        {
            try
            {
                string version = FileVersionInfo.GetVersionInfo(mono).ProductVersion ?? "";
                int plus = version.IndexOf('+');
                state.BepInExVersion = plus > 0 ? version.Substring(0, plus) : version;
            }
            catch (Exception)
            {
            }
        }

        string core = Path.Combine(PluginsDir(game), "PolarisCore.dll");
        state.PolarisInstalled = File.Exists(core);
        if (state.PolarisInstalled)
        {
            PayloadFile payload = Payload().FirstOrDefault(p => p.IsPolaris && Path.GetFileName(p.RelativePath) == "PolarisCore.dll");
            state.PolarisUpToDate = payload != null && HashFile(core) == Hash(ReadPayload(payload));
        }

        return state;
    }

    // ================== 安装 ==================

    /// <returns>备份目录（没有备份任何文件时为 null）。</returns>
    internal static string Install(string game, bool reinstallBepInEx, Action<string> log)
    {
        if (IsGameRunning())
        {
            throw new InvalidOperationException(Loc.T("GameRunning"));
        }

        InstallState state = GetState(game);
        Manifest manifest = LoadManifest(game);
        string backupRoot = Path.Combine(StateDir(game), "installer-backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        bool backedUp = false;

        foreach (string dir in new[] { PluginsDir(game), Path.Combine(game, "BepInEx", "config"), Path.Combine(game, "BepInEx", "patchers"), Path.Combine(PluginsDir(game), "Polaris") })
        {
            Directory.CreateDirectory(dir);
        }

        foreach (PayloadFile file in Payload().OrderBy(p => p.IsPolaris))
        {
            // 已经装好 BepInEx 时不去动它（可能是玩家自己的版本/配置），除非明确要求重装。
            if (!file.IsPolaris && state.BepInExInstalled && !reinstallBepInEx)
            {
                continue;
            }

            string target = Path.Combine(game, file.RelativePath);
            byte[] data = ReadPayload(file);
            Manifest.Entry entry = manifest.Files.FirstOrDefault(e => string.Equals(e.Path, file.RelativePath, StringComparison.OrdinalIgnoreCase));

            if (File.Exists(target))
            {
                if (HashFile(target) == Hash(data))
                {
                    entry ??= Track(manifest, file, replaced: false);
                    continue;
                }

                // 只在第一次覆盖时备份：之后再装/更新，备份里始终是玩家最初的文件，而不是上一版 Polaris。
                if (entry == null)
                {
                    string backup = file.RelativePath;
                    string backupPath = Path.Combine(backupRoot, backup);
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                    File.Copy(target, backupPath, overwrite: true);
                    backedUp = true;
                    entry = Track(manifest, file, replaced: true);
                    entry.Backup = Path.Combine("installer-backup", Path.GetFileName(backupRoot), backup);
                }
            }
            else
            {
                entry ??= Track(manifest, file, replaced: false);
            }

            log(file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            string temp = target + ".polaris-tmp";
            File.WriteAllBytes(temp, data);
            File.Move(temp, target, overwrite: true);
        }

        SaveManifest(game, manifest);
        return backedUp ? backupRoot : null;
    }

    static Manifest.Entry Track(Manifest manifest, PayloadFile file, bool replaced)
    {
        var entry = new Manifest.Entry { Path = file.RelativePath, IsPolaris = file.IsPolaris, Replaced = replaced };
        manifest.Files.Add(entry);
        return entry;
    }

    // ================== 卸载 ==================

    internal static void Uninstall(string game, bool removeBepInEx, Action<string> log)
    {
        if (IsGameRunning())
        {
            throw new InvalidOperationException(Loc.T("GameRunning"));
        }

        Manifest manifest = LoadManifest(game);

        // 没有清单（手动装的）时，按随包文件名单清理，不碰名单之外的任何东西。
        if (manifest.Files.Count == 0)
        {
            foreach (PayloadFile file in Payload())
            {
                manifest.Files.Add(new Manifest.Entry { Path = file.RelativePath, IsPolaris = file.IsPolaris });
            }
        }

        var kept = new List<Manifest.Entry>();
        foreach (Manifest.Entry entry in manifest.Files)
        {
            if (!entry.IsPolaris && !removeBepInEx)
            {
                kept.Add(entry);
                continue;
            }

            string target = Path.Combine(game, entry.Path);
            if (File.Exists(target))
            {
                log(entry.Path);
                File.Delete(target);
            }

            if (entry.Replaced && entry.Backup != null)
            {
                string backup = Path.Combine(StateDir(game), entry.Backup);
                if (File.Exists(backup))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(backup, target, overwrite: true);
                }
            }
        }

        manifest.Files = kept;
        if (kept.Count == 0)
        {
            TryDelete(ManifestPath(game));
        }
        else
        {
            SaveManifest(game, manifest);
        }

        // 清掉因此变空的目录；报告、配置等玩家数据保留。
        foreach (string dir in new[] { Path.Combine(PluginsDir(game), "Polaris"), Path.Combine(game, "BepInEx", "core") })
        {
            TryRemoveIfEmpty(dir);
        }
    }

    // ================== 清单 ==================

    static Manifest LoadManifest(string game)
    {
        try
        {
            string path = ManifestPath(game);
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path)) ?? new Manifest();
            }
        }
        catch (Exception)
        {
        }

        return new Manifest();
    }

    static void SaveManifest(string game, Manifest manifest)
    {
        Directory.CreateDirectory(StateDir(game));
        File.WriteAllText(ManifestPath(game), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }

    static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception)
        {
        }
    }

    static void TryRemoveIfEmpty(string dir)
    {
        try
        {
            if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
            }
        }
        catch (Exception)
        {
        }
    }

    // ================== 诊断相关 ==================

    internal static string LatestReport(string game)
    {
        try
        {
            string dir = ReportsDir(game);
            if (!Directory.Exists(dir))
            {
                return null;
            }

            return new DirectoryInfo(dir).GetFiles("polaris-report_*.txt").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault()?.FullName;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
