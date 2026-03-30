import { test, expect } from '@playwright/test'
import { injectAuthToken } from './auth.setup'

test.describe('Trades', () => {
  test.beforeEach(async ({ page }) => {
    await injectAuthToken(page)
    await page.goto('/trades')
  })

  test('log trade and verify PnL', async ({ page }) => {
    // Form is hidden behind a toggle button
    await page.getByRole('button', { name: /add trade/i }).click()

    await page.getByPlaceholder('Symbol (e.g. BTCUSDT)').fill('BTCUSDT')
    await page.getByPlaceholder('Entry price').fill('60000')
    await page.getByPlaceholder('Exit price').fill('65000')
    await page.getByPlaceholder('Position size').fill('0.1')
    await page.getByPlaceholder('Fees').fill('5')
    await page.getByRole('button', { name: /add trade/i }).last().click()

    // result = (65000 - 60000) * 0.1 - 5 = 495, scoped to the table row
    const row = page.locator('tr').filter({ hasText: 'BTCUSDT' }).filter({ hasText: '+$495.00' })
    await expect(row.first()).toBeVisible({ timeout: 10000 })
  })
})
