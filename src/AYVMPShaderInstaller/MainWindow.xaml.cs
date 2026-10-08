using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;
using Wm = System.Windows.Media;

namespace AYVMPShaderInstaller;

public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AYVMPShaderInstaller");
    private readonly string _packsRoot;

    private string BackupsRoot => Path.Combine(_appData, "Backups");

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public MainWindow()
    {
        InitializeComponent();
        Directory.CreateDirectory(BackupsRoot);
        _packsRoot = ResolvePacksRoot();
        LoadPacks();
        RefreshBackups();
        DetectPath(false);
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var enabled = 1;
            DwmSetWindowAttribute(hwnd, 20, ref enabled, sizeof(int));
        }
        catch { }
    }

    // ---------- Helpers ----------

    private static string ResolvePacksRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "ShaderPacks");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        var fallback = Path.Combine(AppContext.BaseDirectory, "ShaderPacks");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static IEnumerable<string> GetPackFiles(string source) =>
        Directory.GetFiles(source, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(source, f))
            .Where(r => !r.Equals("pack.json", StringComparison.OrdinalIgnoreCase)
                     && !r.Equals("README.txt", StringComparison.OrdinalIgnoreCase));

    private static string SafeCombine(string root, string relative)
    {
        var rootFull = Path.GetFullPath(root);
        if (!rootFull.EndsWith(Path.DirectorySeparatorChar)) rootFull += Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(root, relative));
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid path: " + relative);
        return full;
    }

    private static bool LooksLikeGameRoot(string path) =>
        File.Exists(Path.Combine(path, "GTA5.exe")) || Directory.Exists(Path.Combine(path, "update"));

    private void SetField(System.Windows.Controls.TextBlock block, string text, string brushKey)
    {
        block.Text = text;
        block.Foreground = (Wm.Brush)FindResource(brushKey);
    }

    private void SetMessage(string text, bool isError = false) =>
        SetField(StatusText, text, isError ? "Danger" : "Text");

    private void SetBusy(bool busy)
    {
        Body.IsEnabled = !busy;
        BusyBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true });
    }

    // ---------- Packs ----------

    private void LoadPacks()
    {
        var packs = Directory.GetDirectories(_packsRoot)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .Select(ReadPack)
            .ToList();

        PackList.ItemsSource = packs;
        if (packs.Count > 0) PackList.SelectedIndex = 0;
        EmptyPacks.Visibility = packs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        PackCountText.Text = packs.Count.ToString();
    }

    private static PackItem ReadPack(string dir)
    {
        var folder = Path.GetFileName(dir);
        var name = folder;
        var version = "";
        var description = "No description provided.";

        var json = Path.Combine(dir, "pack.json");
        if (File.Exists(json))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(json));
                name = ReadString(doc.RootElement, "name") ?? name;
                version = ReadString(doc.RootElement, "version") ?? version;
                description = ReadString(doc.RootElement, "description") ?? description;
            }
            catch (JsonException) { }
        }

        return new PackItem(folder, name, version, description, GetPackFiles(dir).Count());
    }

    private static string? ReadString(JsonElement root, string key) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private void OpenPacks_Click(object sender, RoutedEventArgs e) => OpenFolder(_packsRoot);

    // ---------- Directory ----------

    private void DetectPath(bool showMessage)
    {
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var candidates = new List<string>
        {
            Path.Combine(pf, "VMP"), Path.Combine(pf, "VMP Launcher"),
            Path.Combine(pfx86, "VMP"), Path.Combine(pfx86, "VMP Launcher")
        };
        candidates.AddRange(DriveInfo.GetDrives().Where(d => d.IsReady).SelectMany(d => new[]
        {
            Path.Combine(d.RootDirectory.FullName, "VMP"),
            Path.Combine(d.RootDirectory.FullName, "Games", "VMP")
        }));

        var hit = candidates.FirstOrDefault(Directory.Exists);
        if (hit is not null)
        {
            PathBox.Text = hit;
            UpdateDirectoryStatus();
            SetMessage("VMP directory detected.");
        }
        else
        {
            UpdateDirectoryStatus();
            if (showMessage) SetMessage("VMP was not found automatically. Use Browse to select the directory.", true);
        }
    }

    private void UpdateDirectoryStatus()
    {
        var path = PathBox.Text.Trim();
        if (path.Length == 0)
        {
            SetField(DirStatus, "Not set", "Muted");
            SetField(GameStatus, "-", "Muted");
        }
        else if (!Directory.Exists(path))
        {
            SetField(DirStatus, "Not found", "Danger");
            SetField(GameStatus, "-", "Muted");
        }
        else
        {
            SetField(DirStatus, "Ready", "Accent");
            if (LooksLikeGameRoot(path)) SetField(GameStatus, "Found", "Accent");
            else SetField(GameStatus, "Not detected", "Warn");
        }
    }

    private void PathBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateDirectoryStatus();

    private void Detect_Click(object sender, RoutedEventArgs e) => DetectPath(true);

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Select your VMP / GTA V directory",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (Directory.Exists(PathBox.Text.Trim())) dialog.InitialDirectory = PathBox.Text.Trim();
        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            PathBox.Text = dialog.SelectedPath;
            SetMessage("Directory selected. Nothing has been changed yet.");
        }
    }

    private bool TryGetSelection(out string target, out PackItem pack)
    {
        target = PathBox.Text.Trim();
        pack = null!;
        if (!Directory.Exists(target))
        {
            SetMessage("Select a valid game directory first.", true);
            return false;
        }
        if (PackList.SelectedItem is not PackItem selected)
        {
            SetMessage("Select a shader pack first.", true);
            return false;
        }
        if (selected.FileCount == 0)
        {
            SetMessage("This pack contains no files to install.", true);
            return false;
        }
        pack = selected;
        return true;
    }

    // ---------- Backup / Install / Restore ----------

    private (string Folder, BackupManifest Manifest) CreateBackupCore(string target, string packFolder, string kind)
    {
        var source = Path.Combine(_packsRoot, packFolder);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var folder = Path.Combine(BackupsRoot, stamp);
        for (var i = 1; Directory.Exists(folder); i++) folder = Path.Combine(BackupsRoot, stamp + "-" + i);

        var filesDir = Path.Combine(folder, "files");
        Directory.CreateDirectory(filesDir);

        var manifest = new BackupManifest
        {
            CreatedAt = DateTime.Now,
            Target = target,
            Pack = packFolder,
            Kind = kind
        };

        foreach (var relative in GetPackFiles(source))
        {
            var dest = SafeCombine(target, relative);
            var existed = File.Exists(dest);
            if (existed)
            {
                var saved = SafeCombine(filesDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(saved)!);
                File.Copy(dest, saved, true);
            }
            manifest.Files.Add(new BackupFileEntry { Path = relative, Existed = existed });
        }

        File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions));
        return (folder, manifest);
    }

    private int CopyPack(string target, string packFolder)
    {
        var source = Path.Combine(_packsRoot, packFolder);
        var count = 0;
        foreach (var relative in GetPackFiles(source))
        {
            var dest = SafeCombine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(Path.Combine(source, relative), dest, true);
            count++;
        }
        return count;
    }

    private static (int Restored, int Removed) RestoreCore(BackupItem item)
    {
        var manifest = item.Manifest;
        var filesDir = Path.Combine(item.Folder, "files");
        var restored = 0;
        var removed = 0;

        foreach (var entry in manifest.Files)
        {
            var dest = SafeCombine(manifest.Target, entry.Path);
            if (entry.Existed)
            {
                var saved = SafeCombine(filesDir, entry.Path);
                if (!File.Exists(saved)) throw new FileNotFoundException("Backup file is missing: " + entry.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(saved, dest, true);
                restored++;
            }
            else if (File.Exists(dest))
            {
                File.Delete(dest);
                removed++;
            }
        }
        return (restored, removed);
    }

    private void RefreshBackups()
    {
        var items = new List<BackupItem>();
        if (Directory.Exists(BackupsRoot))
        {
            foreach (var dir in Directory.GetDirectories(BackupsRoot))
            {
                var file = Path.Combine(dir, "manifest.json");
                if (!File.Exists(file)) continue;
                try
                {
                    var manifest = JsonSerializer.Deserialize<BackupManifest>(File.ReadAllText(file));
                    if (manifest is not null) items.Add(new BackupItem(dir, manifest));
                }
                catch (JsonException) { }
            }
        }

        items = items.OrderByDescending(i => i.Manifest.CreatedAt).ToList();
        BackupList.ItemsSource = items;
        EmptyBackups.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        BackupCountText.Text = items.Count.ToString();
    }

    private async void CreateBackup_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelection(out var target, out var pack)) return;
        SetBusy(true);
        try
        {
            var result = await Task.Run(() => CreateBackupCore(target, pack.Folder, "manual"));
            var saved = result.Manifest.Files.Count(f => f.Existed);
            SetMessage($"Backup created for '{pack.Name}': {saved} existing file(s) saved.");
        }
        catch (Exception ex) { SetMessage("Backup failed: " + ex.Message, true); }
        finally
        {
            SetBusy(false);
            RefreshBackups();
        }
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelection(out var target, out var pack)) return;
        SetBusy(true);
        try
        {
            var (_, copied) = await Task.Run(() =>
            {
                var backup = CreateBackupCore(target, pack.Folder, "install");
                var count = CopyPack(target, pack.Folder);
                return (backup, count);
            });
            SetMessage($"Installed {copied} file(s) from '{pack.Name}'. A backup was saved and can be restored from the Backups list.");
        }
        catch (Exception ex)
        {
            SetMessage("Install failed: " + ex.Message + " Select the latest backup and use Restore to return to the previous state.", true);
        }
        finally
        {
            SetBusy(false);
            RefreshBackups();
        }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (BackupList.SelectedItem is not BackupItem item)
        {
            SetMessage("Select a backup to restore.", true);
            return;
        }

        var manifest = item.Manifest;
        if (!Directory.Exists(manifest.Target))
        {
            SetMessage("The original directory no longer exists: " + manifest.Target, true);
            return;
        }

        var restoreCount = manifest.Files.Count(f => f.Existed);
        var removeCount = manifest.Files.Count - restoreCount;
        var text = $"Restore the backup from {manifest.CreatedAt:yyyy-MM-dd HH:mm}?\n\n"
                 + $"Directory: {manifest.Target}\n"
                 + $"Files to restore: {restoreCount}\n"
                 + $"Files to remove (they did not exist at backup time): {removeCount}";
        var answer = System.Windows.MessageBox.Show(this, text, "Restore backup", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        SetBusy(true);
        try
        {
            var (restored, removed) = await Task.Run(() => RestoreCore(item));
            SetMessage($"Restore complete: {restored} file(s) restored, {removed} file(s) removed.");
        }
        catch (Exception ex) { SetMessage("Restore failed: " + ex.Message, true); }
        finally { SetBusy(false); }
    }

    private void OpenBackups_Click(object sender, RoutedEventArgs e) => OpenFolder(BackupsRoot);
}