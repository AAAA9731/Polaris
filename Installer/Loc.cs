using System.Collections.Generic;
using System.Globalization;

namespace PolarisInstaller;

/// <summary>界面文案：中文系统用中文，日文系统用日文，其余用英文。英/日译文由 dsh (DeepSeek Harness) 生成。</summary>
internal static class Loc
{
    static readonly string Lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    static readonly Dictionary<string, (string Zh, string En, string Ja)> Table = new()
    {
            ["PickGame"] = ("请点“浏览…”，选择游戏的 AliceInCradle.exe，或把它（或游戏文件夹）拖到这个窗口上。", "Click “Browse…” and select the game's AliceInCradle.exe, or drag it (or the game folder) onto this window.", "「参照…」をクリックしてゲームの AliceInCradle.exe を選択するか、それ（またはゲームフォルダ）をこのウィンドウにドラッグしてください。"),
            ["PickGameTitle"] = ("选择游戏的 AliceInCradle.exe", "Select the game's AliceInCradle.exe", "ゲームの AliceInCradle.exe を選択"),
            ["AppTitle"] = ("Polaris 安装器", "Polaris Installer", "Polaris インストーラー"),
            ["Subtitle"] = ("《Alice in Cradle》模组加载器与诊断工具", "Mod loader and diagnostic tool for Alice in Cradle", "『Alice in Cradle』の Mod ローダーと診断ツール"),
            ["GamePath"] = ("游戏目录", "Game folder", "ゲームフォルダ"),
            ["Browse"] = ("浏览…", "Browse…", "参照…"),
            ["InvalidFolder"] = ("这个文件夹里没有 AliceInCradle.exe，不是游戏目录。", "This folder does not contain AliceInCradle.exe, so it is not the game folder.", "このフォルダには AliceInCradle.exe がありません。ゲームフォルダではありません。"),
            ["GameRunning"] = ("游戏正在运行，请先关闭游戏再继续。", "The game is running. Please close it before continuing.", "ゲームが起動中です。終了してから続行してください。"),
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
