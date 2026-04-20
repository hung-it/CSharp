param(
    [switch]$InstallDependencies,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$adminPath = Join-Path $repoRoot 'PhoAmThuc.Admin'
$backendProject = Join-Path $repoRoot 'VinhKhanhAudioGuide.Api\VinhKhanhAudioGuide.Api.csproj'

if (-not (Test-Path $backendProject)) {
    throw "Backend project not found: $backendProject"
}

if (-not (Test-Path $adminPath)) {
    throw "Frontend admin folder not found: $adminPath"
}

$frontendSetup = if ($InstallDependencies) {
    'npm install; '
}
elseif (-not (Test-Path (Join-Path $adminPath 'node_modules'))) {
    'npm install; '
}
else {
    ''
}

$backendCommand = "Set-Location '$repoRoot'; dotnet run --project '$backendProject' --launch-profile http"
$frontendCommand = "Set-Location '$adminPath'; ${frontendSetup}npm run dev"

Write-Host 'Preparing local dev environment...'
Write-Host "Repo root: $repoRoot"

if ($DryRun) {
    Write-Host ''
    Write-Host '[DryRun] Backend command:'
    Write-Host $backendCommand
    Write-Host ''
    Write-Host '[DryRun] Frontend command:'
    Write-Host $frontendCommand
    exit 0
}

Start-Process -FilePath 'powershell' -ArgumentList @(
    '-NoExit',
    '-ExecutionPolicy',
    'Bypass',
    '-Command',
    $backendCommand
) | Out-Null

Start-Process -FilePath 'powershell' -ArgumentList @(
    '-NoExit',
    '-ExecutionPolicy',
    'Bypass',
    '-Command',
    $frontendCommand
) | Out-Null

Write-Host ''
Write-Host 'Started backend and frontend in separate terminals.'
Write-Host '- Backend:  http://localhost:5140'
Write-Host '- Frontend: http://localhost:5173 (or next available port)'
Write-Host ''
Write-Host 'Tip: close each terminal window to stop its process.'
