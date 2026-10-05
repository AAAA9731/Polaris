using System.ComponentModel;
using System.IO;

namespace PolarisInstaller;

/// <summary>模组列表里的一项：一个 plugins 下的 dll，启用 = <c>.dll</c>，禁用 = <c>.dll.disabled</c>。</summary>
internal sealed class ModItem : INotifyPropertyChanged
{
    const string Disabled = ".disabled";

    bool enabled;
    string error;

    public event PropertyChangedEventHandler PropertyChanged;

    public string Name { get; init; }

    /// <summary>相对 plugins 的所在文件夹；放在根目录时为空。</summary>
    public string Folder { get; init; }

    internal string DllPath { get; init; }

    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
        }
    }

    public string Error
    {
        get => error;
        set
        {
            error = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Error)));
        }
    }

    /// <summary>改名切换启停，下次启动游戏生效。失败时返回原因。</summary>
    internal string SetEnabled(bool value)
    {
        string on = DllPath;
        string off = DllPath + Disabled;

        try
        {
            if (value && File.Exists(off))
            {
                File.Move(off, on, overwrite: true);
            }
            else if (!value && File.Exists(on))
            {
                File.Move(on, off, overwrite: true);
            }

            Enabled = value;
            Error = null;
            return null;
        }
        catch (Exception e)
        {
            Error = e.Message;
            return e.Message;
        }
    }
}

internal static class ModScanner
{
    const string Disabled = ".disabled";

    /// <summary>扫描 plugins 的根目录和一层子目录；Polaris 自己的文件不列出（不允许被禁用）。</summary>
    internal static List<ModItem> Scan(string game)
    {
        var result = new List<ModItem>();
        string root = InstallEngine.PluginsDir(game);
        if (!Directory.Exists(root))
        {
            return result;
        }

        var dirs = new List<string> { root };
        try
        {
            dirs.AddRange(Directory.GetDirectories(root).Where(d => !string.Equals(Path.GetFileName(d), "Polaris", StringComparison.OrdinalIgnoreCase)));
        }
        catch (Exception)
        {
        }

        foreach (string dir in dirs)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(dir, "*.dll*");
            }
            catch (Exception)
            {
                continue;
            }

            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                bool off = name.EndsWith(".dll" + Disabled, StringComparison.OrdinalIgnoreCase);
                bool on = !off && name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
                if (!on && !off)
                {
                    continue;
                }

                string dll = off ? file.Substring(0, file.Length - Disabled.Length) : file;
                string dllName = Path.GetFileName(dll);

                if (dir == root && string.Equals(dllName, "PolarisCore.dll", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(new ModItem
                {
                    Name = Path.GetFileNameWithoutExtension(dllName),
                    Folder = dir == root ? "" : Path.GetFileName(dir),
                    DllPath = dll,
                    Enabled = on,
                });
            }
        }

        return result.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
