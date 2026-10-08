using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Polaris
{
    /// <summary>管理 plugins 根目录下 dll 的启停：改名在 .dll 与 .dll.disabled 之间切换，下次启动才会生效。</summary>
    internal static class UserModToggleManager
    {
        const string DisabledSuffix = ".disabled";

        /// <summary>扫描 <c>plugins</c> 根目录，按去掉 <c>.disabled</c> 后缀的文件名归并出启停记录。</summary>
        internal static List<UserModRecord> Scan()
        {
            string selfFileName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

            // 管理器靠 [BepInDependency] 依赖库：库照常列出，但标为核心、不给启停。
            string coreFileName = Path.GetFileName(typeof(CorePlugin).Assembly.Location);
            var byDisplayName = new Dictionary<string, UserModRecord>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(PolarisAPI.Paths.PluginsRoot))
            {
                return [];
            }

            string root = PolarisAPI.Paths.PluginsRoot;
            string polarisRoot = PolarisAPI.Paths.PolarisRoot;

            // 根目录下的 dll 一律列出（启用的和 .disabled 的）。
            var candidates = Directory.GetFiles(root, "*.dll*", SearchOption.TopDirectoryOnly).ToList();

            // 放在子文件夹里的模组（如 plugins/SomeMod/SomeMod.dll）：启用的以 BepInEx 实际加载的插件为准，
            // 免得把同文件夹里的依赖库（Newtonsoft.Json.dll 之类）也当成模组；被禁用的没加载过，只能认 .dll.disabled 后缀。
            candidates.AddRange(PolarisModInfoResolver.LoadedPluginLocations().Where(p => IsInSubfolder(p, root, polarisRoot)));
            candidates.AddRange(Directory.GetFiles(root, "*.dll" + DisabledSuffix, SearchOption.AllDirectories)
                .Where(p => IsInSubfolder(p, root, polarisRoot)));

            foreach (string path in candidates)
            {
                string fileName = Path.GetFileName(path);
                bool isDisabled = fileName.EndsWith(".dll" + DisabledSuffix, StringComparison.OrdinalIgnoreCase);
                bool isEnabled = !isDisabled && fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
                if (!isDisabled && !isEnabled)
                {
                    continue;
                }

                string enabledFileName = isDisabled
                    ? fileName.Substring(0, fileName.Length - DisabledSuffix.Length)
                    : fileName;

                // 管理器自己不列出：禁用它，这个页面本身就没了，玩家无从再启用。
                if (string.Equals(enabledFileName, selfFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 记录的键是相对 plugins 根的路径：根目录里就是文件名，子文件夹里带上文件夹，避免不同文件夹的同名 dll 串到一起。
                string directory = Path.GetDirectoryName(Path.GetFullPath(path));
                string enabledPath = Path.Combine(directory, enabledFileName);
                string displayName = IsRoot(directory, root)
                    ? enabledFileName
                    : GetRelativePath(root, enabledPath);

                if (!byDisplayName.TryGetValue(displayName, out UserModRecord record))
                {
                    record = new UserModRecord
                    {
                        DisplayName = displayName,
                        EnabledPath = enabledPath,
                        DisabledPath = enabledPath + DisabledSuffix,
                        Info = PolarisModInfoResolver.Resolve(enabledFileName),
                        IsCore = string.Equals(displayName, coreFileName, StringComparison.OrdinalIgnoreCase),
                    };
                    byDisplayName[displayName] = record;
                }

                record.Enabled = isEnabled;
            }

            // 核心排最前，其余按名字。
            return byDisplayName.Values
                .OrderByDescending(r => r.IsCore)
                .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>把文件改名到目标启停状态；已在目标状态直接返回成功，失败记到 <see cref="UserModRecord.Error"/> 并记日志，不抛异常。</summary>
        static bool IsRoot(string directory, string root) =>
            string.Equals(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

        /// <summary>在 plugins 根的子文件夹里，且不在 Polaris 自己的支持文件夹里。</summary>
        static bool IsInSubfolder(string path, string root, string polarisRoot)
        {
            string full = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(full);
            return !IsRoot(directory, root)
                && full.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !full.StartsWith(Path.GetFullPath(polarisRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        static string GetRelativePath(string root, string path) =>
            Path.GetFullPath(path).Substring(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1);

        internal static bool SetEnabled(UserModRecord record, bool enabled)
        {
            if (record.IsCore)
            {
                return false;
            }

            if (record.Enabled == enabled)
            {
                record.Error = null;
                return true;
            }

            string from = enabled ? record.DisabledPath : record.EnabledPath;
            string to = enabled ? record.EnabledPath : record.DisabledPath;

            try
            {
                File.Move(from, to);
                record.Enabled = enabled;
                record.Error = null;
                return true;
            }
            catch (Exception ex)
            {
                record.Error = ex.Message;
                Plugin.Logger.LogWarning($"[Polaris] Failed to toggle mod \"{record.DisplayName}\": {ex.Message}");
                return false;
            }
        }
    }
}
