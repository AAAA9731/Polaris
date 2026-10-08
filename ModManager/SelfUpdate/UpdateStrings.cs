using System;

namespace Polaris.SelfUpdate
{
    /// <summary>自动更新提示的三语文案（中/英/日）。英/日译文由 dsh (DeepSeek Harness) 生成。</summary>
    internal static class UpdateStrings
    {
        static string P(string zh, string en, string ja) => Diagnostics.AlertStrings.P(zh, en, ja);

        static string F(string format, object[] args)
        {
            try
            {
                return args.Length == 0 ? format : string.Format(format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        internal static string UpdAvailable(params object[] a) => F(P("发现 Polaris 新版本 {0}（当前 {1}）", "New Polaris version {0} is available (current: {1})", "Polaris の新しいバージョン {0} があります（現在 {1}）"), a);
        internal static string UpdNotes(params object[] a) => F(P("更新内容：{0}", "What's new: {0}", "更新内容：{0}"), a);
        internal static string UpdAsk(params object[] a) => F(P("更新会自动下载并校验，在你退出游戏之后替换文件，不会打断当前游戏。", "The update downloads and verifies automatically, then replaces the files after you exit the game. Your current session won't be interrupted.", "更新は自動でダウンロード・検証され、ゲーム終了後にファイルが置き換わります。プレイ中のゲームには影響しません。"), a);
        internal static string UpdBtnUpdate(params object[] a) => F(P("更新", "Update", "更新"), a);
        internal static string UpdBtnLater(params object[] a) => F(P("以后再说", "Later", "後で"), a);
        internal static string UpdBtnSkip(params object[] a) => F(P("跳过此版本", "Skip this version", "このバージョンをスキップ"), a);
        internal static string UpdDownloading(params object[] a) => F(P("正在下载…", "Downloading…", "ダウンロード中…"), a);
        internal static string UpdVerifying(params object[] a) => F(P("正在校验…", "Verifying…", "検証中…"), a);
        internal static string UpdReady(params object[] a) => F(P("{0} 已准备好，退出游戏后会自动完成更新。", "{0} is ready. The update will finish automatically after you exit the game.", "{0} の準備ができました。ゲーム終了後に自動で更新が完了します。"), a);
        internal static string UpdBtnQuit(params object[] a) => F(P("立即退出并更新", "Quit and update", "終了して更新"), a);
        internal static string UpdFailed(params object[] a) => F(P("更新失败：{0}", "Update failed: {0}", "更新に失敗しました：{0}"), a);
        internal static string UpdBadHash(params object[] a) => F(P("下载的文件校验不通过，已放弃。", "The downloaded file failed verification and was discarded.", "ダウンロードしたファイルの検証に失敗したため、中止しました。"), a);
        internal static string UpdNoWatcher(params object[] a) => F(P("没有找到 PolarisWatcher.exe，无法在退出游戏后自动完成更新。请重新运行安装器。", "PolarisWatcher.exe was not found, so the update can't finish automatically after you exit the game. Please run the installer again.", "PolarisWatcher.exe が見つからないため、ゲーム終了後に自動で更新を完了できません。インストーラーを再実行してください。"), a);
        internal static string UpdDone(params object[] a) => F(P("Polaris 已更新到 {0}。", "Polaris has been updated to {0}.", "Polaris を {0} に更新しました。"), a);
    }
}
