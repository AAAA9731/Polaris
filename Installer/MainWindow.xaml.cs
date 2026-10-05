using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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

        // 管理员重启时带着游戏目录回来，不用玩家再选一遍。
        string[] args = Environment.GetCommandLineArgs();
        int at = Array.IndexOf(args, "--game");
        string start = at >= 0 && at + 1 < args.Length ? GameLocator.Normalize(args[at + 1]) : GameLocator.Detect();
        SetGame(start, showNotFound: start == null);
    }

    void ApplyText()
    {
        Title = Loc.T("AppTitle");
        TitleText.Text = Loc.T("AppTitle");
        SubtitleText.Text = Loc.T("Subtitle");
        GamePathLabel.Text = Loc.T("GamePath");
        BrowseButton.Content = Loc.T("Browse");
        DetectButton.Content = Loc.T("AutoDetect");
        TabInstall.Header = Loc.T("TabInstall");
        TabMods.Header = Loc.T("TabMods");
        TabDiag.Header = Loc.T("TabDiag");
        BepLabel.Text = Loc.T("StatusBepInEx");
        PolLabel.Text = Loc.T("StatusPolaris");
        UninstallButton.Content = Loc.T("BtnUninstall");
        RemoveBepCheck.Content = Loc.T("ChkRemoveBepInEx");
        ModsHint.Text = Loc.T("ModsHint");
        ModsEmpty.Text = Loc.T("ModsEmpty");
        AddModButton.Content = Loc.T("BtnAddMod");
        OpenModsButton.Content = Loc.T("BtnOpenMods");
        DiagHint.Text = Loc.T("DiagHint");
        OpenReportsButton.Content = Loc.T("BtnOpenReports");
        OpenLogButton.Content = Loc.T("BtnOpenLog");
        OpenGameButton.Content = Loc.T("BtnOpenGameFolder");
    }

    // ================== 游戏目录 ==================

    void SetGame(string path, bool showNotFound = false, string message = null)
    {
        game = path;
        PathBox.Text = path ?? "";
        PathMessage.Text = message ?? (showNotFound ? Loc.T("NotFound") : "");
        Refresh();
    }

    void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Loc.T("GamePath") };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        string found = GameLocator.Normalize(dialog.FolderName);
        SetGame(found ?? game, message: found == null ? Loc.T("InvalidFolder") : null);
    }

    void OnDetect(object sender, RoutedEventArgs e)
    {
        string found = GameLocator.Detect();
        SetGame(found ?? game, showNotFound: found == null);
    }

    void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] items || items.Length == 0)
        {
            return;
        }

        string path = items[0];

        // 拖进来的是 dll 且已经选好游戏：当作要添加的模组。
        if (game != null && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            AddMods(items.Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)));
            Tabs.SelectedItem = TabMods;
            return;
        }

        string found = GameLocator.Normalize(path);
        SetGame(found ?? game, message: found == null ? Loc.T("InvalidFolder") : null);
    }

    // ================== 状态刷新 ==================

    void Refresh()
    {
        bool has = game != null;
        InstallButton.IsEnabled = has && !busy;
        UninstallButton.IsEnabled = false;
        AddModButton.IsEnabled = has;
        OpenModsButton.IsEnabled = has;
        OpenReportsButton.IsEnabled = has;
        OpenLogButton.IsEnabled = has;
        OpenGameButton.IsEnabled = has;

        if (!has)
        {
            BepStatus.Text = PolStatus.Text = Loc.T("NotInstalled");
            BepStatus.Foreground = PolStatus.Foreground = (Brush)FindResource("InkSoft");
            InstallButton.Content = Loc.T("BtnInstall");
            RemoveBepCheck.Visibility = Visibility.Collapsed;
            ModList.ItemsSource = null;
            ModsEmpty.Visibility = Visibility.Visible;
            LastReportText.Text = "";
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

        RefreshMods();

        string report = InstallEngine.LatestReport(game);
        LastReportText.Text = report == null ? Loc.T("NoReports") : Loc.T("LastReport", Path.GetFileName(report));
    }

    void RefreshMods()
    {
        List<ModItem> mods = game == null ? new List<ModItem>() : ModScanner.Scan(game);
        ModList.ItemsSource = mods;
        ModsEmpty.Visibility = mods.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
            // 游戏装在受保护目录（如 Program Files）：问一下，然后以管理员身份重新启动。
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

    // ================== 模组 ==================

    void OnModToggle(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox box && box.DataContext is ModItem mod)
        {
            if (InstallEngine.IsGameRunning())
            {
                // 游戏在跑时 dll 被占用，改名会失败；提示并还原勾选状态。
                mod.Error = Loc.T("GameRunning");
                box.IsChecked = mod.Enabled;
                return;
            }

            mod.SetEnabled(box.IsChecked == true);
            box.IsChecked = mod.Enabled;
        }
    }

    void OnAddMod(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Mod (*.dll)|*.dll", Multiselect = true };
        if (dialog.ShowDialog(this) == true)
        {
            AddMods(dialog.FileNames);
        }
    }

    void AddMods(IEnumerable<string> files)
    {
        if (game == null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(InstallEngine.PluginsDir(game));
            foreach (string file in files)
            {
                File.Copy(file, Path.Combine(InstallEngine.PluginsDir(game), Path.GetFileName(file)), overwrite: true);
            }
        }
        catch (UnauthorizedAccessException)
        {
            if (MessageBox.Show(this, Loc.T("NeedAdmin"), Loc.T("AppTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RelaunchAsAdmin();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, Loc.T("Failed", ex.Message), Loc.T("AppTitle"));
        }

        RefreshMods();
    }

    void OnOpenMods(object sender, RoutedEventArgs e) => OpenPath(game == null ? null : InstallEngine.PluginsDir(game), createIfMissing: true);

    // ================== 诊断 ==================

    void OnOpenReports(object sender, RoutedEventArgs e) => OpenPath(game == null ? null : InstallEngine.ReportsDir(game), createIfMissing: true);

    void OnOpenLog(object sender, RoutedEventArgs e)
    {
        if (game != null)
        {
            OpenPath(Path.Combine(game, "BepInEx", "LogOutput.log"));
        }
    }

    void OnOpenGame(object sender, RoutedEventArgs e) => OpenPath(game);

    static void OpenPath(string path, bool createIfMissing = false)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            if (createIfMissing)
            {
                Directory.CreateDirectory(path);
            }

            if (File.Exists(path) || Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
        }
        catch (Exception)
        {
        }
    }
}
