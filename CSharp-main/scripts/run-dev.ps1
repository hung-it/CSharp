# PhoAmThuc - Dev Server Only
# Chi start: backend API + web app + admin web (KHONG deploy)
#
# Usage:
#   .\run-dev.ps1         # Full run (backend + web app + admin)
#   .\run-dev.ps1 -SkipBackend  # Chi start frontends
#   .\run-dev.ps1 -DryRun     # Xem command truoc khi chay
#
# Prerequisites:
#   - Backend project at: ..\VinhKhanhAudioGuide.Api
#   - Web app at: ..\..\WebApp
#   - Admin web at: ..\PhoAmThuc.Admin
#   - .NET SDK + Node.js / npm installed

param(
    [switch]$SkipBackend,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot

$repoRoot    = (Resolve-Path (Join-Path $scriptRoot '..')).Path
$apiProject  = Join-Path $repoRoot 'VinhKhanhAudioGuide.Api\VinhKhanhAudioGuide.Api.csproj'
$webAppRoot  = Join-Path $repoRoot '..\WebApp'
$adminRoot   = Join-Path $repoRoot 'PhoAmThuc.Admin'

$backendPort = 5140
$backendUrl  = "http://127.0.0.1:$backendPort"

# ============================================================
# Helpers
# ============================================================

function Test-Port {
    param([int]$Port)
    Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue | Select-Object -First 1
}

# ============================================================
# Header
# ============================================================

Write-Host ''
Write-Host '============================================' -ForegroundColor Cyan
Write-Host ' PhoAmThuc - Dev Server (No Deploy)' -ForegroundColor Cyan
Write-Host '============================================' -ForegroundColor Cyan
Write-Host ''

# ============================================================
# 1. Backend API (optional)
# ============================================================

if (-not $SkipBackend) {
    $backendRunning = Test-Port -Port $backendPort
    if ($backendRunning) {
        Write-Host "[INFO] Backend already running on port $backendPort (PID: $($backendRunning.OwningProcess))" -ForegroundColor Yellow
    } else {
        if ($DryRun) {
            Write-Host "[DryRun] Would start: dotnet run --project '$apiProject' --urls '$backendUrl'"
        } else {
            Write-Host '[1/3] Starting backend API...' -ForegroundColor Cyan
            Start-Process -FilePath 'powershell' -ArgumentList '-NoProfile', '-NoExit', '-Command',
                "cd '$repoRoot'; dotnet run --project '$apiProject' --urls '$backendUrl'" -PassThru | Out-Null

            $start = Get-Date
            $ready = $false
            while ((Get-Date) - $start -lt (New-TimeSpan -Seconds 90)) {
                Start-Sleep -Seconds 2
                try {
                    $exitCode = (Start-Process -FilePath 'curl.exe' -ArgumentList '-s','-o',([IO.Path]::GetTempPath()+'health.txt'),'--max-time','3','-w','%{http_code}',"$backendUrl/api/v1/health" -NoNewWindow -PassThru -Wait).ExitCode
                    if ($exitCode -eq 0) {
                        $code = Get-Content ([IO.Path]::GetTempPath()+'health.txt') -Raw -ErrorAction SilentlyContinue
                        if ($code -match '200') { $ready = $true; break }
                    }
                } catch {}
            }
            if (-not $ready) { throw '[ERROR] Backend failed to start within 90 seconds' }
            Write-Host "[OK] Backend ready at $backendUrl" -ForegroundColor Green
        }
    }
} else {
    Write-Host '[SKIP] Backend skipped' -ForegroundColor Yellow
}

# ============================================================
# 2. Web App (Vite dev server)
# ============================================================

Write-Host '[2/3] Starting web app...' -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "[DryRun] Would start: cd '$webAppRoot'; npm run dev"
} else {
    Start-Process -FilePath 'powershell' -ArgumentList '-NoProfile', '-NoExit', '-Command',
        "cd '$webAppRoot'; npm run dev" -PassThru | Out-Null
    Write-Host '[OK] Web app starting at http://localhost:5173' -ForegroundColor Green
}

# ============================================================
# 3. Admin Web (Vite dev server)
# ============================================================

Write-Host '[3/3] Starting admin web...' -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "[DryRun] Would start: cd '$adminRoot'; npm run dev"
} else {
    Start-Process -FilePath 'powershell' -ArgumentList '-NoProfile', '-NoExit', '-Command',
        "cd '$adminRoot'; npm run dev" -PassThru | Out-Null
    Write-Host '[OK] Admin web starting at http://localhost:5180' -ForegroundColor Green
}

# ============================================================
# Summary
# ============================================================

Write-Host ''
Write-Host '============================================' -ForegroundColor Cyan
Write-Host ' Ready!' -ForegroundColor Green
Write-Host '============================================' -ForegroundColor Cyan
Write-Host ''
Write-Host "  Backend API:  $backendUrl" -ForegroundColor White
Write-Host '  Web App:     http://localhost:5173' -ForegroundColor White
Write-Host '  Admin Web:   http://localhost:5180' -ForegroundColor White
Write-Host ''
Write-Host ' NOTE: Use run-all.ps1 if you need cloudflared tunnel + deploy.' -ForegroundColor Gray
Write-Host ' Keep these terminal windows open. Close them to stop.' -ForegroundColor Gray
Write-Host ''
