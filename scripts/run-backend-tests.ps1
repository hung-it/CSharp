Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

Push-Location $repoRoot
try {
    dotnet build VinhKhanhAudioGuide.Backend
    dotnet test VinhKhanhAudioGuide.Backend.Tests --logger "console;verbosity=minimal"
}
finally {
    Pop-Location
}