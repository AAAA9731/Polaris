namespace Polaris.Localization
{
    /// <summary>
    /// Polaris 自己那几条设置项文案的内置翻译，写在代码里而非 <c>.plang</c>：
    /// 设置项在 <c>Plugin.Awake</c> 绑定配置文件时就要查表（写进 <c>.cfg</c> 注释），早于 <c>.plang</c> 在 <c>Start</c> 才生效的注册。
    /// </summary>
    internal static class PolarisStrings
    {
        /// <summary>key 前缀。带 <c>polaris.</c> 是为了和模组自己的 key 分开，不会互相顶掉。</summary>
        const string P = "polaris.settings.";

        internal const string TitleVersionLine = "&" + P + "title_version";
        internal const string TitleVersionLineDesc = "&" + P + "title_version.desc";
        internal const string ErrorNotice = "&" + P + "error_notice";
        internal const string ErrorNoticeDesc = "&" + P + "error_notice.desc";
        internal const string TabTitle = "&" + P + "tab_title";
        internal const string Alerts = "&" + P + "alerts";
        internal const string AlertsDesc = "&" + P + "alerts.desc";
        internal const string AlertLevel = "&" + P + "alert_level";
        internal const string AlertLevelDesc = "&" + P + "alert_level.desc";
        internal const string AlertLevelAll = "&" + P + "alert_level.all";
        internal const string AlertLevelPersistent = "&" + P + "alert_level.persistent";
        internal const string AlertSeconds = "&" + P + "alert_seconds";
        internal const string AlertSecondsDesc = "&" + P + "alert_seconds.desc";
        internal const string AlertCorner = "&" + P + "alert_corner";
        internal const string AlertCornerDesc = "&" + P + "alert_corner.desc";
        internal const string CornerTopRight = "&" + P + "corner.top_right";
        internal const string CornerTopLeft = "&" + P + "corner.top_left";
        internal const string CornerBottomRight = "&" + P + "corner.bottom_right";
        internal const string CornerBottomLeft = "&" + P + "corner.bottom_left";
        internal const string CheckUpdates = "&" + P + "check_updates";
        internal const string CheckUpdatesDesc = "&" + P + "check_updates.desc";
        internal const string AutoQuit = "&" + P + "auto_quit";
        internal const string AutoQuitDesc = "&" + P + "auto_quit.desc";

        static bool registered;

        /// <summary>由 <c>Plugin.Awake</c> 调一次，须早于设置项扫描（<c>Plugin.Start</c>）以便绑定配置文件时表里已有文案。</summary>
        internal static void Register()
        {
            if (registered)
            {
                return;
            }

            registered = true;

            LocalizationAPI loc = PolarisAPI.Localization;

            loc.Register(P + "title_version", new LocalizedText("Version line on title screen")
            {
                ["zh"] = "标题画面版本行",
                ["ja"] = "タイトル画面のバージョン表記",
            });

            loc.Register(P + "title_version.desc", new LocalizedText(
                "Show a \"Polaris vX.Y.Z\" line under the game version on the title screen.\n"
                + "Hiding it changes nothing else.")
            {
                ["zh"] = "在标题画面的游戏版本号下面显示一行 \"Polaris vX.Y.Z\"。\n"
                       + "关掉只是不显示这一行，别的不受影响。",
                ["ja"] = "タイトル画面のバージョン表記の下に「Polaris vX.Y.Z」を表示します。\n"
                       + "オフにしても表示が消えるだけです。",
            });

            loc.Register(P + "error_notice", new LocalizedText("Report previous run's errors")
            {
                ["zh"] = "提示上一局的错误",
                ["ja"] = "前回のエラーを通知",
            });

            loc.Register(P + "error_notice.desc", new LocalizedText(
                "If the previous run hit mod errors, crashed or froze, show a summary on the "
                + "title screen.\nReports go to BepInEx/Polaris/reports either way.")
            {
                ["zh"] = "上一局出现模组错误、崩溃或卡死时，在标题画面列出摘要。\n"
                       + "无论开关，报告都会写进 BepInEx/Polaris/reports。",
                ["ja"] = "前回の実行でMODエラー・クラッシュ・フリーズがあった場合、"
                       + "タイトル画面に概要を表示します。\n"
                       + "レポートはどちらでも BepInEx/Polaris/reports に出力されます。",
            });

            loc.Register(P + "tab_title", new LocalizedText("Polaris Settings")
            {
                ["zh"] = "Polaris 设置",
                ["ja"] = "Polaris 設定",
            });

            loc.Register(P + "alerts", new LocalizedText("In-game error alerts")
            {
                ["zh"] = "游戏内错误提示",
                ["ja"] = "ゲーム内エラー通知",
            });
            loc.Register(P + "alerts.desc", new LocalizedText("Shows an alert on screen when a mod runs into an error.\nWhen off, errors are only written to the log and error reports, with no pop-ups (except alerts for fatal errors that close the game).")
            {
                ["zh"] = "模组出错时，在游戏画面里弹出提示。\n关掉后只写日志和错误报告，不再弹窗（严重错误导致游戏退出时的提示除外）。",
                ["ja"] = "Modでエラーが発生した際、ゲーム画面に通知を表示します。\nオフにするとログとエラーレポートに記録されるだけで、ポップアップは表示されません（ゲーム終了につながる重大エラーの通知を除く）。",
            });
            loc.Register(P + "alert_level", new LocalizedText("Minimum alert level")
            {
                ["zh"] = "提示最低级别",
                ["ja"] = "通知の最低レベル",
            });
            loc.Register(P + "alert_level.desc", new LocalizedText("With \"Persistent and above\" selected, only repeated errors of the same kind or serious problems trigger an alert;\nminor one-off errors are only written to reports, with no pop-up.")
            {
                ["zh"] = "选“仅持续及以上”时，只有同类错误反复出现或严重问题才会提示；\n偶发的小错误只写进报告，不弹窗。",
                ["ja"] = "「継続以上のみ」を選ぶと、同種のエラーが繰り返し発生した場合や深刻な問題のみ通知されます。\n一時的な軽微なエラーはレポートに記録されるだけで、ポップアップは表示されません。",
            });
            loc.Register(P + "alert_level.all", new LocalizedText("All (including minor)")
            {
                ["zh"] = "全部（含轻微）",
                ["ja"] = "すべて（軽微を含む）",
            });
            loc.Register(P + "alert_level.persistent", new LocalizedText("Persistent and above")
            {
                ["zh"] = "仅持续及以上",
                ["ja"] = "継続以上のみ",
            });
            loc.Register(P + "alert_seconds", new LocalizedText("Small alert duration (seconds)")
            {
                ["zh"] = "小提示停留秒数",
                ["ja"] = "小通知の表示秒数",
            });
            loc.Register(P + "alert_seconds.desc", new LocalizedText("How many seconds the small alert in the screen corner stays visible before fading out.")
            {
                ["zh"] = "屏幕角落的小提示显示多少秒后淡出。",
                ["ja"] = "画面隅の小通知が何秒表示された後にフェードアウトするかを設定します。",
            });
            loc.Register(P + "alert_corner", new LocalizedText("Alert position")
            {
                ["zh"] = "提示位置",
                ["ja"] = "通知の位置",
            });
            loc.Register(P + "alert_corner.desc", new LocalizedText("Which corner of the screen the small alert appears in.")
            {
                ["zh"] = "小提示出现在屏幕的哪个角落。",
                ["ja"] = "小通知が画面のどの隅に表示されるかを設定します。",
            });
            loc.Register(P + "corner.top_right", new LocalizedText("Top right")
            {
                ["zh"] = "右上角",
                ["ja"] = "右上",
            });
            loc.Register(P + "corner.top_left", new LocalizedText("Top left")
            {
                ["zh"] = "左上角",
                ["ja"] = "左上",
            });
            loc.Register(P + "corner.bottom_right", new LocalizedText("Bottom right")
            {
                ["zh"] = "右下角",
                ["ja"] = "右下",
            });
            loc.Register(P + "corner.bottom_left", new LocalizedText("Bottom left")
            {
                ["zh"] = "左下角",
                ["ja"] = "左下",
            });
            loc.Register(P + "check_updates", new LocalizedText("Auto-check for updates")
            {
                ["zh"] = "自动检查更新",
                ["ja"] = "自動更新チェック",
            });
            loc.Register(P + "check_updates.desc", new LocalizedText("Checks for a new version after the game starts.\nIf one is available, you are asked first and it downloads only after you agree; files are replaced after you quit the game.")
            {
                ["zh"] = "游戏启动后检查有没有新版本。\n有新版本时会先询问你，同意后才会下载；退出游戏之后才会替换文件。",
                ["ja"] = "ゲーム起動後に新しいバージョンがないか確認します。\n新しいバージョンがある場合はまず確認され、同意した場合のみダウンロードします。ファイルの置き換えはゲーム終了後に行われます。",
            });
            loc.Register(P + "auto_quit", new LocalizedText("Quit the game on fatal errors")
            {
                ["zh"] = "严重错误时自动退出游戏",
                ["ja"] = "重大エラー時にゲームを自動終了",
            });
            loc.Register(P + "auto_quit.desc", new LocalizedText("When a mod problem is severe enough that the game cannot continue, the game quits automatically after a countdown.\nWhen off, you only get an alert and the game keeps running, but more problems may follow.")
            {
                ["zh"] = "模组问题严重到游戏无法继续时，倒计时后自动退出。\n关掉后只会提示，游戏继续运行，但可能出现更多问题。",
                ["ja"] = "Modの問題が深刻でゲームを続行できない場合、カウントダウン後に自動で終了します。\nオフにすると通知のみでゲームは続行しますが、さらなる問題が発生する可能性があります。",
            });
        }
    }
}
