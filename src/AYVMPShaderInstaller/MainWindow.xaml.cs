using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using Forms = System.Windows.Forms;

namespace AYVMPShaderInstaller;

public partial class MainWindow : Window
{
    private readonly string _appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AYVMPShaderInstaller");
    private string? _detectedPath;

    public MainWindow()
    {
        InitializeComponent();
        Directory.CreateDirectory(_appData);
        LoadPacks();
        DetectPath(false);
    }

    private void LoadPacks()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "ShaderPacks");
        Directory.CreateDirectory(root);
        var packs = Directory.GetDirectories(root).Select(Path.GetFileName).Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
        PackBox.ItemsSource = packs;
        if (packs.Count > 0) PackBox.SelectedIndex = 0;
    }

    private void DetectPath(bool showMessage = true)
    {
        var candidates = new List<string>();
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        candidates.AddRange(new[] { Path.Combine(pf, "VMP"), Path.Combine(pf, "VMP Launcher"), Path.Combine(pfx86, "VMP"), Path.Combine(pfx86, "VMP Launcher") });
        candidates.AddRange(DriveInfo.GetDrives().Where(d => d.IsReady).SelectMany(d => new[] { Path.Combine(d.RootDirectory.FullName, "VMP"), Path.Combine(d.RootDirectory.FullName, "Games", "VMP") }));

        var hit = candidates.FirstOrDefault(Directory.Exists);
        if (hit is not null)
        {
            _detectedPath = hit;
            PathBox.Text = hit;
            VmpStatus.Text = "Detected";
            VmpStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            GameStatus.Text = LooksLikeGameRoot(hit) ? "Detected" : "Check path";
        }
        else
        {
            VmpStatus.Text = "Not found";
            VmpStatus.Foreground = System.Windows.Media.Brushes.Orange;
            if (showMessage) StatusText.Text = "VMP was not found automatically. Browse to the correct game/VMP directory.";
        }
    }

    private static bool LooksLikeGameRoot(string path) => File.Exists(Path.Combine(path, "GTA5.exe")) || Directory.Exists(Path.Combine(path, "update"));

    private void Detect_Click(object sender, RoutedEventArgs e) => DetectPath();

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog { Description = "Select your VMP / GTA V directory" };
        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            PathBox.Text = dialog.SelectedPath;
            _detectedPath = dialog.SelectedPath;
            GameStatus.Text = LooksLikeGameRoot(dialog.SelectedPath) ? "Detected" : "Custom";
            VmpStatus.Text = "Selected";
            StatusText.Text = "Directory selected. Nothing has been changed yet.";
        }
    }

    private void Install_Click(object sender, RoutedEventArgs e)
    {
        var target = PathBox.Text.Trim();
        var pack = PackBox.SelectedItem?.ToString();
        if (!Directory.Exists(target)) { StatusText.Text = "Invalid directory."; return; }
        if (string.IsNullOrWhiteSpace(pack)) { StatusText.Text = "Select a shader pack first."; return; }
        var source = Path.Combine(AppContext.BaseDirectory, "ShaderPacks", pack);
        if (!Directory.Exists(source)) { StatusText.Text = "Shader pack folder was not found."; return; }
        try
        {
            var backupRoot = Path.Combine(_appData, "Backups", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(backupRoot);
            var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
            var copied = 0;
            foreach (var file in files)
            {
                var relative = Path.GetRelativePath(source, file);
                if (relative.Equals("pack.json", StringComparison.OrdinalIgnoreCase) || relative.Equals("README.txt", StringComparison.OrdinalIgnoreCase)) continue;
                var dest = Path.Combine(target, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                if (File.Exists(dest))
                {
                    var backup = Path.Combine(backupRoot, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                    File.Copy(dest, backup, true);
                }
                File.Copy(file, dest, true);
                copied++;
            }
            BackupStatus.Text = copied == 0 ? "No files" : "Created";
            StatusText.Text = $"Installed {copied} file(s) from '{pack}'. Backup: {backupRoot}";
        }
        catch (Exception ex) { StatusText.Text = "Install failed: " + ex.Message; }
    }

    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(_appData, "Backups");
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
    }
}
