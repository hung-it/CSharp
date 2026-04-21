param(
    [switch]$BuildOnly,
    [switch]$StartBackend,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$mauiProject = Join-Path $repoRoot 'CSharp-app\VinhKhanhAudioGuide.App\VinhKhanhAudioGuide.App.csproj'
$backendProject = Join-Path $repoRoot 'CSharp-main\VinhKhanhAudioGuide.Api\VinhKhanhAudioGuide.Api.csproj'
$windowsTargetFramework = 'net10.0-windows10.0.19041.0'

if (-not (Test-Path $mauiProject)) {
    throw "MAUI project not found: $mauiProject"
}

if ($StartBackend -and -not (Test-Path $backendProject)) {
    throw "Backend project not found: $backendProject"
}

$backendCommandPreview = "Set-Location '$repoRoot'; dotnet run --project '$backendProject' --urls http://localhost:5140"
$mauiBuildCommandPreview = "Set-Location '$repoRoot'; dotnet build '$mauiProject' -f $windowsTargetFramework"
$mauiRunCommandPreview = "Set-Location '$repoRoot'; dotnet build '$mauiProject' -t:Run -f $windowsTargetFramework"

Write-Host 'Preparing MAUI run helper...'
Write-Host "Repo root: $repoRoot"
Write-Host "MAUI project: $mauiProject"

if ($DryRun) {
    Write-Host ''
    if ($StartBackend) {
        Write-Host '[DryRun] Backend command:'
        Write-Host $backendCommandPreview
        Write-Host ''
    }

    if ($BuildOnly) {
        Write-Host '[DryRun] MAUI build command:'
        Write-Host $mauiBuildCommandPreview
    }
    else {
        Write-Host '[DryRun] MAUI run command:'
        Write-Host $mauiRunCommandPreview
    }

    exit 0
}

if ($StartBackend) {
    Start-Process -FilePath 'powershell' -ArgumentList @(
        '-NoExit',
        '-ExecutionPolicy',
        'Bypass',
        '-Command',
        $backendCommandPreview
    ) | Out-Null

    Write-Host 'Started backend API in a separate terminal (http://localhost:5140).'
}

Push-Location $repoRoot
try {
    if ($BuildOnly) {
        dotnet build $mauiProject -f $windowsTargetFramework
    }
    else {
        dotnet build $mauiProject -t:Run -f $windowsTargetFramework
    }
}
finally {
    Pop-Location
}
