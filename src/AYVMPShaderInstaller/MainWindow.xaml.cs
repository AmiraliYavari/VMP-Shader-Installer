using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;
using Wm = System.Windows.Media;

namespace AYVMPShaderInstaller;

public partial class MainWindow : Window
{
    private const string VmpIni = "VMP.ini";

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

    private static bool IsVmpRoot(string path) => File.Exists(Path.Combine(path, VmpIni));

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
        var ini = new List<IniEntry>();

        var json = Path.Combine(dir, "pack.json");
        if (File.Exists(json))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(json));
                var root = doc.RootElement;
                name = ReadString(root, "name") ?? name;
                version = ReadString(root, "version") ?? version;
                description = ReadString(root, "description") ?? description;

                if (root.ValueKind == JsonValueKind.Object
                    && root.TryGetProperty("ini", out var list)
                    && list.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in list.EnumerateArray())
                    {
                        var section = ReadString(item, "section");
                        var key = ReadString(item, "key");
                        var value = ReadString(item, "value");
                        if (!string.IsNullOrWhiteSpace(section) && !string.IsNullOrWhiteSpace(key) && value is not null)
                            ini.Add(new IniEntry(section.Trim(), key.Trim(), value));
                    }
                }
            }
            catch (JsonException) { }
        }

        return new PackItem(folder, name, version, description, GetPackFiles(dir).Count(), ini);
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
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        var roots = new List<string>
        {
            Path.Combine(local, "VMP"), Path.Combine(roaming, "VMP"),
            Path.Combine(pf, "VMP"), Path.Combine(pfx86, "VMP"),
            Path.Combine(pf, "VMP Launcher"), Path.Combine(pfx86, "VMP Launcher")
        };
        roots.AddRange(DriveInfo.GetDrives().Where(d => d.IsReady).SelectMany(d => new[]
        {
            Path.Combine(d.RootDirectory.FullName, "VMP"),
            Path.Combine(d.RootDirectory.FullName, "Games", "VMP")
        }));

        var hit = roots
            .SelectMany(r => new[] { r, Path.Combine(r, "VMP.app") })
            .FirstOrDefault(IsVmpRoot);

        if (hit is not null)
        {
            PathBox.Text = hit;
            UpdateDirectoryStatus();
            SetMessage("VMP folder detected.");
        }
        else
        {
            UpdateDirectoryStatus();
            if (showMessage) SetMessage("The VMP folder was not found automatically. Use Browse and select the folder that contains VMP.ini.", true);
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
            SetField(DirStatus, "Found", "Accent");
            if (IsVmpRoot(path)) SetField(GameStatus, "Found", "Accent");
            else SetField(GameStatus, "Missing", "Warn");
        }
    }

    private void PathBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateDirectoryStatus();

    private void Detect_Click(object sender, RoutedEventArgs e) => DetectPath(true);

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Select the VMP folder that contains VMP.ini",
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
            SetMessage("Select a valid VMP folder first.", true);
            return false;
        }
        if (!IsVmpRoot(target))
        {
            SetMessage("This folder does not contain VMP.ini. Select the VMP launcher folder, not the game folder.", true);
            return false;
        }
        if (PackList.SelectedItem is not PackItem selected)
        {
            SetMessage("Select a shader pack first.", true);
            return false;
        }
        if (selected.FileCount == 0 && selected.IniEntries.Count == 0)
        {
            SetMessage("This pack contains nothing to install.", true);
            return false;
        }
        pack = selected;
        return true;
    }

    // ---------- Backup / Install / Restore ----------

    private static List<string> GetBackupTargets(string source, PackItem pack)
    {
        var list = GetPackFiles(source).ToList();
        if (pack.IniEntries.Count > 0) list.Add(VmpIni);
        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private (string Folder, BackupManifest Manifest) CreateBackupCore(string target, PackItem pack, string kind)
    {
        var source = Path.Combine(_packsRoot, pack.Folder);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var folder = Path.Combine(BackupsRoot, stamp);
        for (var i = 1; Directory.Exists(folder); i++) folder = Path.Combine(BackupsRoot, stamp + "-" + i);

        var filesDir = Path.Combine(folder, "files");
        Directory.CreateDirectory(filesDir);

        var manifest = new BackupManifest
        {
            CreatedAt = DateTime.Now,
            Target = target,
            Pack = pack.Folder,
            Kind = kind
        };

        foreach (var relative in GetBackupTargets(source, pack))
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

    private static void ApplyIni(string target, IReadOnlyList<IniEntry> entries)
    {
        if (entries.Count == 0) return;
        var path = SafeCombine(target, VmpIni);
        var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();
        foreach (var entry in entries) SetIniValue(lines, entry);
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    private static void SetIniValue(List<string> lines, IniEntry entry)
    {
        var header = "[" + entry.Section + "]";
        var start = lines.FindIndex(l => l.Trim().Equals(header, StringComparison.OrdinalIgnoreCase));
        var newLine = entry.Key + "=" + entry.Value;

        if (start < 0)
        {
            if (lines.Count > 0 && lines[^1].Trim().Length > 0) lines.Add("");
            lines.Add(header);
            lines.Add(newLine);
            return;
        }

        var end = start + 1;
        while (end < lines.Count && !lines[end].TrimStart().StartsWith('['))
        {
            var line = lines[end];
            var eq = line.IndexOf('=');
            if (eq > 0 && line[..eq].Trim().Equals(entry.Key, StringComparison.OrdinalIgnoreCase))
            {
                lines[end] = newLine;
                return;
            }
            end++;
        }

        var insertAt = end;
        while (insertAt > start + 1 && lines[insertAt - 1].Trim().Length == 0) insertAt--;
        lines.Insert(insertAt, newLine);
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
            var result = await Task.Run(() => CreateBackupCore(target, pack, "manual"));
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
            var copied = await Task.Run(() =>
            {
                CreateBackupCore(target, pack, "install");
                var count = CopyPack(target, pack.Folder);
                ApplyIni(target, pack.IniEntries);
                return count;
            });
            SetMessage($"Installed {copied} file(s) from '{pack.Name}'. A backup was saved and can be restored from the Backups list.");
        }
        catch (Exception ex)
        {
            SetMessage("Install failed: " + ex.Message + " Make sure VMP is closed. You can select the latest backup and use Restore.", true);
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