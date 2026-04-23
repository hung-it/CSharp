# Script chạy test nhanh - chỉ test quan trọng
Write-Host "=== CHẠY TEST NHANH ===" -ForegroundColor Green

# 1. Backend API Tests (chỉ integration tests)
Write-Host "`n1. Test Backend API..." -ForegroundColor Yellow
Set-Location "CSharp-main\VinhKhanhAudioGuide.Backend.Tests"
dotnet test --filter "Category=Integration" --logger "console;verbosity=minimal"

# 2. Web Admin Unit Tests (chỉ critical tests)  
Write-Host "`n2. Test Web Admin..." -ForegroundColor Yellow
Set-Location "..\PhoAmThuc.Admin"
npm run test -- --run --reporter=basic

# 3. Mobile App Core Tests
Write-Host "`n3. Test Mobile App..." -ForegroundColor Yellow
Set-Location "..\..\CSharp-app\VinhKhanhAudioGuide.App"
dotnet test Tests\ --filter "Category=Core" --logger "console;verbosity=minimal"

Write-Host "`n✅ HOÀN THÀNH TEST NHANH" -ForegroundColor Green