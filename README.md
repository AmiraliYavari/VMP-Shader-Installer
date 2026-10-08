# AY VMP Shader Installer

English | [فارسی](README.fa.md)

A Windows tool for installing shader packs for VMP / GTA V, with backup and restore support.

Developed by Amirali Yavari.

This repository does not include VMP, GTA V, ReShade, or any third-party files. Only install packs you have permission to use.

## Requirements

- Windows 10 or 11
- .NET 10 SDK

## Run

```powershell
dotnet run --project .\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj
```

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

## Shader Packs

Each pack is a folder inside ShaderPacks. Its files are copied to the selected directory with the same relative paths. pack.json and README.txt are not installed.

## License

MIT