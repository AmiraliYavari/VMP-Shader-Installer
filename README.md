# AY VMP Shader Installer

English | [فارسی](README.fa.md)

A Windows desktop tool for installing and managing shader packs for VMP / GTA V, built with WPF and .NET.

Designed and developed by Amirali Yavari.

## Important Notice

This repository does not include VMP, GTA V, ReShade, or any third-party shader files. The bundled ExamplePack is an empty template. Only install shader packs you have permission to use and distribute. Compatibility of a pack with VMP must be verified by the pack author.

## Features

- Dark WPF interface
- Automatic detection of common VMP folders
- Manual directory selection
- Shader pack discovery from the ShaderPacks folder
- Installation that preserves relative file paths
- Automatic backup of any file that gets overwritten
- Quick access to the backup folder
- Runs without administrator rights

## Requirements

- Windows 10 or 11
- .NET 10 SDK

## Run From Source

```powershell
dotnet restore .\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj
dotnet run --project .\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj
```

## Build a Standalone EXE

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

The output is placed in the publish folder together with a copy of ShaderPacks.

## Creating a Shader Pack

Each pack is a folder inside ShaderPacks:

```
ShaderPacks/
  MyPack/
    pack.json
    README.txt
    ...files to install
```

All files are copied to the selected directory using the same relative paths. pack.json and README.txt are metadata and are not installed.

## Backups

Before a file is overwritten, the original is copied to:

```
%LOCALAPPDATA%\AYVMPShaderInstaller\Backups\<date-time>
```

## Roadmap

- Detection based on real VMP launcher data
- Pack version and compatibility checks
- SHA-256 verification
- Restore and uninstall per pack
- Pack repository and update channel
- Code signing for releases

## License

MIT. See the LICENSE file.