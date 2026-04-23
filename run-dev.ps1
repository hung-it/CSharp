# PhoAmThuc - Development Script
# Chạy cả Backend API và Frontend Admin cùng lúc

$ErrorActionPreference = "Stop"

# Đường dẫn tuyệt đối
$projectRoot = "D:\PhoAmThuc"
$apiPath = "D:\PhoAmThuc\CSharp-main\VinhKhanhAudioGuide.Api"
$frontendPath = "D:\PhoAmThuc\CSharp-main\PhoAmThuc.Admin"

# Colors
function Write-Step { param($msg) Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "    $msg" -ForegroundColor Green }
function Write-Warning { param($msg) Write-Host "    $msg" -ForegroundColor Yellow }

Write-Host @"

╔════════════════════════════════════════════════════════════╗
║              PhoAmThuc Development Server                ║
╚════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Magenta

# Kill existing processes on ports 5140 and 5173
Write-Step "Cleaning up existing processes..."
Get-NetTCPConnection -LocalPort 5140, 5173 -ErrorAction SilentlyContinue | ForEach-Object {
    Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
    Write-Warning "Killed process on port $($_.LocalPort)"
}

# Start Backend (C# API) - chạy trên port 5140
Write-Step "Starting Backend API..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$apiPath'; dotnet run"

Start-Sleep -Seconds 2

# Start Frontend (React/Vite)
Write-Step "Starting Frontend Admin..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$frontendPath'; npm run dev"

Write-Host @"

╔════════════════════════════════════════════════════════════╗
║  Backend API:  http://localhost:5140                       ║
║  Frontend:     http://localhost:5173                       ║
╚════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Green

Write-Step "Servers started! Press Ctrl+C to stop."
