using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace PolarisInstaller;

/// <summary>找到《Alice in Cradle》的安装目录：安装器自己所在目录、Steam 库、常见位置，依次尝试。</summary>
internal static class GameLocator
{
    internal const string ExeName = "AliceInCradle.exe";

    /// <summary>是不是游戏根目录：有主程序，也有 Unity 数据目录。</summary>
    internal static bool IsGameFolder(string path)
        => !string.IsNullOrEmpty(path)
           && File.Exists(Path.Combine(path, ExeName))
           && Directory.Exists(Path.Combine(path, "AliceInCradle_Data"));

    /// <summary>把玩家拖入/选中的路径规整成游戏根目录：给的是 exe 就取它所在目录。</summary>
    internal static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        path = path.Trim().Trim('"');
        if (File.Exists(path))
        {
            path = Path.GetDirectoryName(path);
        }

        return IsGameFolder(path) ? Path.GetFullPath(path) : null;
    }

    internal static string Detect()
    {
        foreach (string candidate in Candidates())
        {
            string found = Normalize(candidate);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    static IEnumerable<string> Candidates()
    {
        // 安装器就放在游戏目录里（或上一层）运行。
        string here = AppContext.BaseDirectory;
        yield return here;
        string parent = Directory.GetParent(here.TrimEnd(Path.DirectorySeparatorChar))?.FullName;
        if (parent != null)
        {
            yield return parent;
        }

        foreach (string library in SteamLibraries())
        {
            string common = Path.Combine(library, "steamapps", "common");
            if (!Directory.Exists(common))
            {
                continue;
            }

            string[] dirs;
            try
            {
                dirs = Directory.GetDirectories(common);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (string dir in dirs)
            {
                if (Path.GetFileName(dir).Contains("Cradle", StringComparison.OrdinalIgnoreCase))
                {
                    yield return dir;
                }
            }
        }

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
            {
                continue;
            }

            yield return Path.Combine(drive.Name, "Games", "AliceInCradle");
            yield return Path.Combine(drive.Name, "AliceInCradle");
        }
    }

    static IEnumerable<string> SteamLibraries()
    {
        var roots = new List<string>();

        foreach (string key in new[] { @"HKEY_CURRENT_USER\Software\Valve\Steam", @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam" })
        {
            foreach (string name in new[] { "SteamPath", "InstallPath" })
            {
                if (Registry.GetValue(key, name, null) is string value && Directory.Exists(value))
                {
                    roots.Add(Path.GetFullPath(value));
                }
            }
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in roots)
        {
            if (seen.Add(root))
            {
                yield return root;
            }

            string vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf))
            {
                continue;
            }

            string text;
            try
            {
                text = File.ReadAllText(vdf);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (Match m in Regex.Matches(text, "\"path\"\\s+\"([^\"]+)\""))
            {
                string library = m.Groups[1].Value.Replace(@"\\", @"\");
                if (Directory.Exists(library) && seen.Add(library))
                {
                    yield return library;
                }
            }
        }
    }
}
