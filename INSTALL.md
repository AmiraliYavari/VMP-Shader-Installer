# Quick start

1. Install the .NET 10 SDK.
2. Open this folder in VS Code.
3. Run `dotnet run --project .\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj`.
4. For a release EXE, run `powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1`.
5. Put compatible, legally distributable shader files inside a pack folder under `ShaderPacks`.

The current project is an installer framework. It deliberately does not pretend that an arbitrary ReShade/ENB pack is compatible with VMP; the exact VMP rendering/injection requirements must be validated before shipping a real pack.
