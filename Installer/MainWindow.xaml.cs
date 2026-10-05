using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace PolarisInstaller;

public partial class MainWindow : Window
{
    string game;
    bool busy;

    public MainWindow()
    {
        InitializeComponent();
        ApplyText();

        // 管理员重启时带着游戏目录回来；否则只看安装器自己所在的目录，不做任何搜索。
        string[] args = Environment.GetCommandLineArgs();
        int at = Array.IndexOf(args, "--game");
        string start = at >= 0 && at + 1 < args.Length ? GameLocator.Normalize(args[at + 1]) : GameLocator.FromOwnFolder();
        SetGame(start, showHint: start == null);
    }

    void ApplyText()
    {
        Title = Loc.T("AppTitle");
        TitleText.Text = Loc.T("AppTitle");
        SubtitleText.Text = Loc.T("Subtitle");
        GamePathLabel.Text = Loc.T("GamePath");
        BrowseButton.Content = Loc.T("Browse");
        BepLabel.Text = Loc.T("StatusBepInEx");
        PolLabel.Text = Loc.T("StatusPolaris");
        UninstallButton.Content = Loc.T("BtnUninstall");
        RemoveBepCheck.Content = Loc.T("ChkRemoveBepInEx");
    }

    // ================== 游戏目录 ==================

    void SetGame(string path, bool showHint = false, string message = null)
    {
        game = path;
        PathBox.Text = path ?? "";
        PathMessage.Text = message ?? (showHint ? Loc.T("PickGame") : "");
        Refresh();
    }

    void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.T("PickGameTitle"),
            Filter = "AliceInCradle.exe|AliceInCradle.exe",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        string found = GameLocator.Normalize(dialog.FileName);
        SetGame(found ?? game, message: found == null ? Loc.T("InvalidFolder") : null);
    }

    void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] items || items.Length == 0)
        {
            return;
        }

        string found = GameLocator.Normalize(items[0]);
        SetGame(found ?? game, message: found == null ? Loc.T("InvalidFolder") : null);
    }

    // ================== 状态刷新 ==================

    void Refresh()
    {
        bool has = game != null;
        InstallButton.IsEnabled = has && !busy;
        UninstallButton.IsEnabled = false;

        if (!has)
        {
            BepStatus.Text = PolStatus.Text = Loc.T("NotInstalled");
            BepStatus.Foreground = PolStatus.Foreground = (Brush)FindResource("InkSoft");
            InstallButton.Content = Loc.T("BtnInstall");
            RemoveBepCheck.Visibility = Visibility.Collapsed;
            return;
        }

        InstallState state = InstallEngine.GetState(game);

        BepStatus.Text = state.BepInExInstalled ? Loc.T("InstalledVer", state.BepInExVersion ?? "") : Loc.T("NotInstalled");
        BepStatus.Foreground = (Brush)FindResource(state.BepInExInstalled ? "Ok" : "InkSoft");

        if (!state.PolarisInstalled)
        {
            PolStatus.Text = Loc.T("NotInstalled");
            PolStatus.Foreground = (Brush)FindResource("InkSoft");
            InstallButton.Content = Loc.T("BtnInstall");
        }
        else if (state.PolarisUpToDate)
        {
            PolStatus.Text = Loc.T("UpToDate");
            PolStatus.Foreground = (Brush)FindResource("Ok");
            InstallButton.Content = Loc.T("BtnReinstall");
        }
        else
        {
            PolStatus.Text = Loc.T("UpdateAvailable");
            PolStatus.Foreground = (Brush)FindResource("Accent");
            InstallButton.Content = Loc.T("BtnUpdate");
        }

        UninstallButton.IsEnabled = !busy && (state.PolarisInstalled || state.BepInExInstalled);
        RemoveBepCheck.Visibility = state.BepInExInstalled ? Visibility.Visible : Visibility.Collapsed;
    }

    // ================== 安装 / 卸载 ==================

    async void OnInstall(object sender, RoutedEventArgs e)
    {
        if (game == null)
        {
            return;
        }

        InstallState before = InstallEngine.GetState(game);

        // 点的是“重新安装”（Polaris 已是最新）时，连 BepInEx 一并重装；改动过的原文件会先备份。
        bool reinstallBep = before.PolarisInstalled && before.PolarisUpToDate;

        await RunAsync(Loc.T("Installing"), () => InstallEngine.Install(game, reinstallBep, Log), backup =>
        {
            Log(Loc.T("DoneInstall"));
            if (backup != null)
            {
                Log(Loc.T("BackupAt", backup));
            }
        });
    }

    async void OnUninstall(object sender, RoutedEventArgs e)
    {
        if (game == null)
        {
            return;
        }

        bool removeBep = RemoveBepCheck.IsChecked == true;
        await RunAsync(Loc.T("Uninstalling"), () =>
        {
            InstallEngine.Uninstall(game, removeBep, Log);
            return null;
        }, _ => Log(Loc.T("DoneUninstall")));
    }

    async Task RunAsync(string title, Func<string> work, Action<string> done)
    {
        if (busy)
        {
            return;
        }

        busy = true;
        LogBox.Clear();
        Log(title);
        Progress.Visibility = Visibility.Visible;
        Progress.IsIndeterminate = true;
        InstallButton.IsEnabled = UninstallButton.IsEnabled = false;

        try
        {
            string result = await Task.Run(work);
            done(result);
        }
        catch (UnauthorizedAccessException)
        {
            // 游戏装在受保护目录：问一下，然后以管理员身份重新启动。
            if (MessageBox.Show(this, Loc.T("NeedAdmin"), Loc.T("AppTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RelaunchAsAdmin();
            }
        }
        catch (Exception ex)
        {
            Log(Loc.T("Failed", ex.Message));
        }
        finally
        {
            busy = false;
            Progress.Visibility = Visibility.Collapsed;
            Progress.IsIndeterminate = false;
            Refresh();
        }
    }

    void RelaunchAsAdmin()
    {
        try
        {
            string exe = Environment.ProcessPath;
            Process.Start(new ProcessStartInfo(exe, $"--game \"{game}\"") { UseShellExecute = true, Verb = "runas" });
            Application.Current.Shutdown();
        }
        catch (Exception)
        {
            // 玩家在 UAC 里点了"否"：保持现状。
        }
    }

    void Log(string line)
    {
        // 安装在后台线程跑，日志要回到界面线程。
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => Log(line));
            return;
        }

        LogBox.AppendText(line + Environment.NewLine);
        LogBox.ScrollToEnd();
    }
}
