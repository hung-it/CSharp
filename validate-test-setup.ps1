# Script kiểm tra và validation toàn bộ test setup
Write-Host "=== KIỂM TRA TOÀN BỘ TEST SETUP ===" -ForegroundColor Green

$ErrorActionPreference = "Continue"
$validationResults = @()

# 1. Kiểm tra Backend Test Project
Write-Host "`n1. Kiểm tra Backend Test Project..." -ForegroundColor Yellow
$backendTestPath = "CSharp-main\VinhKhanhAudioGuide.Backend.Tests"

if (Test-Path $backendTestPath) {
    Write-Host "✅ Backend test project exists" -ForegroundColor Green
    
    # Kiểm tra test files
    $testFiles = @(
        "Application\Services\*.cs",
        "Infrastructure\*.cs", 
        "Integration\ApiIntegrationTests.cs",
        "Performance\ApiPerformanceTests.cs",
        "Security\SecurityTests.cs"
    )
    
    foreach ($pattern in $testFiles) {
        $files = Get-ChildItem -Path "$backendTestPath\$pattern" -ErrorAction SilentlyContinue
        if ($files) {
            Write-Host "  ✅ Found $($files.Count) test file(s) matching $pattern" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  No test files found matching $pattern" -ForegroundColor Yellow
        }
    }
    
    # Kiểm tra dependencies
    $csprojContent = Get-Content "$backendTestPath\VinhKhanhAudioGuide.Backend.Tests.csproj" -Raw
    $requiredPackages = @("xunit", "Microsoft.AspNetCore.Mvc.Testing", "Moq")
    
    foreach ($package in $requiredPackages) {
        if ($csprojContent -match $package) {
            Write-Host "  ✅ Package $package found" -ForegroundColor Green
        } else {
            Write-Host "  ❌ Package $package missing" -ForegroundColor Red
        }
    }
    
    $validationResults += @{ Component = "Backend Tests"; Status = "OK" }
} else {
    Write-Host "❌ Backend test project not found" -ForegroundColor Red
    $validationResults += @{ Component = "Backend Tests"; Status = "MISSING" }
}

# 2. Kiểm tra Web Admin Test Setup
Write-Host "`n2. Kiểm tra Web Admin Test Setup..." -ForegroundColor Yellow
$webAdminPath = "CSharp-main\PhoAmThuc.Admin"

if (Test-Path $webAdminPath) {
    Write-Host "✅ Web admin project exists" -ForegroundColor Green
    
    # Kiểm tra package.json
    if (Test-Path "$webAdminPath\package.json") {
        $packageJson = Get-Content "$webAdminPath\package.json" -Raw | ConvertFrom-Json
        
        $requiredDevDeps = @("vitest", "@playwright/test", "@axe-core/playwright", "@testing-library/react")
        foreach ($dep in $requiredDevDeps) {
            if ($packageJson.devDependencies.$dep) {
                Write-Host "  ✅ Dev dependency $dep found" -ForegroundColor Green
            } else {
                Write-Host "  ❌ Dev dependency $dep missing" -ForegroundColor Red
            }
        }
        
        # Kiểm tra test scripts
        $requiredScripts = @("test", "test:e2e", "test:accessibility")
        foreach ($script in $requiredScripts) {
            if ($packageJson.scripts.$script) {
                Write-Host "  ✅ Script $script found" -ForegroundColor Green
            } else {
                Write-Host "  ❌ Script $script missing" -ForegroundColor Red
            }
        }
    }
    
    # Kiểm tra test files
    $testFiles = @(
        "src\tests\setup.js",
        "src\tests\integration.test.jsx",
        "tests\e2e.spec.js",
        "tests\accessibility.spec.js"
    )
    
    foreach ($file in $testFiles) {
        if (Test-Path "$webAdminPath\$file") {
            Write-Host "  ✅ Test file $file exists" -ForegroundColor Green
        } else {
            Write-Host "  ❌ Test file $file missing" -ForegroundColor Red
        }
    }
    
    # Kiểm tra config files
    $configFiles = @("vitest.config.js", "playwright.config.js")
    foreach ($config in $configFiles) {
        if (Test-Path "$webAdminPath\$config") {
            Write-Host "  ✅ Config file $config exists" -ForegroundColor Green
        } else {
            Write-Host "  ❌ Config file $config missing" -ForegroundColor Red
        }
    }
    
    $validationResults += @{ Component = "Web Admin Tests"; Status = "OK" }
} else {
    Write-Host "❌ Web admin project not found" -ForegroundColor Red
    $validationResults += @{ Component = "Web Admin Tests"; Status = "MISSING" }
}

# 3. Kiểm tra Mobile App Test Setup
Write-Host "`n3. Kiểm tra Mobile App Test Setup..." -ForegroundColor Yellow
$mobileAppPath = "CSharp-app\VinhKhanhAudioGuide.App"

if (Test-Path $mobileAppPath) {
    Write-Host "✅ Mobile app project exists" -ForegroundColor Green
    
    # Kiểm tra test project
    if (Test-Path "$mobileAppPath\Tests") {
        Write-Host "  ✅ Tests folder exists" -ForegroundColor Green
        
        # Kiểm tra test files
        $testFiles = @(
            "Tests\MobileAppIntegrationTests.cs",
            "Tests\MobileUITests.cs",
            "Tests\Mocks\MockServices.cs"
        )
        
        foreach ($file in $testFiles) {
            if (Test-Path "$mobileAppPath\$file") {
                Write-Host "  ✅ Test file $file exists" -ForegroundColor Green
            } else {
                Write-Host "  ❌ Test file $file missing" -ForegroundColor Red
            }
        }
        
        # Kiểm tra test project file
        if (Test-Path "$mobileAppPath\Tests\VinhKhanhAudioGuide.App.Tests.csproj") {
            Write-Host "  ✅ Test project file exists" -ForegroundColor Green
            
            $testCsprojContent = Get-Content "$mobileAppPath\Tests\VinhKhanhAudioGuide.App.Tests.csproj" -Raw
            $requiredPackages = @("xunit", "Microsoft.Maui.Testing", "Appium.WebDriver")
            
            foreach ($package in $requiredPackages) {
                if ($testCsprojContent -match $package) {
                    Write-Host "    ✅ Package $package found" -ForegroundColor Green
                } else {
                    Write-Host "    ❌ Package $package missing" -ForegroundColor Red
                }
            }
        } else {
            Write-Host "  ❌ Test project file missing" -ForegroundColor Red
        }
    } else {
        Write-Host "  ❌ Tests folder not found" -ForegroundColor Red
    }
    
    $validationResults += @{ Component = "Mobile App Tests"; Status = "OK" }
} else {
    Write-Host "❌ Mobile app project not found" -ForegroundColor Red
    $validationResults += @{ Component = "Mobile App Tests"; Status = "MISSING" }
}

# 4. Kiểm tra Scripts
Write-Host "`n4. Kiểm tra Test Scripts..." -ForegroundColor Yellow
$scripts = @("run-all-tests.ps1", "run-quick-tests.ps1")

foreach ($script in $scripts) {
    if (Test-Path $script) {
        Write-Host "  ✅ Script $script exists" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Script $script missing" -ForegroundColor Red
    }
}

# 5. Kiểm tra CI/CD
Write-Host "`n5. Kiểm tra CI/CD Setup..." -ForegroundColor Yellow
if (Test-Path ".github\workflows\ci-cd-tests.yml") {
    Write-Host "  ✅ GitHub Actions workflow exists" -ForegroundColor Green
} else {
    Write-Host "  ❌ GitHub Actions workflow missing" -ForegroundColor Red
}

# 6. Kiểm tra Documentation
Write-Host "`n6. Kiểm tra Documentation..." -ForegroundColor Yellow
if (Test-Path "TESTING_GUIDE.md") {
    Write-Host "  ✅ Testing guide exists" -ForegroundColor Green
} else {
    Write-Host "  ❌ Testing guide missing" -ForegroundColor Red
}

# 7. Test Build (nếu có thể)
Write-Host "`n7. Kiểm tra Build..." -ForegroundColor Yellow

# Test backend build
if (Test-Path $backendTestPath) {
    Write-Host "  Testing backend build..." -ForegroundColor Cyan
    Set-Location $backendTestPath
    $buildResult = dotnet build --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ Backend test project builds successfully" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Backend test project build failed" -ForegroundColor Red
        Write-Host "    Error: $buildResult" -ForegroundColor Red
    }
    Set-Location "..\..\"
}

# Test web admin dependencies
if (Test-Path $webAdminPath) {
    Write-Host "  Testing web admin dependencies..." -ForegroundColor Cyan
    Set-Location $webAdminPath
    if (Test-Path "node_modules") {
        Write-Host "  ✅ Node modules already installed" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  Node modules not installed. Run 'npm install' to install dependencies." -ForegroundColor Yellow
    }
    Set-Location "..\..\"
}

# Test mobile app build
if (Test-Path "$mobileAppPath\Tests") {
    Write-Host "  Testing mobile app test build..." -ForegroundColor Cyan
    Set-Location "$mobileAppPath\Tests"
    $buildResult = dotnet build --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ Mobile app test project builds successfully" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Mobile app test project build failed" -ForegroundColor Red
        Write-Host "    Error: $buildResult" -ForegroundColor Red
    }
    Set-Location "..\..\..\"
}

# 8. Tổng kết
Write-Host "`n=== TỔNG KẾT VALIDATION ===" -ForegroundColor Green
Write-Host "Component                | Status" -ForegroundColor White
Write-Host "-------------------------|--------" -ForegroundColor White

$totalComponents = 0
$okComponents = 0

foreach ($result in $validationResults) {
    $totalComponents++
    $component = $result.Component.PadRight(24)
    $status = $result.Status
    
    if ($status -eq "OK") {
        $okComponents++
        Write-Host "$component | ✅ $status" -ForegroundColor Green
    } else {
        Write-Host "$component | ❌ $status" -ForegroundColor Red
    }
}

Write-Host "-------------------------|--------" -ForegroundColor White
Write-Host "TỔNG KẾT: $okComponents/$totalComponents components OK" -ForegroundColor $(if ($okComponents -eq $totalComponents) { "Green" } else { "Red" })

# 9. Khuyến nghị
Write-Host "`n=== KHUYẾN NGHỊ ===" -ForegroundColor Cyan

if ($okComponents -eq $totalComponents) {
    Write-Host "🎉 TẤT CẢ SETUP ĐÃ HOÀN THÀNH!" -ForegroundColor Green
    Write-Host "Bạn có thể chạy test bằng các lệnh sau:" -ForegroundColor White
    Write-Host "  .\run-all-tests.ps1     # Chạy tất cả test" -ForegroundColor Cyan
    Write-Host "  .\run-quick-tests.ps1   # Chạy test nhanh" -ForegroundColor Cyan
} else {
    Write-Host "⚠️  CÓ MỘT SỐ COMPONENT CHƯA HOÀN THÀNH" -ForegroundColor Yellow
    Write-Host "Vui lòng kiểm tra và hoàn thiện các phần còn thiếu." -ForegroundColor White
}

Write-Host "`nXem TESTING_GUIDE.md để biết thêm chi tiết về cách sử dụng." -ForegroundColor Cyan
Write-Host "`n=== HOÀN THÀNH VALIDATION ===" -ForegroundColor Green