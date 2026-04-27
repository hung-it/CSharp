# PhoAmThuc - Run All
# Khoi dong tat ca: backend API + cloudflared tunnel + web app + admin web
#
# Usage:
#   .\run-all.ps1              # Full run
#   .\run-all.ps1 -SkipDeploy  # Chi start services (khong deploy)
#   .\run-all.ps1 -DryRun     # Xem command truoc khi chay
#
# Prerequisites:
#   - Backend project at: ..\VinhKhanhAudioGuide.Api
#   - Web app at: ..\..\WebApp
#   - Admin web at: ..\PhoAmThuc.Admin
#   - Cloudflared.exe at: ..\..\tool\Cloudflared.exe
#   - .NET SDK + Node.js / npm installed

param(
    [switch]$SkipDeploy,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot

$repoRoot    = (Resolve-Path (Join-Path $scriptRoot '..')).Path
$apiProject  = Join-Path $repoRoot 'VinhKhanhAudioGuide.Api\VinhKhanhAudioGuide.Api.csproj'
$webAppRoot  = Join-Path $repoRoot '..\WebApp'
$adminRoot   = Join-Path $repoRoot 'PhoAmThuc.Admin'
$cloudflared = Join-Path $repoRoot '..\tool\Cloudflared.exe'
$envFile     = Join-Path $webAppRoot '.env.production'

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
# Pre-flight checks
# ============================================================

Write-Host ''
Write-Host '============================================' -ForegroundColor Cyan
Write-Host ' PhoAmThuc - Run All' -ForegroundColor Cyan
Write-Host '============================================' -ForegroundColor Cyan
Write-Host ''

if (-not (Test-Path $apiProject))  { throw "[ERROR] Backend project not found: $apiProject" }
if (-not (Test-Path $webAppRoot))  { throw "[ERROR] WebApp folder not found: $webAppRoot" }
if (-not (Test-Path $adminRoot))   { throw "[ERROR] Admin web folder not found: $adminRoot" }
if (-not (Test-Path $cloudflared)) { throw "[ERROR] Cloudflared.exe not found: $cloudflared" }

Write-Host '[OK] All paths valid' -ForegroundColor Green

# ============================================================
# 1. Backend API
# ============================================================

$backendRunning = Test-Port -Port $backendPort
if ($backendRunning) {
    Write-Host "[INFO] Backend already running on port $backendPort (PID: $($backendRunning.OwningProcess))" -ForegroundColor Yellow
} else {
    if ($DryRun) {
        Write-Host "[DryRun] Would start: dotnet run --project '$apiProject' --urls '$backendUrl'"
    } else {
        Write-Host '[1/4] Starting backend API...' -ForegroundColor Cyan
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

# ============================================================
# 2. Cloudflared tunnel
# ============================================================

Write-Host '[2/4] Starting Cloudflared tunnel...' -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "[DryRun] Would start: cloudflared tunnel --url $backendUrl"
    Write-Host '[DryRun] NOTE: Each run produces a NEW URL. Will update .env.production + redeploy after tunnel is up.'
} else {
    Get-Process -Name 'cloudflared' -ErrorAction SilentlyContinue | Stop-Process -Force

    $cfLogDir  = "$env:TEMP\cf_logs_$([guid]::NewGuid().ToString('N').Substring(0,8))"
    $cfLogFile = Join-Path $cfLogDir 'cf_output.log'
    New-Item -ItemType Directory -Path $cfLogDir -Force | Out-Null

    # Run cloudflared via Start-Process with a -NoExit wrapper so it stays alive.
    # We use PowerShell's -Command to redirect stdout+stderr to the log file.
    $wrapper = "-NoProfile -NoExit -Command `"& '$cloudflared' tunnel --url '$backendUrl' 2>&1 | Out-File -FilePath '$cfLogFile' -Encoding ASCII`""
    Start-Process -FilePath 'powershell' -ArgumentList $wrapper -WindowStyle Hidden

    $tunnelUrl = $null
    $start = Get-Date
    while ((Get-Date) - $start -lt (New-TimeSpan -Seconds 60)) {
        Start-Sleep -Seconds 3
        if (Test-Path $cfLogFile) {
            $size = (Get-Item $cfLogFile -ErrorAction SilentlyContinue).Length
            if ($null -ne $size -and $size -gt 0) {
                $content = Get-Content $cfLogFile -Raw -ErrorAction SilentlyContinue
                if ($content -match 'https://[a-z0-9-]+\.trycloudflare\.com') {
                    $tunnelUrl = ($Matches[0] -split '\s')[0] -replace '/$', ''
                    break
                }
            }
        }
    }

    if (-not $tunnelUrl) {
        Write-Host '[WARN] Could not detect tunnel URL from log.' -ForegroundColor Yellow
        if (Test-Path $cfLogFile) {
            Get-Content $cfLogFile -ErrorAction SilentlyContinue | Select-Object -Last 15 |
                ForEach-Object { Write-Host $_ -ForegroundColor Gray }
        }
        throw '[ERROR] Cloudflared tunnel URL not detected within 60 seconds'
    }

    Write-Host "[OK] Tunnel ready: $tunnelUrl" -ForegroundColor Green
}

# ============================================================
# 3. Web App (Vite dev server)
# ============================================================

Write-Host '[3/4] Starting web app...' -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "[DryRun] Would start: cd '$webAppRoot'; npm run dev"
} else {
    Start-Process -FilePath 'powershell' -ArgumentList '-NoProfile', '-NoExit', '-Command',
        "cd '$webAppRoot'; npm run dev" -PassThru | Out-Null
    Write-Host '[OK] Web app starting at http://localhost:5173' -ForegroundColor Green
}

# ============================================================
# 4. Admin Web (Vite dev server)
# ============================================================

Write-Host '[4/4] Starting admin web...' -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "[DryRun] Would start: cd '$adminRoot'; npm run dev"
} else {
    Start-Process -FilePath 'powershell' -ArgumentList '-NoProfile', '-NoExit', '-Command',
        "cd '$adminRoot'; npm run dev" -PassThru | Out-Null
    Write-Host '[OK] Admin web starting at http://localhost:5180' -ForegroundColor Green
}

# ============================================================
# 5. Deploy web app (unless skipped)
# ============================================================

if (-not $SkipDeploy) {
    if ($DryRun) {
        Write-Host '[DryRun] Would: update .env.production + build + deploy web app'
    } else {
        if (-not $tunnelUrl) {
            Write-Host '[WARN] No tunnel URL, skipping deploy.' -ForegroundColor Yellow
        } else {
            Write-Host '[DEPLOY] Updating .env.production with: '"$tunnelUrl" -ForegroundColor Cyan
            Set-Content -Path $envFile -Value "VITE_API_BASE_URL=$tunnelUrl`n" -NoNewline -ErrorAction Stop

            Write-Host '[DEPLOY] Building web app...' -ForegroundColor Cyan
            $build = Start-Process -FilePath 'powershell' -ArgumentList '-NoProfile', '-Command',
                "cd '$webAppRoot'; npm run build 2>&1" -PassThru -Wait -NoNewWindow
            if ($build.ExitCode -ne 0) {
                $errCode = $build.ExitCode
                throw "[ERROR] Build failed (exit code $errCode)"
            }
            Write-Host '[OK] Web app built' -ForegroundColor Green

            Write-Host '[DEPLOY] Deploying to Cloudflare Pages...' -ForegroundColor Cyan
            $deploy = Start-Process -FilePath 'powershell' -ArgumentList '-NoProfile', '-Command',
                "cd '$webAppRoot'; npx wrangler pages deploy dist --project-name=pho-am-thuc-web --commit-dirty=true 2>&1" -PassThru -Wait -NoNewWindow
            if ($deploy.ExitCode -ne 0) {
                $errCode = $deploy.ExitCode
                throw "[ERROR] Deploy failed (exit code $errCode)"
            }
            Write-Host '[OK] Deployed to Cloudflare Pages!' -ForegroundColor Green
        }
    }
} else {
    Write-Host '[SKIP] Cloudflare deploy skipped (-SkipDeploy)' -ForegroundColor Yellow
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
Write-Host "  Cloudflared: $tunnelUrl" -ForegroundColor White
Write-Host '  Web App:     http://localhost:5173' -ForegroundColor White
Write-Host '  Admin Web:   http://localhost:5180' -ForegroundColor White
if (-not $SkipDeploy) {
    Write-Host '  Prod Web:    https://pho-am-thuc-web.pages.dev' -ForegroundColor White
}
Write-Host ''
Write-Host ' Keep these terminal windows open. Close them to stop.' -ForegroundColor Gray
Write-Host ''
