# AY VMP Shader Installer

[English](README.md) | فارسی

یک ابزار ویندوزی برای نصب شیدر پک‌های VMP و GTA V با قابلیت پشتیبان‌گیری و بازگردانی.

توسعه: امیرعلی یاوری

این مخزن شامل VMP، GTA V، ReShade یا هیچ فایل متعلق به دیگران نیست. فقط پک‌هایی را نصب کنید که اجازه استفاده از آن‌ها را دارید.

## پیش‌نیازها

- ویندوز 10 یا 11
- .NET 10 SDK

## اجرا

```powershell
dotnet run --project .\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj
```

## ساخت

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

## شیدر پک‌ها

پوشه لانچر VMP را انتخاب کنید، همان پوشه‌ای که VMP.ini و پوشه plugins داخل آن است. هر پک یک پوشه داخل ShaderPacks است که ساختارش مثل همان پوشه چیده شده، مثلا plugins\dxgi.dll. فایل‌ها با همان مسیر نسبی کپی می‌شوند. فایل‌های pack.json و README.txt نصب نمی‌شوند.

هر پک می‌تواند با فهرست کردن موارد در pack.json فایل VMP.ini را هم تغییر دهد:

```json
"ini": [
  { "section": "Addons", "key": "ReShade5", "value": "..." }
]
```

قبل از نصب VMP را ببندید. آنتی‌چیت ممکن است مودهای گرافیکی غیررسمی را مسدود کند، پس با مسئولیت خودتان استفاده کنید.

## لایسنس

MIT