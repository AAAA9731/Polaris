using System.Collections.Generic;
using System.Globalization;

namespace PolarisInstaller;

/// <summary>界面文案：中文系统用中文，日文系统用日文，其余用英文。英/日译文由 dsh (DeepSeek Harness) 生成。</summary>
internal static class Loc
{
    static readonly string Lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    static readonly Dictionary<string, (string Zh, string En, string Ja)> Table = new()
    {
            ["AppTitle"] = ("Polaris 安装器", "Polaris Installer", "Polaris インストーラー"),
            ["Subtitle"] = ("《Alice in Cradle》模组加载器与诊断工具", "Mod loader and diagnostic tool for Alice in Cradle", "『Alice in Cradle』の Mod ローダーと診断ツール"),
            ["GamePath"] = ("游戏目录", "Game folder", "ゲームフォルダ"),
            ["Browse"] = ("浏览…", "Browse…", "参照…"),
            ["AutoDetect"] = ("自动检测", "Auto-detect", "自動検出"),
            ["NotFound"] = ("没有找到游戏。请点“浏览”选择包含 AliceInCradle.exe 的文件夹，或把它拖到这个窗口上。", "No game found. Click “Browse” to select the folder containing AliceInCradle.exe, or drag it onto this window.", "ゲームが見つかりません。「参照」をクリックして AliceInCradle.exe のあるフォルダを選ぶか、このウィンドウにドラッグしてください。"),
            ["InvalidFolder"] = ("这个文件夹里没有 AliceInCradle.exe，不是游戏目录。", "This folder does not contain AliceInCradle.exe, so it is not the game folder.", "このフォルダには AliceInCradle.exe がありません。ゲームフォルダではありません。"),
            ["GameRunning"] = ("游戏正在运行，请先关闭游戏再继续。", "The game is running. Please close it before continuing.", "ゲームが起動中です。終了してから続行してください。"),
            ["TabInstall"] = ("安装", "Install", "インストール"),
            ["TabMods"] = ("模组", "Mods", "Mod"),
            ["TabDiag"] = ("诊断", "Diagnostics", "診断"),
            ["StatusBepInEx"] = ("模组加载器（BepInEx）", "Mod loader (BepInEx)", "Mod ローダー（BepInEx）"),
            ["StatusPolaris"] = ("Polaris", "Polaris", "Polaris"),
            ["NotInstalled"] = ("未安装", "Not installed", "未インストール"),
            ["InstalledVer"] = ("已安装（{0}）", "Installed ({0})", "インストール済み（{0}）"),
            ["UpToDate"] = ("已是最新", "Up to date", "最新です"),
            ["UpdateAvailable"] = ("有可用更新", "Update available", "更新があります"),
            ["BtnInstall"] = ("安装", "Install", "インストール"),
            ["BtnUpdate"] = ("更新", "Update", "更新"),
            ["BtnReinstall"] = ("重新安装", "Reinstall", "再インストール"),
            ["BtnUninstall"] = ("卸载", "Uninstall", "アンインストール"),
            ["ChkRemoveBepInEx"] = ("同时卸载 BepInEx（如果还装有其它模组，请不要勾选）", "Also uninstall BepInEx (leave unchecked if other mods are installed)", "BepInEx もアンインストールする（他の Mod が入っている場合はチェックしないでください）"),
            ["Installing"] = ("正在安装…", "Installing…", "インストール中…"),
            ["Uninstalling"] = ("正在卸载…", "Uninstalling…", "アンインストール中…"),
            ["DoneInstall"] = ("安装完成，现在可以启动游戏了。", "Installation complete. You can start the game now.", "インストールが完了しました。ゲームを起動できます。"),
            ["DoneUninstall"] = ("卸载完成。", "Uninstallation complete.", "アンインストールが完了しました。"),
            ["BackupAt"] = ("被替换的原文件已备份到：{0}", "The replaced original files were backed up to: {0}", "置き換えた元のファイルは次にバックアップされています：{0}"),
            ["NeedAdmin"] = ("没有写入权限（游戏可能装在受保护的位置）。要以管理员身份重新启动安装器吗？", "No write permission (the game may be in a protected location). Restart the installer as administrator?", "書き込み権限がありません（ゲームが保護された場所にある可能性があります）。インストーラーを管理者として再起動しますか？"),
            ["Failed"] = ("操作失败：{0}", "Operation failed: {0}", "操作に失敗しました：{0}"),
            ["ModsHint"] = ("勾选表示启用，改动在下次启动游戏时生效。", "Checked means enabled; changes take effect the next time you start the game.", "チェックが入っていると有効になります。変更は次回ゲーム起動時に反映されます。"),
            ["ModsEmpty"] = ("还没有安装任何模组。把模组的 .dll 放进 BepInEx/plugins，或点下面的按钮添加。", "No mods installed yet. Put the mod's .dll into BepInEx/plugins, or click the button below to add one.", "まだ Mod がインストールされていません。Mod の .dll を BepInEx/plugins に入れるか、下のボタンから追加してください。"),
            ["BtnAddMod"] = ("添加模组…", "Add mod…", "Mod を追加…"),
            ["BtnOpenMods"] = ("打开模组文件夹", "Open mods folder", "Mod フォルダを開く"),
            ["ModLocked"] = ("Polaris 本体，不可禁用", "Polaris itself; cannot be disabled", "Polaris 本体のため無効にできません"),
            ["DiagHint"] = ("游戏异常退出后，Polaris 会弹出窗口说明原因，并保存报告。", "If the game crashes, Polaris shows a window explaining why and saves a report.", "ゲームが異常終了すると、Polaris が原因を説明するウィンドウを表示し、レポートを保存します。"),
            ["BtnOpenReports"] = ("打开报告文件夹", "Open reports folder", "レポートフォルダを開く"),
            ["BtnOpenLog"] = ("打开 BepInEx 日志", "Open BepInEx log", "BepInEx ログを開く"),
            ["NoReports"] = ("还没有报告（没有出现过需要记录的错误）。", "No reports yet (no errors have needed recording).", "レポートはまだありません（記録が必要なエラーは発生していません）。"),
            ["LastReport"] = ("最近一份报告：{0}", "Latest report: {0}", "最新のレポート：{0}"),
            ["BtnOpenGameFolder"] = ("打开游戏文件夹", "Open game folder", "ゲームフォルダを開く"),
            ["Yes"] = ("是", "Yes", "はい"),
            ["No"] = ("否", "No", "いいえ"),
    };

    internal static string T(string key, params object[] args)
    {
        if (!Table.TryGetValue(key, out var entry))
        {
            return key;
        }

        string text = Lang == "zh" ? entry.Zh : Lang == "ja" ? entry.Ja : entry.En;
        return args.Length == 0 ? text : string.Format(text, args);
    }
}
