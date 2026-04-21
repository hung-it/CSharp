Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$coreRoot = Join-Path $repoRoot 'CSharp-main'

Push-Location $coreRoot
try {
    dotnet build VinhKhanhAudioGuide.Backend
    dotnet test VinhKhanhAudioGuide.Backend.Tests --logger "console;verbosity=minimal"
}
finally {
    Pop-Location
}