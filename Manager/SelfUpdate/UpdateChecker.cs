using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Polaris.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;

namespace Polaris.SelfUpdate
{
    /// <summary>
    /// 游戏内自我更新。流程：启动后检查 GitHub 最新 Release → 有新版本就弹窗询问 → 玩家同意后下载并校验 SHA256、
    /// 解到暂存目录 → 游戏退出后由 PolarisWatcher.exe 把文件换上（运行中的 dll 无法覆盖，所以必须等游戏退出）。
    /// 全程只访问配置里指定的仓库、只走 HTTPS、只接受 <c>BepInEx/plugins/</c> 下的文件；玩家不点“更新”就什么都不会下载或改动。
    /// 任何网络错误都只记一行日志，绝不影响游戏。
    /// </summary>
    internal static class UpdateChecker
    {
        const string ZipName = "PolarisCore-manual.zip";
        const string AllowedPrefix = "BepInEx/plugins/";
        const int MaxZipBytes = 20 * 1024 * 1024;

        static string UpdateDir => Path.Combine(DiagnosticsRuntime.StateDir, "update");
        static string StagingDir => Path.Combine(UpdateDir, "staging");
        static string PendingFile => Path.Combine(UpdateDir, "pending.txt");
        static string LastCheckFile => Path.Combine(UpdateDir, "last-check.txt");
        static string SkippedFile => Path.Combine(UpdateDir, "skipped.txt");
        static string DoneFile => Path.Combine(UpdateDir, "done.txt");

        static bool started;

        /// <summary>由 <c>Plugin.Start</c> 调一次。</summary>
        internal static void Begin(MonoBehaviour host)
        {
            if (started)
            {
                return;
            }

            started = true;
            UpdateSettings.Resolve();
            host.StartCoroutine(Run());
        }

        static IEnumerator Run()
        {
            // 先等游戏进到标题画面、告知页弹完，别和启动期的提示挤在一起。
            yield return new WaitForSecondsRealtime(12f);

            ShowDoneNoticeIfAny();

            if (!UpdateSettings.Enabled || !DueForCheck())
            {
                yield break;
            }

            string repo = UpdateSettings.Repository;
            string url = $"{UpdateSettings.ApiBase}/repos/{repo}/releases/latest";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 15;
                request.SetRequestHeader("User-Agent", "PolarisCore/" + MyPluginInfo.PLUGIN_VERSION);
                request.SetRequestHeader("Accept", "application/vnd.github+json");
                yield return request.SendWebRequest();

                // 不论成败都记一次检查时间，避免断网时每次启动都重试刷日志。
                MarkChecked();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Plugin.Logger.LogInfo($"[Polaris] Update check skipped: {request.error}");
                    yield break;
                }

                Release release;
                try
                {
                    release = Release.Parse(request.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogInfo($"[Polaris] Update check: unreadable response ({e.Message})");
                    yield break;
                }

                if (release == null || !IsNewer(release.Version))
                {
                    yield break;
                }

                // 此前下载好、还没换上的更新如果比 GitHub 上的最新版更旧，就作废：否则玩家一退出游戏，Watcher 会先装上那个旧版本，
                // 下次启动再提示最新版，变成一个版本一个版本地爬。不论落后几个版本，都只直接更新到最新的那个。
                string pendingTag = File.Exists(PendingFile) ? Read(PendingFile)?.Split('\n')[0].Trim() : null;
                if (pendingTag != null && pendingTag != release.Tag)
                {
                    DiscardPending();
                    pendingTag = null;
                }

                if (Read(SkippedFile) == release.Tag)
                {
                    yield break;
                }

                if (pendingTag == release.Tag)
                {
                    // 这个版本已经下载好了，只等退出游戏。
                    ShowReady(release.Tag);
                    yield break;
                }

                Offer(release);
            }
        }

        // ================== 弹窗 ==================

        static void Offer(Release release)
        {
            string notes = CleanNotes(release.Notes);

            var item = new InGameAlert.Item
            {
                Key = "polaris-update",
                Title = UpdateStrings.UpdAvailable(release.Tag, "v" + MyPluginInfo.PLUGIN_VERSION),
                Body = (string.IsNullOrEmpty(notes) ? "" : UpdateStrings.UpdNotes("\n" + notes) + "\n\n") + UpdateStrings.UpdAsk(),
            };

            item.Buttons = new List<InGameAlert.NoticeButton>
            {
                new InGameAlert.NoticeButton { Label = UpdateStrings.UpdBtnUpdate(), OnClick = () => { StartDownload(item, release); return false; } },
                new InGameAlert.NoticeButton { Label = UpdateStrings.UpdBtnSkip(), OnClick = () => { Write(SkippedFile, release.Tag); return true; } },
                new InGameAlert.NoticeButton { Label = UpdateStrings.UpdBtnLater(), OnClick = () => true },
            };

            InGameAlert.ShowNotice(item);
        }

        /// <summary>
        /// 把 GitHub 自动生成的 Markdown 发布说明整理成适合弹窗的几行纯文本：去掉标题井号、加粗/代码标记、
        /// "Full Changelog" 之类的链接行，最多留 5 行、220 个字符。
        /// </summary>
        internal static string CleanNotes(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return "";
            }

            var lines = new List<string>();
            foreach (string raw in markdown.Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0
                    || line.StartsWith("**Full Changelog**", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("<!--", StringComparison.Ordinal))
                {
                    continue;
                }

                line = line.TrimStart('#', ' ');
                if (line.StartsWith("* ", StringComparison.Ordinal))
                {
                    line = "- " + line.Substring(2);
                }

                line = line.Replace("**", "").Replace("`", "");
                if (line.Length > 0)
                {
                    lines.Add(line);
                }

                if (lines.Count >= 5)
                {
                    break;
                }
            }

            string text = string.Join("\n", lines);
            return text.Length > 220 ? text.Substring(0, 220) + "…" : text;
        }

        static void StartDownload(InGameAlert.Item item, Release release)
        {
            item.Buttons = new List<InGameAlert.NoticeButton>();
            item.Status = UpdateStrings.UpdDownloading();
            Plugin.Instance.StartCoroutine(Download(item, release));
        }

        static IEnumerator Download(InGameAlert.Item item, Release release)
        {
            string zipUrl = release.AssetUrl(ZipName);
            string shaUrl = release.AssetUrl(ZipName + ".sha256");
            if (zipUrl == null || shaUrl == null)
            {
                Fail(item, "release has no " + ZipName);
                yield break;
            }

            byte[] zip = null;
            string shaText = null;

            using (UnityWebRequest sha = UnityWebRequest.Get(shaUrl))
            {
                sha.timeout = 20;
                sha.SetRequestHeader("User-Agent", "PolarisCore/" + MyPluginInfo.PLUGIN_VERSION);
                yield return sha.SendWebRequest();
                if (sha.result != UnityWebRequest.Result.Success)
                {
                    Fail(item, sha.error);
                    yield break;
                }

                shaText = sha.downloadHandler.text;
            }

            using (UnityWebRequest req = UnityWebRequest.Get(zipUrl))
            {
                req.timeout = 120;
                req.SetRequestHeader("User-Agent", "PolarisCore/" + MyPluginInfo.PLUGIN_VERSION);
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Fail(item, req.error);
                    yield break;
                }

                zip = req.downloadHandler.data;
            }

            item.Status = UpdateStrings.UpdVerifying();
            yield return null;

            string error = Stage(zip, shaText, release.Tag);
            if (error != null)
            {
                Fail(item, error);
                yield break;
            }

            ShowReadyIn(item, release.Tag);
        }

        static void Fail(InGameAlert.Item item, string why)
        {
            Plugin.Logger.LogWarning($"[Polaris] Update failed: {why}");
            item.Status = UpdateStrings.UpdFailed(why);
            item.Buttons = new List<InGameAlert.NoticeButton>
            {
                new InGameAlert.NoticeButton { Label = UpdateStrings.UpdBtnLater(), OnClick = () => true },
            };
        }

        static void ShowReady(string tag)
        {
            var item = new InGameAlert.Item { Key = "polaris-update", Title = UpdateStrings.UpdAvailable(tag, "v" + MyPluginInfo.PLUGIN_VERSION), Body = "" };
            ShowReadyIn(item, tag);
            InGameAlert.ShowNotice(item);
        }

        static void ShowReadyIn(InGameAlert.Item item, string tag)
        {
            bool hasWatcher = File.Exists(Path.Combine(PolarisAPI.Paths.PolarisRoot, "PolarisWatcher.exe"));
            item.Status = hasWatcher ? UpdateStrings.UpdReady(tag) : UpdateStrings.UpdNoWatcher();
            item.Buttons = new List<InGameAlert.NoticeButton>();

            if (hasWatcher)
            {
                item.Buttons.Add(new InGameAlert.NoticeButton { Label = UpdateStrings.UpdBtnQuit(), OnClick = () => { Application.Quit(); return true; } });
            }

            item.Buttons.Add(new InGameAlert.NoticeButton { Label = UpdateStrings.UpdBtnLater(), OnClick = () => true });
        }

        /// <summary>上次启动后 Watcher 完成了更新：提示一次。</summary>
        static void ShowDoneNoticeIfAny()
        {
            try
            {
                string done = Read(DoneFile);
                if (string.IsNullOrWhiteSpace(done))
                {
                    return;
                }

                File.Delete(DoneFile);
                InGameAlert.Toast("polaris-update-done", UpdateStrings.UpdDone(done.Trim()), "", null, null);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>丢掉已暂存但尚未应用的更新。</summary>
        static void DiscardPending()
        {
            try
            {
                if (Directory.Exists(StagingDir))
                {
                    Directory.Delete(StagingDir, recursive: true);
                }

                if (File.Exists(PendingFile))
                {
                    File.Delete(PendingFile);
                }

                Plugin.Logger.LogInfo("[Polaris] Discarded an older downloaded update; a newer release is available.");
            }
            catch (Exception)
            {
            }
        }

        // ================== 校验与暂存 ==================

        /// <returns>失败原因；成功为 null。</returns>
        static string Stage(byte[] zip, string shaText, string tag)
        {
            try
            {
                if (zip == null || zip.Length == 0 || zip.Length > MaxZipBytes)
                {
                    return "bad download size";
                }

                string expected = shaText.Trim().Split(' ', '\t', '\r', '\n')[0].ToLowerInvariant();
                string actual;
                using (var sha = SHA256.Create())
                {
                    actual = BitConverter.ToString(sha.ComputeHash(zip)).Replace("-", "").ToLowerInvariant();
                }

                if (expected != actual)
                {
                    return UpdateStrings.UpdBadHash();
                }

                if (Directory.Exists(StagingDir))
                {
                    Directory.Delete(StagingDir, recursive: true);
                }

                Directory.CreateDirectory(StagingDir);
                string stagingFull = Path.GetFullPath(StagingDir);
                var files = new List<string>();

                using (var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            continue;
                        }

                        string rel = entry.FullName.Replace('\\', '/');
                        if (!rel.StartsWith(AllowedPrefix, StringComparison.Ordinal) || rel.Contains(".."))
                        {
                            return "unexpected file in update: " + rel;
                        }

                        string target = Path.GetFullPath(Path.Combine(StagingDir, rel));
                        if (!target.StartsWith(stagingFull, StringComparison.OrdinalIgnoreCase))
                        {
                            return "unexpected path in update: " + rel;
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        entry.ExtractToFile(target, overwrite: true);
                        files.Add(rel);
                    }
                }

                if (files.Count == 0)
                {
                    return "empty update";
                }

                var pending = new StringBuilder();
                pending.Append(tag).Append('\n');
                foreach (string f in files)
                {
                    pending.Append(f).Append('\n');
                }

                File.WriteAllText(PendingFile, pending.ToString(), new UTF8Encoding(false));
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        // ================== 版本与节流 ==================

        static bool IsNewer(Version remote)
        {
            return remote != null && Version.TryParse(MyPluginInfo.PLUGIN_VERSION, out Version local) && remote > local;
        }

        static bool DueForCheck()
        {
            string text = Read(LastCheckFile);
            if (text != null && long.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks))
            {
                return (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalHours >= UpdateSettings.IntervalHours;
            }

            return true;
        }

        static void MarkChecked() => Write(LastCheckFile, DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));

        static string Read(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static void Write(string path, string text)
        {
            try
            {
                Directory.CreateDirectory(UpdateDir);
                File.WriteAllText(path, text, new UTF8Encoding(false));
            }
            catch (Exception)
            {
            }
        }

        // ================== Release 数据 ==================

        sealed class Release
        {
            internal string Tag;
            internal Version Version;
            internal string Notes;
            readonly Dictionary<string, string> assets = new(StringComparer.OrdinalIgnoreCase);

            internal string AssetUrl(string name) => assets.TryGetValue(name, out string url) ? url : null;

            internal static Release Parse(string json)
            {
                JObject o = JObject.Parse(json);
                string tag = (string)o["tag_name"];
                if (string.IsNullOrEmpty(tag))
                {
                    return null;
                }

                var release = new Release { Tag = tag, Notes = (string)o["body"] };
                string numeric = tag.TrimStart('v', 'V').Split('-', '+')[0];
                Version.TryParse(numeric, out release.Version);

                if (o["assets"] is JArray list)
                {
                    foreach (JToken asset in list)
                    {
                        string name = (string)asset["name"];
                        string url = (string)asset["browser_download_url"];
                        // 只认 HTTPS 下载地址（本机测试用的 127.0.0.1 服务除外）。
                        bool secure = url != null && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                        bool localTest = url != null && url.StartsWith("http://127.0.0.1", StringComparison.Ordinal)
                                         && UpdateSettings.ApiBase.StartsWith("http://127.0.0.1", StringComparison.Ordinal);
                        if (!string.IsNullOrEmpty(name) && (secure || localTest))
                        {
                            release.assets[name] = url;
                        }
                    }
                }

                return release;
            }
        }
    }
}
