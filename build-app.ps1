# Build and Deploy script for VinhKhanhAudioGuide MAUI App
# Usage: .\build-app.ps1 [-Target android|windows|ios|all] [-Deploy]

param(
    [ValidateSet("android", "windows", "ios", "all")]
    [string]$Target = "all",
    [switch]$Deploy
)

$ErrorActionPreference = "Stop"
$ProjectPath = "d:\PhoAmThuc\CSharp-app\VinhKhanhAudioGuide.App"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VinhKhanhAudioGuide App Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Set environment variables
$env:JAVA_HOME = "C:\Program Files\Android\Android Studio\jbr"
$env:JavaSdkDirectory = $env:JAVA_HOME
$env:ANDROID_HOME = "C:\Users\VIET HUNG\AppData\Local\Android\Sdk"
$env:AndroidSdkDirectory = $env:ANDROID_HOME
$env:PATH = "$env:PATH;$env:ANDROID_HOME\platform-tools"

Write-Host "[1/4] Cleaning previous build..." -ForegroundColor Yellow
cd $ProjectPath
dotnet clean -q
Remove-Item -Recurse -Force "$ProjectPath\obj" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "$ProjectPath\bin" -ErrorAction SilentlyContinue
Write-Host "      Done!" -ForegroundColor Green

Write-Host "[2/4] Building..." -ForegroundColor Yellow

switch ($Target) {
    "android" {
        dotnet build -f net10.0-android --configuration Debug 2>&1 | Out-Null
    }
    "windows" {
        dotnet build -f net10.0-windows10.0.19041.0 --configuration Debug 2>&1 | Out-Null
    }
    "ios" {
        dotnet build -f net10.0-ios --configuration Debug 2>&1 | Out-Null
    }
    "all" {
        dotnet build --configuration Debug 2>&1 | Out-Null
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "      Build failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  BUILD FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}

Write-Host "      Done!" -ForegroundColor Green

# Deploy to Android emulator
if ($Deploy -and ($Target -eq "android" -or $Target -eq "all")) {
    Write-Host "[3/4] Checking emulator..." -ForegroundColor Yellow
    
    $devices = adb devices | Select-String "device$"
    if (-not $devices) {
        Write-Host "      No emulator found! Make sure an Android emulator is running." -ForegroundColor Red
        Write-Host "      Start emulator with: emulator -avd <avd_name>" -ForegroundColor Yellow
    } else {
        Write-Host "      Found emulator: $devices" -ForegroundColor Green
        
        # Find APK
        $apkPath = "$ProjectPath\bin\Debug\net10.0-android"
        $apk = Get-ChildItem $apkPath -Filter "*-Signed.apk" | Select-Object -First 1
        
        if ($apk) {
            Write-Host "[4/4] Installing APK..." -ForegroundColor Yellow
            adb install -r $apk.FullName | Out-Null
            Write-Host "      Done!" -ForegroundColor Green
            
            # Find activity
            $packageName = "com.companyname.vinhkhanhaudioguide.app"
            $activityLine = adb shell dumpsys package $packageName | Select-String "Activity" | Select-Object -First 1
            if ($activityLine -match "(\S+)\s+$packageName/(\S+)") {
                $activity = $Matches[2]
                
                Write-Host "[5/5] Launching app..." -ForegroundColor Yellow
                adb shell am start -n "$packageName/$activity" | Out-Null
                Write-Host "      Done!" -ForegroundColor Green
                Write-Host ""
                Write-Host "========================================" -ForegroundColor Green
                Write-Host "  APP IS RUNNING ON EMULATOR!" -ForegroundColor Green
                Write-Host "========================================" -ForegroundColor Green
            }
        } else {
            Write-Host "      APK not found!" -ForegroundColor Red
        }
    }
} else {
    Write-Host "[3/4] Skip deploy (use -Deploy flag)" -ForegroundColor Gray
    Write-Host "[4/4] Skip deploy" -ForegroundColor Gray
    Write-Host "[5/5] Skip deploy" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  BUILD SUCCESSFUL!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output locations:" -ForegroundColor Cyan

switch ($Target) {
    { $_ -eq "android" -or $_ -eq "all" } {
        $apk = "$ProjectPath\bin\Debug\net10.0-android\com.companyname.vinhkhanhaudioguide.app-Signed.apk"
        if (Test-Path $apk) {
            Write-Host "  Android APK: $apk" -ForegroundColor White
        }
    }
    { $_ -eq "windows" -or $_ -eq "all" } {
        $exe = "$ProjectPath\bin\Debug\net10.0-windows10.0.19041.0\win-x64\VinhKhanhAudioGuide.App.exe"
        if (Test-Path $exe) {
            Write-Host "  Windows EXE: $exe" -ForegroundColor White
        }
    }
}

Write-Host ""
