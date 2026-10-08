$ErrorActionPreference = 'Stop'
dotnet restore "$PSScriptRoot\..\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj"
dotnet publish "$PSScriptRoot\..\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "$PSScriptRoot\..\publish"
Copy-Item -Recurse -Force "$PSScriptRoot\..\ShaderPacks" "$PSScriptRoot\..\publish\ShaderPacks"
Write-Host "Build complete: publish\AYVMPShaderInstaller.exe"
