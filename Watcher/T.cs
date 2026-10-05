using System.Globalization;

namespace Polaris.Watcher
{
    /// <summary>界面文案：中文系统用中文，日文系统用日文，其余用英文。英/日译文由 dsh (DeepSeek Harness) 生成。</summary>
    internal static class T
    {
        static readonly string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        static string P(string zh, string en, string ja) => lang == "zh" ? zh : lang == "ja" ? ja : en;

        internal static string Title => P("游戏异常退出", "Abnormal Game Exit", "ゲームが異常終了しました");
        internal static string Terminated => P("Polaris 因严重错误结束了游戏", "Polaris ended the game because of a critical error", "Polaris が重大なエラーのためゲームを終了しました");
        internal static string Responsible(string who) => P("责任方：" + who, "Responsible: " + who, "原因となったもの：" + who);
        internal static string Hung => P("游戏卡死后被结束", "The game froze and was then terminated", "ゲームがフリーズした後、強制終了されました");
        internal static string Oom => P("游戏因内存不足而崩溃", "The game crashed due to running out of memory", "メモリ不足によりゲームがクラッシュしました");
        internal static string StackOverflow => P("游戏因栈溢出（无限递归）而崩溃", "The game crashed due to a stack overflow (infinite recursion)", "スタックオーバーフロー（無限再帰）によりゲームがクラッシュしました");
        internal static string NativeCrash => P("游戏发生了原生崩溃", "The game suffered a native crash", "ゲームでネイティブクラッシュが発生しました");
        internal static string Vanished => P("游戏进程在没有正常退出的情况下消失了", "The game process disappeared without exiting normally", "ゲームプロセスが正常に終了することなく消滅しました");

        internal static string OomDetail => P(
            "日志里出现了内存分配失败。关闭其它占内存的程序，或减少同时启用的模组/高清资源。",
            "The log shows memory allocation failures. Close other memory-hungry programs, or reduce the number of mods/HD assets enabled at the same time.",
            "ログにメモリ割り当ての失敗が記録されています。他のメモリを消費するプログラムを閉じるか、同時に有効にするMod／高解像度アセットを減らしてください。");

        internal static string StackOverflowDetail => P(
            "通常是某段代码无限递归。若刚装了新模组，先禁用它再试。",
            "This is usually caused by infinite recursion in some code. If you just installed a new mod, try disabling it first.",
            "通常はどこかのコードが無限再帰しています。新しいModを入れた直後なら、まずそれを無効にして試してください。");

        internal static string EarlierErrors => P(
            "崩溃前游戏已记录过这些错误（可能是根因的早期症状）：",
            "The game had already logged these errors before the crash (possibly early symptoms of the root cause):",
            "クラッシュ前にゲームがこれらのエラーを記録していました（根本原因の初期症状の可能性があります）：");

        internal static string HungDetail(string seconds, string activity)
        {
            string s = seconds ?? "?";
            return P(
                "主线程停止响应约 " + s + " 秒" + (activity != null ? "，卡在：" + activity : "") + "。",
                "The main thread stopped responding for about " + s + " seconds" + (activity != null ? ", stuck at: " + activity : "") + ".",
                "メインスレッドが約 " + s + " 秒間応答しなくなりました" + (activity != null ? "。停止箇所：" + activity : "") + "。");
        }

        internal static string ExitCodeCause(string name) => P("进程退出码：" + name + "。", "Process exit code: " + name + ".", "プロセスの終了コード：" + name + "。");

        internal static string VanishedDetail(int code) => P(
            "退出码 " + code + "，没有崩溃记录。常见原因：被任务管理器/Steam 结束、被杀毒软件拦截、断电或系统强制关闭。若不是你主动关闭的，请重试并留意是否可复现。",
            "Exit code " + code + ", with no crash record. Common causes: terminated by Task Manager/Steam, blocked by antivirus software, power loss, or a forced system shutdown. If you did not close it yourself, please try again and see whether it reproduces.",
            "終了コード " + code + "、クラッシュ記録はありません。よくある原因：タスクマネージャー／Steam による終了、ウイルス対策ソフトによるブロック、停電、システムによる強制終了。ご自身で閉じた覚えがない場合は、もう一度試して再現するか確認してください。");

        internal static string BlameMod(string mod) => P(
            "崩溃现场涉及模组文件：" + mod + "。最可疑，先禁用它再试。",
            "The crash involves a mod file: " + mod + ". It is the most suspicious, so try disabling it first.",
            "クラッシュに関与したModファイル：" + mod + "。最も疑わしいため、まずこれを無効にして試してください。");

        internal static string FaultModuleCause(string module) => P(
            "故障模块：" + module + "（不是模组，属于游戏引擎或系统组件）。",
            "Faulting module: " + module + " (not a mod; it belongs to the game engine or a system component).",
            "障害が発生したモジュール：" + module + "（Modではなく、ゲームエンジンまたはシステムコンポーネントに属します）。");

        internal static string Basics => P("基本信息", "Basic information", "基本情報");
        internal static string NativeStack => P("崩溃栈（来自 Unity Player.log）：", "Crash stack (from Unity Player.log):", "クラッシュスタック（Unity Player.log より）：");
        internal static string BepErrors => P("BepInEx 日志中最近的错误：", "Most recent errors in the BepInEx log:", "BepInEx ログの直近のエラー：");
        internal static string Line(string key, string value) => value == null ? null : key + ": " + value;

        internal static string OpenReport => P("打开报告", "Open report", "レポートを開く");
        internal static string OpenLogs => P("打开日志目录", "Open log folder", "ログフォルダを開く");
        internal static string Copy => P("复制全部", "Copy all", "すべてコピー");
        internal static string Close => P("关闭", "Close", "閉じる");
    }
}
