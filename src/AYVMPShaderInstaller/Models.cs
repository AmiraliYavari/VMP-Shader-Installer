namespace AYVMPShaderInstaller;

public sealed record PackItem(string Folder, string Name, string Version, string Description, int FileCount)
{
    public string VersionLabel => string.IsNullOrWhiteSpace(Version) ? "" : "v" + Version;
    public string FilesLabel => FileCount == 1 ? "1 file" : $"{FileCount} files";
}

public sealed class BackupFileEntry
{
    public string Path { get; set; } = "";
    public bool Existed { get; set; }
}

public sealed class BackupManifest
{
    public DateTime CreatedAt { get; set; }
    public string Target { get; set; } = "";
    public string Pack { get; set; } = "";
    public string Kind { get; set; } = "manual";
    public List<BackupFileEntry> Files { get; set; } = new();
}

public sealed record BackupItem(string Folder, BackupManifest Manifest)
{
    public string Title => Manifest.Pack;

    public string Subtitle
    {
        get
        {
            var saved = Manifest.Files.Count(f => f.Existed);
            var kind = Manifest.Kind == "install" ? "Before install" : "Manual";
            return $"{Manifest.CreatedAt:yyyy-MM-dd HH:mm}  |  {kind}  |  {saved} of {Manifest.Files.Count} files saved";
        }
    }

    public string Detail => Manifest.Target;
}