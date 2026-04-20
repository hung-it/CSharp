# Script chạy toàn bộ test cho app mobile và web
Write-Host "=== CHẠY TOÀN BỘ TEST CHO ỨNG DỤNG ===" -ForegroundColor Green

$ErrorActionPreference = "Continue"
$testResults = @()

# 1. Test Backend API
Write-Host "`n1. Chạy test Backend API..." -ForegroundColor Yellow
Set-Location "CSharp-main\VinhKhanhAudioGuide.Backend.Tests"
$backendResult = dotnet test --logger "console;verbosity=detailed" --collect:"XPlat Code Coverage"
$testResults += @{
    Component = "Backend API"
    Result = if ($LASTEXITCODE -eq 0) { "PASS" } else { "FAIL" }
    ExitCode = $LASTEXITCODE
}

# 2. Test Web Admin - Unit Tests
Write-Host "`n2. Chạy test Web Admin - Unit Tests..." -ForegroundColor Yellow
Set-Location "..\PhoAmThuc.Admin"
npm install
$webUnitResult = npm run test
$testResults += @{
    Component = "Web Admin Unit"
    Result = if ($LASTEXITCODE -eq 0) { "PASS" } else { "FAIL" }
    ExitCode = $LASTEXITCODE
}

# 3. Test Web Admin - E2E Tests
Write-Host "`n3. Chạy test Web Admin - E2E Tests..." -ForegroundColor Yellow
npx playwright install
$webE2EResult = npm run test:e2e
$testResults += @{
    Component = "Web Admin E2E"
    Result = if ($LASTEXITCODE -eq 0) { "PASS" } else { "FAIL" }
    ExitCode = $LASTEXITCODE
}

# 4. Test Mobile App - Integration Tests
Write-Host "`n4. Chạy test Mobile App - Integration Tests..." -ForegroundColor Yellow
Set-Location "..\..\CSharp-app\VinhKhanhAudioGuide.App"
$mobileResult = dotnet test Tests\ --logger "console;verbosity=detailed"
$testResults += @{
    Component = "Mobile App Integration"
    Result = if ($LASTEXITCODE -eq 0) { "PASS" } else { "FAIL" }
    ExitCode = $LASTEXITCODE
}

# 5. Tạo báo cáo tổng hợp
Write-Host "`n=== BÁO CÁO KẾT QUẢ TEST ===" -ForegroundColor Green
Write-Host "Component                | Result | Status" -ForegroundColor White
Write-Host "-------------------------|--------|--------" -ForegroundColor White

$totalTests = 0
$passedTests = 0

foreach ($result in $testResults) {
    $totalTests++
    $status = if ($result.Result -eq "PASS") { 
        $passedTests++
        "✅ PASS" 
    } else { 
        "❌ FAIL" 
    }
    
    $component = $result.Component.PadRight(24)
    $resultText = $result.Result.PadRight(6)
    
    Write-Host "$component | $resultText | $status"
}

Write-Host "-------------------------|--------|--------" -ForegroundColor White
Write-Host "TỔNG KẾT: $passedTests/$totalTests test suites passed" -ForegroundColor $(if ($passedTests -eq $totalTests) { "Green" } else { "Red" })

# 6. Tạo báo cáo HTML
$htmlReport = @"
<!DOCTYPE html>
<html>
<head>
    <title>Báo Cáo Test - VinhKhanh Audio Guide</title>
    <meta charset="utf-8">
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background: #2196F3; color: white; padding: 20px; border-radius: 5px; }
        .summary { background: #f5f5f5; padding: 15px; margin: 20px 0; border-radius: 5px; }
        .pass { color: #4CAF50; font-weight: bold; }
        .fail { color: #f44336; font-weight: bold; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
        th { background-color: #f2f2f2; }
        .timestamp { color: #666; font-size: 0.9em; }
    </style>
</head>
<body>
    <div class="header">
        <h1>Báo Cáo Test - VinhKhanh Audio Guide</h1>
        <p class="timestamp">Thời gian: $(Get-Date -Format "dd/MM/yyyy HH:mm:ss")</p>
    </div>
    
    <div class="summary">
        <h2>Tổng Kết</h2>
        <p><strong>Tổng số test suites:</strong> $totalTests</p>
        <p><strong>Passed:</strong> <span class="pass">$passedTests</span></p>
        <p><strong>Failed:</strong> <span class="fail">$($totalTests - $passedTests)</span></p>
        <p><strong>Tỷ lệ thành công:</strong> $([math]::Round(($passedTests / $totalTests) * 100, 2))%</p>
    </div>
    
    <h2>Chi Tiết Kết Quả</h2>
    <table>
        <tr>
            <th>Component</th>
            <th>Kết Quả</th>
            <th>Trạng Thái</th>
            <th>Mô Tả</th>
        </tr>
"@

foreach ($result in $testResults) {
    $statusClass = if ($result.Result -eq "PASS") { "pass" } else { "fail" }
    $statusIcon = if ($result.Result -eq "PASS") { "✅" } else { "❌" }
    
    $description = switch ($result.Component) {
        "Backend API" { "Test các API endpoints, database operations, business logic" }
        "Web Admin Unit" { "Test các React components, hooks, utilities" }
        "Web Admin E2E" { "Test end-to-end user workflows trên web admin" }
        "Mobile App Integration" { "Test tích hợp các chức năng mobile app" }
        default { "Test tổng hợp" }
    }
    
    $htmlReport += @"
        <tr>
            <td>$($result.Component)</td>
            <td class="$statusClass">$($result.Result)</td>
            <td>$statusIcon</td>
            <td>$description</td>
        </tr>
"@
}

$htmlReport += @"
    </table>
    
    <h2>Khuyến Nghị</h2>
    <ul>
"@

if ($passedTests -eq $totalTests) {
    $htmlReport += "<li class='pass'>✅ Tất cả test đều pass! Ứng dụng sẵn sàng deploy.</li>"
} else {
    $htmlReport += "<li class='fail'>❌ Có $($totalTests - $passedTests) test suite(s) fail. Cần kiểm tra và sửa lỗi trước khi deploy.</li>"
    $htmlReport += "<li>🔍 Xem chi tiết log để xác định nguyên nhân lỗi.</li>"
    $htmlReport += "<li>🛠️ Chạy lại test sau khi fix để đảm bảo tất cả đều pass.</li>"
}

$htmlReport += @"
    </ul>
    
    <h2>Cách Chạy Test Riêng Lẻ</h2>
    <pre>
# Backend API Tests
cd CSharp-main\VinhKhanhAudioGuide.Backend.Tests
dotnet test

# Web Admin Unit Tests  
cd CSharp-main\PhoAmThuc.Admin
npm run test

# Web Admin E2E Tests
cd CSharp-main\PhoAmThuc.Admin  
npm run test:e2e

# Mobile App Tests
cd CSharp-app\VinhKhanhAudioGuide.App
dotnet test Tests\
    </pre>
</body>
</html>
"@

$htmlReport | Out-File -FilePath "test-report.html" -Encoding UTF8
Write-Host "`n📊 Báo cáo HTML đã được tạo: test-report.html" -ForegroundColor Cyan

# 7. Mở báo cáo nếu tất cả test pass
if ($passedTests -eq $totalTests) {
    Write-Host "`n🎉 TẤT CẢ TEST ĐỀU PASS! Mở báo cáo..." -ForegroundColor Green
    Start-Process "test-report.html"
} else {
    Write-Host "`n⚠️  CÓ TEST FAIL. Vui lòng kiểm tra log và sửa lỗi." -ForegroundColor Red
}

Write-Host "`n=== HOÀN THÀNH TEST ===" -ForegroundColor Green