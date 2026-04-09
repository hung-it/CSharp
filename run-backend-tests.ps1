Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Push-Location 'D:\C#'
try {
    dotnet build VinhKhanhAudioGuide.Backend
    dotnet test VinhKhanhAudioGuide.Backend.Tests --logger "console;verbosity=minimal"
}
finally {
    Pop-Location
}