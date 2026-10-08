using System;

namespace Polaris.Diagnostics
{
    /// <summary>游戏内提示的三语文案（中/英/日），写在代码里：错误发生时本地化机制本身可能就不可用。英/日译文由 dsh 生成。</summary>
    internal static class AlertStrings
    {
        /// <summary>当前语言；由 <see cref="InGameAlert"/> 每秒刷新一次，避免每帧去问游戏。</summary>
        internal static Polaris.Localization.Language Language = Polaris.Localization.Language.English;

        internal static string P(string zh, string en, string ja)
            => PolarisAPI.Localization.Pick(Language, zh, en, ja);

        static string F(string format, params object[] args)
        {
            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        internal static string ToastTitle(string mod, int count) => F(P("模组「{0}」出错（累计 {1} 次）", "Mod \"{0}\" encountered an error ({1} so far)", "Mod「{0}」でエラーが発生しました（累計 {1} 回）"), mod, count);
        internal static string ToastSuspect(string mod, int count) => F(P("疑似模组「{0}」出错（累计 {1} 次）", "Possible error from mod \"{0}\" ({1} so far)", "Mod「{0}」のエラーの疑い（累計 {1} 回）"), mod, count);
        internal static string ToastPolaris(int count) => F(P("Polaris 自身出错（累计 {0} 次）", "Polaris itself encountered an error ({0} so far)", "Polaris 自体でエラーが発生しました（累計 {0} 回）"), count);
        internal static string ToastHint => P("点击查看详情", "Click for details", "クリックして詳細を表示");

        internal static string PersistTitle(string mod) => F(P("模组「{0}」持续出错", "Mod \"{0}\" keeps erroring", "Mod「{0}」がエラーを繰り返しています"), mod);
        internal static string PersistBody => P("它在反复触发同类错误，对应功能本局基本已失效。游戏仍可继续，但建议禁用它。", "It keeps triggering the same kind of error, and that feature is effectively broken for this session. You can keep playing, but disabling the mod is recommended.", "同じ種類のエラーを繰り返しており、このセッションでは該当機能はほぼ機能しません。ゲームは続行できますが、無効化をおすすめします。");
        internal static string PersistKinds(string mod, int kinds) => F(P("模组「{0}」已触发 {1} 种不同的错误，很可能存在严重问题。游戏仍可继续，但建议禁用它。", "Mod \"{0}\" has triggered {1} different errors and very likely has a serious problem. You can keep playing, but disabling it is recommended.", "Mod「{0}」が{1}種類の異なるエラーを発生させており、深刻な問題が存在する可能性が高いです。ゲームは続行できますが、無効化をおすすめします。"), mod, kinds);
        internal static string PersistHang(string mod) => F(P("游戏曾长时间无响应，卡在模组「{0}」的代码里。游戏仍可继续，但建议禁用它。", "The game stopped responding for a long time, stuck in mod \"{0}\"'s code. You can keep playing, but disabling it is recommended.", "ゲームが長時間応答せず、Mod「{0}」のコードで停止していました。ゲームは続行できますが、無効化をおすすめします。"), mod);

        internal static string BtnDisable => P("禁用该模组（重启生效）", "Disable this mod (takes effect after restart)", "このModを無効化（再起動後に反映）");
        internal static string BtnIgnore => P("忽略本局", "Ignore for this session", "今回のセッションでは無視");
        internal static string BtnReport => P("查看报告", "View report", "レポートを表示");
        internal static string DisabledOk => P("已禁用，重启游戏后生效。", "Disabled. It will take effect after you restart the game.", "無効化しました。ゲームを再起動すると反映されます。");
        internal static string DisableFail(string why) => F(P("无法禁用：{0}", "Could not disable: {0}", "無効化できません：{0}"), why);
        internal static string CannotDisable => P("这个错误不是来自可禁用的模组。", "This error does not come from a mod that can be disabled.", "このエラーは無効化できるModによるものではありません。");

        internal static string CritTitle => P("游戏需要退出", "The game needs to exit", "ゲームを終了する必要があります");
        internal static string CritCountdown(int seconds) => F(P("{0} 秒后自动退出（未保存的进度会丢失）", "Exiting automatically in {0} seconds (unsaved progress will be lost)", "{0} 秒後に自動的に終了します（保存されていない進行状況は失われます）"), seconds);
        internal static string CritNoQuit => P("Polaris 没有自动退出游戏，但继续游玩可能出现严重问题，建议保存后重启。", "Polaris did not exit the game automatically, but continuing to play may cause serious problems. Please save and restart.", "Polarisはゲームを自動終了しませんでしたが、そのままプレイを続けると深刻な問題が発生する可能性があります。保存して再起動することをおすすめします。");
        internal static string Responsible(string who) => P("责任方：" + who, "Responsible: " + who, "原因となったもの：" + who);
        internal static string BtnQuitNow => P("立即退出", "Quit now", "今すぐ終了");
        internal static string BtnDismiss => P("知道了", "Got it", "了解");

        internal static string ReasonStorm(string mod, int seconds) => F(P("模组「{0}」持续出错已超过 {1} 秒，游戏已无法正常运行。", "Mod \"{0}\" has been erroring for over {1} seconds; the game can no longer run properly.", "Mod「{0}」のエラーが{1}秒以上続いており、ゲームは正常に動作できません。"), mod, seconds);
        internal static string ReasonMods(int count) => F(P("{0} 个模组同时在持续出错，游戏已无法正常运行。", "{0} mods are erroring at the same time; the game can no longer run properly.", "{0}個のModが同時にエラーを繰り返しており、ゲームは正常に動作できません。"), count);
        internal static string ReasonHang(int seconds) => F(P("游戏已无响应超过 {0} 秒。", "The game has been unresponsive for over {0} seconds.", "ゲームが{0}秒以上応答していません。"), seconds);
        internal static string ReasonHangMod(int seconds, string mod) => F(P("游戏已无响应超过 {0} 秒，卡在模组「{1}」的代码里。", "The game has been unresponsive for over {0} seconds, stuck in mod \"{1}\"'s code.", "ゲームが{0}秒以上応答していません。Mod「{1}」のコードで停止しています。"), seconds, mod);
        internal static string ReasonStructural(string what) => F(P("Polaris 的核心补丁无法工作（{0}），继续游戏会得到错误的结果。", "Polaris's core patches are not working ({0}); continuing to play will produce incorrect results.", "Polarisの中核パッチが機能していません（{0}）。プレイを続けると誤った結果になります。"), what);
        internal static string ReasonOom => P("游戏内存已耗尽，继续运行很可能崩溃。", "The game has run out of memory; continuing is very likely to crash it.", "ゲームのメモリが枯渇しました。続行するとクラッシュする可能性が高いです。");
    }
}
