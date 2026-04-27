# Update POI Priorities Script
# Chạy script này SAU KHI backend đã tạo database

$dbPath = "d:\PhoAmThuc\CSharp-main\VinhKhanhAudioGuide.Api\Data\vinh-khanh-guide.db"

if (-not (Test-Path $dbPath)) {
    Write-Host "Database not found. Please run the backend first!" -ForegroundColor Red
    exit 1
}

# Load SQLite assembly
Add-Type -Path "C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.0\System.Data.SQLite.dll" -ErrorAction SilentlyContinue

$connectionString = "Data Source=$dbPath;Version=3;"
$connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
$connection.Open()

$command = $connection.CreateCommand()

# Priority assignments:
# Tour 1 landmarks: POI005, POI006, POI008, POI009, POI010, POI011 = Priority 10 (high traffic tourist spots)
# Food spots: POI001, POI002, POI003, POI004, POI007, POI012 = Priority 5 (local food)
$priorities = @{
    "POI001" = 5   # Quán Bánh Mì - food
    "POI002" = 5   # Tiệm Cơm Gia Đình - food
    "POI003" = 5   # Hẻm Ăn Vĩnh Hội - food
    "POI004" = 5   # Quán Cà Phê Sân Đình - food
    "POI005" = 10  # Chợ Xóm Chiếu - landmark
    "POI006" = 10  # Nhà Thờ Vĩnh Hội - landmark
    "POI007" = 5   # Hồ Cá Cảnh - scenic
    "POI008" = 10  # Cây Cổ Thụ - landmark
    "POI009" = 10  # Đình Xóm Chiếu - landmark
    "POI010" = 10  # Chùa An Lạc - landmark
    "POI011" = 10  # Khách Sạn Vĩnh Hội - landmark
    "POI012" = 5   # Chợ Đêm Khánh Hội - food
}

Write-Host "Updating POI Priorities..." -ForegroundColor Cyan

foreach ($code in $priorities.Keys) {
    $priority = $priorities[$code]
    $command.CommandText = "UPDATE Pois SET Priority = $priority WHERE Code = '$code'"
    $rowsAffected = $command.ExecuteNonQuery()
    if ($rowsAffected -gt 0) {
        Write-Host "  $code -> Priority $priority" -ForegroundColor Green
    } else {
        Write-Host "  $code -> Not found (may not exist yet)" -ForegroundColor Yellow
    }
}

# Verify
$command.CommandText = "SELECT Code, Name, Priority FROM Pois ORDER BY Priority DESC, Code"
$reader = $command.ExecuteReader()

Write-Host "`nCurrent POI Priorities:" -ForegroundColor Cyan
Write-Host ("{0,-10} {1,-35} {2}" -f "Code", "Name", "Priority")
Write-Host ("-" * 60)

while ($reader.Read()) {
    $code = $reader.GetString(0)
    $name = $reader.GetString(1)
    $priority = $reader.GetInt32(2)
    Write-Host ("{0,-10} {1,-35} {2}" -f $code, $name, $priority)
}

$reader.Close()
$connection.Close()

Write-Host "`nDone! Priority updated successfully." -ForegroundColor Green
