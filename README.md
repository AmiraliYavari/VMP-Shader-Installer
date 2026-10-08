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

Select the VMP launcher folder, the one that contains VMP.ini and the plugins folder. Each pack is a folder inside ShaderPacks laid out like that folder, for example plugins\dxgi.dll. Files are copied with the same relative paths. pack.json and README.txt are not installed.

A pack can also change VMP.ini by listing entries in pack.json:

```json
"ini": [
  { "section": "Addons", "key": "ReShade5", "value": "..." }
]
```

Close VMP before installing. The anti-cheat may block third-party graphics mods, so use this at your own risk.

## License

MIT