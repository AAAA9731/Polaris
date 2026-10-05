using System.IO;
using System.Windows;

namespace PolarisInstaller;

public partial class App : Application
{
    /// <summary>
    /// 无界面模式，给脚本/自动化用：<c>PolarisInstaller.exe --silent install|uninstall --game "路径" [--remove-bepinex]</c>。
    /// 结果写进安装器旁边的 <c>polaris-installer.log</c>，退出码 0 = 成功。
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string[] args = e.Args;
        int silent = Array.IndexOf(args, "--silent");
        if (silent < 0)
        {
            new MainWindow().Show();
            return;
        }

        Shutdown(RunSilent(args, silent));
    }

    static int RunSilent(string[] args, int silentAt)
    {
        string log = Path.Combine(AppContext.BaseDirectory, "polaris-installer.log");
        void Write(string line) => File.AppendAllText(log, line + Environment.NewLine);

        try
        {
            string action = silentAt + 1 < args.Length ? args[silentAt + 1] : "";
            int gameAt = Array.IndexOf(args, "--game");
            string game = gameAt >= 0 && gameAt + 1 < args.Length ? GameLocator.Normalize(args[gameAt + 1]) : GameLocator.Detect();
            if (game == null)
            {
                Write("game folder not found");
                return 2;
            }

            switch (action)
            {
                case "install":
                    string backup = InstallEngine.Install(game, reinstallBepInEx: false, Write);
                    Write("installed" + (backup != null ? ", backup: " + backup : ""));
                    return 0;

                case "uninstall":
                    InstallEngine.Uninstall(game, removeBepInEx: Array.IndexOf(args, "--remove-bepinex") >= 0, Write);
                    Write("uninstalled");
                    return 0;

                default:
                    Write("unknown action: " + action);
                    return 3;
            }
        }
        catch (Exception ex)
        {
            Write("failed: " + ex);
            return 1;
        }
    }
}
