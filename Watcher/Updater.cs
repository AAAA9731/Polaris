using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Polaris.Watcher
{
    /// <summary>
    /// 游戏内“自我更新”的最后一步：插件把校验过的新文件解到 <c>BepInEx/Polaris/update/staging</c> 并写好 <c>pending.txt</c>，
    /// 游戏一退出（无论正常还是崩溃）这里就把文件换到位。必须等游戏退出——运行中的 dll 无法覆盖。
    /// 只会动 <c>BepInEx/plugins/</c> 下 pending.txt 列出的文件；被替换的旧文件备份到 <c>update/backup</c>，方便回退。
    /// 本程序自己的 exe 正在运行无法覆盖，改用“先改名为 .old 再写入新文件”（Windows 允许给运行中的 exe 改名）。
    /// </summary>
    internal static class Updater
    {
        const string AllowedPrefix = "BepInEx/plugins/";

        internal static void TryApply(string stateDir)
        {
            string update = Path.Combine(stateDir, "update");
            string pending = Path.Combine(update, "pending.txt");
            string game = null;

            try
            {
                CleanOldFiles(stateDir);

                if (!File.Exists(pending))
                {
                    return;
                }

                // stateDir = <game>\BepInEx\Polaris
                game = Directory.GetParent(Directory.GetParent(stateDir.TrimEnd('\\', '/')).FullName).FullName;
                string[] lines = File.ReadAllLines(pending, Encoding.UTF8);
                string version = lines[0].Trim();
                string[] files = lines.Skip(1).Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
                string staging = Path.Combine(update, "staging");
                string backup = Path.Combine(update, "backup");

                foreach (string rel in files)
                {
                    if (!rel.StartsWith(AllowedPrefix, StringComparison.Ordinal) || rel.Contains(".."))
                    {
                        throw new InvalidOperationException("unexpected path in pending list: " + rel);
                    }
                }

                // 游戏刚退出，句柄可能还没完全释放：给它几秒。
                Thread.Sleep(1500);

                foreach (string rel in files)
                {
                    string src = Path.Combine(staging, rel.Replace('/', '\\'));
                    string dst = Path.Combine(game, rel.Replace('/', '\\'));
                    if (!File.Exists(src))
                    {
                        throw new FileNotFoundException("staged file missing", src);
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(dst));

                    if (File.Exists(dst))
                    {
                        string bak = Path.Combine(backup, rel.Replace('/', '\\'));
                        Directory.CreateDirectory(Path.GetDirectoryName(bak));
                        File.Copy(dst, bak, overwrite: true);
                    }

                    Place(src, dst);
                }

                Directory.Delete(staging, recursive: true);
                File.Delete(pending);
                File.WriteAllText(Path.Combine(update, "done.txt"), version, new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                try
                {
                    Directory.CreateDirectory(update);
                    File.WriteAllText(Path.Combine(update, "error.txt"), DateTime.Now + " " + e, new UTF8Encoding(false));
                }
                catch (Exception)
                {
                }
            }
        }

        static void Place(string src, string dst)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    File.Copy(src, dst, overwrite: true);
                    return;
                }
                catch (IOException)
                {
                    if (attempt == 4)
                    {
                        break;
                    }

                    Thread.Sleep(600);
                }
                catch (UnauthorizedAccessException)
                {
                    break;
                }
            }

            // 还是覆盖不了（例如正在运行的自己）：把旧文件改名挪开，再写新的。
            string old = dst + ".old";
            if (File.Exists(old))
            {
                File.Delete(old);
            }

            File.Move(dst, old);
            File.Copy(src, dst, overwrite: true);
        }

        /// <summary>清掉上一次更新留下的 .old 文件（它们当时被占用，现在多半已经能删了）。</summary>
        static void CleanOldFiles(string stateDir)
        {
            try
            {
                string game = Directory.GetParent(Directory.GetParent(stateDir.TrimEnd('\\', '/')).FullName).FullName;
                string plugins = Path.Combine(game, "BepInEx", "plugins");
                if (!Directory.Exists(plugins))
                {
                    return;
                }

                foreach (string file in Directory.GetFiles(plugins, "*.old", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
