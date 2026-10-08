# AY VMP Shader Installer

A Windows/WPF shader-pack installer and manager framework for VMP/GTA V.

**Designed & developed by Amirali Yavari.**

> Important: this repository does not ship VMP, GTA V, ReShade, or any third-party copyrighted shader files. The included `ExamplePack` is only a template.

## Current features

- Modern dark WPF UI
- VMP/GTA directory detection helpers
- Manual directory selection
- Shader pack discovery from `ShaderPacks/`
- Relative-path pack installation
- Automatic backup of overwritten files
- Backup folder shortcut
- No admin elevation by default
- Ready for GitHub

## Requirements

- Windows 10/11
- .NET 10 SDK
- VS Code + C# Dev Kit or Visual Studio

## Run

```powershell
dotnet restore .\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj
dotnet run --project .\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj
```

## Publish a standalone EXE

```powershell
dotnet publish .\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish
```

Copy `ShaderPacks/` next to the published executable.

## Pack format

Each pack is a folder under `ShaderPacks/<PackName>/`. All files are copied using their relative paths. `pack.json` and `README.txt` are metadata and are not installed.

## Roadmap

- Real VMP installation detection based on launcher data
- Pack compatibility/version checks
- SHA-256 manifest verification
- Restore/uninstall per pack
- Pack repository / update channel
- Digital signing for release builds
- Optional elevated install mode only when required

## License

MIT. See `LICENSE`.
