import { test, expect } from '@playwright/test'
import { injectAuthToken } from './auth.setup'

test.describe('Transactions', () => {
  test.beforeEach(async ({ page }) => {
    await injectAuthToken(page)
    await page.goto('/transactions')
  })

  test('create transaction', async ({ page }) => {
    // Amount input is type="number" with no placeholder text
    await page.locator('input[type="number"]').fill('85.50')
    await page.getByPlaceholder('Category').fill('E2E Groceries')
    await page.getByRole('button', { name: /add transaction/i }).click()

    // Verify the new row appears in the list (scoped to the list item)
    const row = page.locator('li').filter({ hasText: 'E2E Groceries' }).filter({ hasText: '-$85.50' })
    await expect(row.first()).toBeVisible({ timeout: 10000 })
  })
})
