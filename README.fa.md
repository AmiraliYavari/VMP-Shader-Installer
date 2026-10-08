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

هر پک یک پوشه داخل ShaderPacks است. فایل‌های آن با همان مسیر نسبی در پوشه انتخاب‌شده کپی می‌شوند. فایل‌های pack.json و README.txt نصب نمی‌شوند.

## لایسنس

MIT