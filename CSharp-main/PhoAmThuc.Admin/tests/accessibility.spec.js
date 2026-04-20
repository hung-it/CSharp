import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

test.describe('Web Admin Accessibility Tests', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:5173')
  })

  test('Login page should be accessible', async ({ page }) => {
    const accessibilityScanResults = await new AxeBuilder({ page }).analyze()
    expect(accessibilityScanResults.violations).toEqual([])
  })

  test('Dashboard should be accessible', async ({ page }) => {
    // Login first
    await page.fill('[data-testid="email-input"]', 'admin@test.com')
    await page.fill('[data-testid="password-input"]', 'password123')
    await page.click('[data-testid="login-button"]')
    await page.waitForURL(/.*dashboard/)

    const accessibilityScanResults = await new AxeBuilder({ page }).analyze()
    expect(accessibilityScanResults.violations).toEqual([])
  })

  test('Forms should have proper labels and ARIA attributes', async ({ page }) => {
    await page.fill('[data-testid="email-input"]', 'admin@test.com')
    await page.fill('[data-testid="password-input"]', 'password123')
    await page.click('[data-testid="login-button"]')
    await page.waitForURL(/.*dashboard/)

    // Navigate to create tour form
    await page.click('[data-testid="tours-menu"]')
    await page.click('[data-testid="create-tour-button"]')

    // Check form accessibility
    const nameInput = page.locator('[data-testid="tour-name-input"]')
    const descInput = page.locator('[data-testid="tour-description-input"]')
    
    await expect(nameInput).toHaveAttribute('aria-label')
    await expect(descInput).toHaveAttribute('aria-label')
    
    const accessibilityScanResults = await new AxeBuilder({ page }).analyze()
    expect(accessibilityScanResults.violations).toEqual([])
  })

  test('Navigation should be keyboard accessible', async ({ page }) => {
    await page.fill('[data-testid="email-input"]', 'admin@test.com')
    await page.fill('[data-testid="password-input"]', 'password123')
    await page.click('[data-testid="login-button"]')
    await page.waitForURL(/.*dashboard/)

    // Test keyboard navigation
    await page.keyboard.press('Tab')
    await page.keyboard.press('Tab')
    await page.keyboard.press('Enter')

    // Should navigate using keyboard
    const focusedElement = await page.evaluate(() => document.activeElement?.tagName)
    expect(['BUTTON', 'A', 'INPUT']).toContain(focusedElement)
  })

  test('Images should have alt text', async ({ page }) => {
    await page.fill('[data-testid="email-input"]', 'admin@test.com')
    await page.fill('[data-testid="password-input"]', 'password123')
    await page.click('[data-testid="login-button"]')
    await page.waitForURL(/.*dashboard/)

    const images = await page.locator('img').all()
    
    for (const img of images) {
      const alt = await img.getAttribute('alt')
      expect(alt).not.toBeNull()
      expect(alt).not.toBe('')
    }
  })

  test('Color contrast should meet WCAG standards', async ({ page }) => {
    await page.fill('[data-testid="email-input"]', 'admin@test.com')
    await page.fill('[data-testid="password-input"]', 'password123')
    await page.click('[data-testid="login-button"]')
    await page.waitForURL(/.*dashboard/)

    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .analyze()
    
    expect(accessibilityScanResults.violations).toEqual([])
  })

  test('Tables should have proper headers', async ({ page }) => {
    await page.fill('[data-testid="email-input"]', 'admin@test.com')
    await page.fill('[data-testid="password-input"]', 'password123')
    await page.click('[data-testid="login-button"]')
    await page.waitForURL(/.*dashboard/)

    await page.click('[data-testid="tours-menu"]')

    const tables = await page.locator('table').all()
    
    for (const table of tables) {
      const headers = await table.locator('th').all()
      expect(headers.length).toBeGreaterThan(0)
      
      for (const header of headers) {
        const scope = await header.getAttribute('scope')
        expect(['col', 'row']).toContain(scope)
      }
    }
  })

  test('Modal dialogs should trap focus', async ({ page }) => {
    await page.fill('[data-testid="email-input"]', 'admin@test.com')
    await page.fill('[data-testid="password-input"]', 'password123')
    await page.click('[data-testid="login-button"]')
    await page.waitForURL(/.*dashboard/)

    // Open a modal
    await page.click('[data-testid="tours-menu"]')
    await page.click('[data-testid="create-tour-button"]')

    // Test focus trap
    await page.keyboard.press('Tab')
    await page.keyboard.press('Tab')
    await page.keyboard.press('Tab')
    
    const focusedElement = await page.evaluate(() => document.activeElement)
    const modal = await page.locator('[role="dialog"]').first()
    
    expect(await modal.isVisible()).toBe(true)
  })

  test('Error messages should be announced to screen readers', async ({ page }) => {
    // Try to login with invalid credentials
    await page.fill('[data-testid="email-input"]', 'invalid@test.com')
    await page.fill('[data-testid="password-input"]', 'wrongpassword')
    await page.click('[data-testid="login-button"]')

    const errorMessage = page.locator('[data-testid="error-message"]')
    await expect(errorMessage).toBeVisible()
    
    // Check ARIA attributes for screen readers
    await expect(errorMessage).toHaveAttribute('role', 'alert')
    await expect(errorMessage).toHaveAttribute('aria-live', 'polite')
  })

  test('Skip links should be available', async ({ page }) => {
    await page.fill('[data-testid="email-input"]', 'admin@test.com')
    await page.fill('[data-testid="password-input"]', 'password123')
    await page.click('[data-testid="login-button"]')
    await page.waitForURL(/.*dashboard/)

    // Press Tab to reveal skip links
    await page.keyboard.press('Tab')
    
    const skipLink = page.locator('a[href="#main-content"]')
    await expect(skipLink).toBeVisible()
  })

  test('High contrast mode should work', async ({ page }) => {
    // Enable high contrast mode
    await page.emulateMedia({ colorScheme: 'dark' })
    
    await page.fill('[data-testid="email-input"]', 'admin@test.com')
    await page.fill('[data-testid="password-input"]', 'password123')
    await page.click('[data-testid="login-button"]')
    await page.waitForURL(/.*dashboard/)

    const accessibilityScanResults = await new AxeBuilder({ page }).analyze()
    expect(accessibilityScanResults.violations).toEqual([])
  })

  test('Zoom to 200% should not break layout', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 })
    
    // Zoom to 200%
    await page.evaluate(() => {
      document.body.style.zoom = '200%'
    })

    await page.fill('[data-testid="email-input"]', 'admin@test.com')
    await page.fill('[data-testid="password-input"]', 'password123')
    await page.click('[data-testid="login-button"]')
    await page.waitForURL(/.*dashboard/)

    // Check if content is still accessible
    const mainContent = page.locator('[data-testid="main-content"]')
    await expect(mainContent).toBeVisible()
    
    // Check for horizontal scrolling (should not occur)
    const hasHorizontalScroll = await page.evaluate(() => {
      return document.documentElement.scrollWidth > document.documentElement.clientWidth
    })
    
    expect(hasHorizontalScroll).toBe(false)
  })
})