using System.IO;

namespace PolarisInstaller;

/// <summary>校验玩家选的游戏目录。游戏没上 Steam，不做任何自动搜索，由玩家自己定位。</summary>
internal static class GameLocator
{
    internal const string ExeName = "AliceInCradle.exe";

    /// <summary>是不是游戏根目录：有主程序，也有 Unity 数据目录。</summary>
    internal static bool IsGameFolder(string path)
        => !string.IsNullOrEmpty(path)
           && File.Exists(Path.Combine(path, ExeName))
           && Directory.Exists(Path.Combine(path, "AliceInCradle_Data"));

    /// <summary>把玩家拖入/选中的路径规整成游戏根目录：给的是 exe 就取它所在目录；不是游戏目录返回 null。</summary>
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

    /// <summary>安装器就放在游戏目录里运行时，直接用它（只看自己所在目录，不搜索）。</summary>
    internal static string FromOwnFolder() => Normalize(AppContext.BaseDirectory);
}
