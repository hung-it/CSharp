# Hướng Dẫn Testing - VinhKhanh Audio Guide

## 📋 Tổng Quan

Dự án sử dụng một bộ test toàn diện bao gồm:
- **Backend API Tests**: Unit, Integration, Performance, Security
- **Web Admin Tests**: Unit, E2E, Accessibility  
- **Mobile App Tests**: Integration, UI, Performance

## 🚀 Chạy Test

### Chạy Tất Cả Test
```powershell
.\run-all-tests.ps1
```

### Chạy Test Nhanh
```powershell
.\run-quick-tests.ps1
```

### Chạy Test Riêng Lẻ

#### Backend API Tests
```bash
cd CSharp-main\VinhKhanhAudioGuide.Backend.Tests

# Tất cả test
dotnet test

# Chỉ Integration tests
dotnet test --filter "Category=Integration"

# Chỉ Performance tests  
dotnet test Performance\

# Chỉ Security tests
dotnet test Security\
```

#### Web Admin Tests
```bash
cd CSharp-main\PhoAmThuc.Admin

# Unit tests
npm run test

# E2E tests
npm run test:e2e

# Accessibility tests
npm run test:accessibility

# Tất cả test
npm run test:all

# Test với coverage
npm run test:coverage
```

#### Mobile App Tests
```bash
cd CSharp-app\VinhKhanhAudioGuide.App

# Integration tests
dotnet test Tests\

# Chỉ Core tests
dotnet test Tests\ --filter "Category=Core"

# UI tests (cần emulator)
dotnet test Tests\ --filter "Category=UI"
```

## 📊 Loại Test

### 1. Backend API Tests

#### Unit Tests
- **Location**: `Application/Services/`
- **Coverage**: Business logic, services, domain models
- **Framework**: xUnit, Moq
- **Example**:
```csharp
[Fact]
public async Task CreateTour_Should_Return_Success()
{
    // Test tour creation logic
}
```

#### Integration Tests  
- **Location**: `Integration/`
- **Coverage**: API endpoints, database operations
- **Framework**: ASP.NET Core Testing, TestServer
- **Example**:
```csharp
[Fact]
public async Task POST_Tours_Should_Create_New_Tour()
{
    var response = await _client.PostAsJsonAsync("/api/tours", newTour);
    response.EnsureSuccessStatusCode();
}
```

#### Performance Tests
- **Location**: `Performance/`
- **Coverage**: Load testing, stress testing, memory usage
- **Metrics**: Response time < 2s, 90% success rate
- **Example**:
```csharp
[Fact]
public async Task Load_Test_Should_Handle_50_Concurrent_Requests()
{
    // Test concurrent API calls
}
```

#### Security Tests
- **Location**: `Security/`
- **Coverage**: SQL injection, XSS, authentication, authorization
- **Example**:
```csharp
[Fact]
public async Task SQL_Injection_Test_Should_Not_Allow_Malicious_Input()
{
    // Test malicious SQL inputs
}
```

### 2. Web Admin Tests

#### Unit Tests
- **Location**: `src/tests/`
- **Coverage**: React components, hooks, utilities
- **Framework**: Vitest, Testing Library
- **Example**:
```javascript
test('should create new tour', async () => {
  render(<TourForm />);
  // Test component behavior
});
```

#### E2E Tests
- **Location**: `tests/`
- **Coverage**: User workflows, integration scenarios
- **Framework**: Playwright
- **Example**:
```javascript
test('should login and create tour', async ({ page }) => {
  await page.goto('/login');
  // Test complete user journey
});
```

#### Accessibility Tests
- **Location**: `tests/accessibility.spec.js`
- **Coverage**: WCAG compliance, keyboard navigation, screen readers
- **Framework**: Playwright + axe-core
- **Example**:
```javascript
test('should be accessible', async ({ page }) => {
  const results = await new AxeBuilder({ page }).analyze();
  expect(results.violations).toEqual([]);
});
```

### 3. Mobile App Tests

#### Integration Tests
- **Location**: `Tests/`
- **Coverage**: App functionality, services integration
- **Framework**: xUnit, MAUI Testing
- **Example**:
```csharp
[Fact]
public async Task QrScanPage_Should_Process_Valid_QR_Code()
{
    await qrScanPage.ProcessQrCodeAsync(testQrData);
    Assert.True(qrScanPage.IsNavigationCompleted);
}
```

#### UI Tests
- **Location**: `Tests/`
- **Coverage**: User interface, user interactions
- **Framework**: Appium WebDriver
- **Example**:
```csharp
[Fact]
public void QR_Scanner_Should_Open_Camera()
{
    var scanButton = _driver.FindElement(By.Id("ScanQrButton"));
    scanButton.Click();
    // Test UI behavior
}
```

## 🔧 Cấu Hình Test

### Backend Test Configuration
```xml
<!-- VinhKhanhAudioGuide.Backend.Tests.csproj -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
```

### Web Admin Test Configuration
```json
// package.json
{
  "devDependencies": {
    "@playwright/test": "^1.40.0",
    "@axe-core/playwright": "^4.8.2",
    "vitest": "^1.0.4"
  }
}
```

```javascript
// vitest.config.js
export default defineConfig({
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/tests/setup.js']
  }
})
```

### Mobile App Test Configuration
```xml
<!-- VinhKhanhAudioGuide.App.Tests.csproj -->
<PackageReference Include="Microsoft.Maui.Testing" Version="8.0.0" />
<PackageReference Include="Appium.WebDriver" Version="5.0.0-rc.1" />
```

## 📈 Test Coverage

### Mục Tiêu Coverage
- **Backend**: > 80% line coverage
- **Web Admin**: > 75% line coverage  
- **Mobile App**: > 70% line coverage

### Xem Coverage Report
```bash
# Backend
dotnet test --collect:"XPlat Code Coverage"

# Web Admin
npm run test:coverage

# Mở HTML report
start coverage/index.html
```

## 🐛 Debug Tests

### Backend Tests
```bash
# Debug mode
dotnet test --logger "console;verbosity=detailed"

# Specific test
dotnet test --filter "TestMethodName"
```

### Web Admin Tests
```bash
# Debug mode
npm run test -- --reporter=verbose

# UI mode
npm run test:ui

# E2E debug
npm run test:e2e -- --debug
```

### Mobile App Tests
```bash
# Verbose output
dotnet test --verbosity diagnostic

# Specific category
dotnet test --filter "Category=Integration"
```

## 🔄 CI/CD Integration

### GitHub Actions
- **File**: `.github/workflows/ci-cd-tests.yml`
- **Triggers**: Push to main/develop, Pull requests
- **Jobs**: Backend tests, Web tests, Mobile tests, Security scan

### Test Reports
- Tự động tạo báo cáo HTML
- Upload artifacts cho failed tests
- Comment kết quả trên PR

## 📝 Best Practices

### 1. Test Naming
```csharp
// Good
[Fact]
public async Task CreateTour_WithValidData_Should_Return_Success()

// Bad  
[Fact]
public async Task Test1()
```

### 2. Test Structure (AAA Pattern)
```csharp
[Fact]
public async Task Test_Method()
{
    // Arrange
    var service = new TourService();
    var tour = new Tour { Name = "Test" };
    
    // Act
    var result = await service.CreateAsync(tour);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Name);
}
```

### 3. Mock Dependencies
```csharp
// Use dependency injection and mocking
var mockRepository = new Mock<ITourRepository>();
var service = new TourService(mockRepository.Object);
```

### 4. Test Data
```csharp
// Use builders or factories for test data
var tour = new TourBuilder()
    .WithName("Test Tour")
    .WithLanguage("vi")
    .Build();
```

## 🚨 Troubleshooting

### Common Issues

#### Backend Tests Fail
```bash
# Check database connection
dotnet ef database update

# Clear test cache
dotnet clean
dotnet build
```

#### Web Admin Tests Fail
```bash
# Clear node modules
rm -rf node_modules package-lock.json
npm install

# Update browsers
npx playwright install
```

#### Mobile Tests Fail
```bash
# Check emulator
adb devices

# Restart emulator
adb kill-server
adb start-server
```

### Performance Issues
- Chạy test song song: `dotnet test --parallel`
- Giới hạn test: `--filter "Category=Fast"`
- Skip slow tests: `--filter "Category!=Slow"`

## 📞 Support

- **Issues**: Tạo GitHub issue với label `testing`
- **Documentation**: Xem README.md trong mỗi test project
- **Examples**: Tham khảo existing test cases

---

**Lưu ý**: Luôn chạy test trước khi commit code và đảm bảo tất cả test pass trước khi merge PR.