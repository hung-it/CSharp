import { test, expect } from '@playwright/test'

test.describe('Web Admin E2E Tests', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:5173')
  })

  test.describe('Authentication', () => {
    test('should login and access dashboard', async ({ page }) => {
      await page.fill('[data-testid="email-input"]', 'admin@test.com')
      await page.fill('[data-testid="password-input"]', 'password123')
      await page.click('[data-testid="login-button"]')

      await expect(page).toHaveURL(/.*dashboard/)
      await expect(page.locator('[data-testid="dashboard-title"]')).toBeVisible()
    })

    test('should show error for invalid credentials', async ({ page }) => {
      await page.fill('[data-testid="email-input"]', 'invalid@test.com')
      await page.fill('[data-testid="password-input"]', 'wrongpassword')
      await page.click('[data-testid="login-button"]')

      await expect(page.locator('[data-testid="error-message"]')).toBeVisible()
    })
  })

  test.describe('Tour Management', () => {
    test.beforeEach(async ({ page }) => {
      await page.fill('[data-testid="email-input"]', 'admin@test.com')
      await page.fill('[data-testid="password-input"]', 'password123')
      await page.click('[data-testid="login-button"]')
      await page.waitForURL(/.*dashboard/)
    })

    test('should create new tour', async ({ page }) => {
      await page.click('[data-testid="tours-menu"]')
      await page.click('[data-testid="create-tour-button"]')

      await page.fill('[data-testid="tour-name-input"]', 'Test Tour E2E')
      await page.fill('[data-testid="tour-description-input"]', 'This is a test tour created via E2E')
      await page.selectOption('[data-testid="tour-language-select"]', 'vi')
      
      await page.click('[data-testid="save-tour-button"]')
      
      await expect(page.locator('[data-testid="success-message"]')).toBeVisible()
      await expect(page.locator('text=Test Tour E2E')).toBeVisible()
    })

    test('should edit existing tour', async ({ page }) => {
      await page.click('[data-testid="tours-menu"]')
      await page.click('[data-testid="tour-item"]:first-child [data-testid="edit-button"]')

      await page.fill('[data-testid="tour-name-input"]', 'Updated Tour Name')
      await page.click('[data-testid="save-tour-button"]')

      await expect(page.locator('text=Updated Tour Name')).toBeVisible()
    })
  })

  test.describe('POI Management', () => {
    test.beforeEach(async ({ page }) => {
      await page.fill('[data-testid="email-input"]', 'admin@test.com')
      await page.fill('[data-testid="password-input"]', 'password123')
      await page.click('[data-testid="login-button"]')
      await page.waitForURL(/.*dashboard/)
    })

    test('should create POI with map selection', async ({ page }) => {
      await page.click('[data-testid="pois-menu"]')
      await page.click('[data-testid="create-poi-button"]')

      await page.fill('[data-testid="poi-name-input"]', 'Test POI')
      await page.fill('[data-testid="poi-description-input"]', 'Test POI Description')
      
      await page.click('[data-testid="map-container"]', { position: { x: 200, y: 200 } })
      
      await page.click('[data-testid="save-poi-button"]')
      
      await expect(page.locator('[data-testid="success-message"]')).toBeVisible()
    })
  })

  test.describe('Map Functionality', () => {
    test.beforeEach(async ({ page }) => {
      await page.fill('[data-testid="email-input"]', 'admin@test.com')
      await page.fill('[data-testid="password-input"]', 'password123')
      await page.click('[data-testid="login-button"]')
      await page.waitForURL(/.*dashboard/)
    })

    test('should display POIs on map', async ({ page }) => {
      await page.click('[data-testid="map-menu"]')
      
      await expect(page.locator('[data-testid="map-container"]')).toBeVisible()
      await expect(page.locator('[data-testid="poi-marker"]')).toHaveCount(3, { timeout: 5000 })
    })
  })

  test.describe('QR Code Management', () => {
    test.beforeEach(async ({ page }) => {
      await page.fill('[data-testid="email-input"]', 'admin@test.com')
      await page.fill('[data-testid="password-input"]', 'password123')
      await page.click('[data-testid="login-button"]')
      await page.waitForURL(/.*dashboard/)
    })

    test('should generate QR code for POI', async ({ page }) => {
      await page.click('[data-testid="pois-menu"]')
      await page.click('[data-testid="poi-item"]:first-child [data-testid="qr-button"]')
      
      await expect(page.locator('[data-testid="qr-code-image"]')).toBeVisible()
      await expect(page.locator('[data-testid="download-qr-button"]')).toBeVisible()
    })
  })
})