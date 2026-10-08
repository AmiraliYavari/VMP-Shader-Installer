# AY VMP Shader Installer

[English](README.md) | فارسی

یک ابزار دسکتاپ ویندوزی برای نصب و مدیریت شیدر پک‌های VMP و GTA V که با WPF و .NET ساخته شده است.

طراحی و توسعه: امیرعلی یاوری

## توجه مهم

این مخزن شامل VMP، GTA V، ReShade یا هیچ فایل شیدر متعلق به دیگران نیست. پوشه ExamplePack فقط یک قالب خالی است. فقط شیدر پک‌هایی را نصب کنید که اجازه استفاده و انتشار آن‌ها را دارید. سازگاری هر پک با VMP باید توسط سازنده همان پک بررسی شود.

## امکانات

- رابط کاربری تیره با WPF
- تشخیص خودکار مسیرهای رایج VMP
- انتخاب دستی مسیر
- پیدا کردن شیدر پک‌ها از پوشه ShaderPacks
- نصب با حفظ ساختار مسیر فایل‌ها
- پشتیبان‌گیری خودکار از فایل‌هایی که بازنویسی می‌شوند
- دسترسی سریع به پوشه پشتیبان
- اجرا بدون نیاز به دسترسی ادمین

## پیش‌نیازها

- ویندوز 10 یا 11
- .NET 10 SDK

## اجرا از روی سورس

```powershell
dotnet restore .\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj
dotnet run --project .\src\AYVMPShaderInstaller\AYVMPShaderInstaller.csproj
```

## ساخت فایل EXE مستقل

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

خروجی در پوشه publish و همراه با یک کپی از ShaderPacks قرار می‌گیرد.

## ساخت شیدر پک

هر پک یک پوشه داخل ShaderPacks است:

```
ShaderPacks/
  MyPack/
    pack.json
    README.txt
    ...files to install
```

همه فایل‌ها با همان مسیر نسبی در پوشه انتخاب‌شده کپی می‌شوند. فایل‌های pack.json و README.txt فقط اطلاعات پک هستند و نصب نمی‌شوند.

## پشتیبان‌گیری

قبل از بازنویسی هر فایل، نسخه اصلی در این مسیر کپی می‌شود:

```
%LOCALAPPDATA%\AYVMPShaderInstaller\Backups\<date-time>
```

## نقشه راه

- تشخیص بر اساس اطلاعات واقعی لانچر VMP
- بررسی نسخه و سازگاری پک‌ها
- تایید با SHA-256
- بازگردانی و حذف نصب هر پک
- مخزن پک‌ها و کانال به‌روزرسانی
- امضای دیجیتال نسخه‌های منتشرشده

## لایسنس

MIT. فایل LICENSE را ببینید.