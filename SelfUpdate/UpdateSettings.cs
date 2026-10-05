using System;
using System.IO;
using BepInEx.Configuration;

namespace Polaris.SelfUpdate
{
    /// <summary>自动更新的配置，在 <c>BepInEx/config/Polaris/_polaris_update.cfg</c>。绑定失败则整体退回默认值。</summary>
    internal static class UpdateSettings
    {
        const string FileName = "_polaris_update.cfg";

        static bool resolved;
        static ConfigEntry<bool> enabled;
        static ConfigEntry<float> intervalHours;
        static ConfigEntry<string> repository;
        static ConfigEntry<string> apiBase;

        internal static void Resolve()
        {
            if (resolved)
            {
                return;
            }

            resolved = true;

            try
            {
                string dir = Diagnostics.DiagnosticsRuntime.ConfigDir;
                Directory.CreateDirectory(dir);
                var file = new ConfigFile(Path.Combine(dir, FileName), saveOnInit: false);
                file.SaveOnConfigSet = false;

                enabled = file.Bind("Update", "CheckForUpdates", true,
                    "Check GitHub for a newer Polaris release when the game starts. A popup asks before anything is downloaded; nothing is installed without your confirmation.");

                intervalHours = file.Bind("Update", "CheckIntervalHours", 12f,
                    "Minimum hours between two update checks.");

                repository = file.Bind("Update", "Repository", "AAAA9731/Polaris",
                    "GitHub repository (owner/name) whose latest release is checked.");

                apiBase = file.Bind("Update", "ApiBase", "https://api.github.com",
                    "GitHub API base URL. Only change this for testing.");

                file.Save();
                file.SaveOnConfigSet = true;
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"[Polaris] Failed to open {FileName}; update settings fall back to defaults: {e.Message}");
            }
        }

        internal static bool Enabled => enabled?.Value ?? true;
        internal static float IntervalHours => Math.Max(0.1f, intervalHours?.Value ?? 12f);
        internal static string Repository => string.IsNullOrWhiteSpace(repository?.Value) ? "AAAA9731/Polaris" : repository.Value.Trim();
        internal static string ApiBase => (string.IsNullOrWhiteSpace(apiBase?.Value) ? "https://api.github.com" : apiBase.Value.Trim()).TrimEnd('/');
    }
}
