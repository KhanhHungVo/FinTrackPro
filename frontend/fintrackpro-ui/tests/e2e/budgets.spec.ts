import { test, expect } from '@playwright/test'
import { injectAuthToken } from './auth.setup'

test.describe('Budgets', () => {
  test.beforeEach(async ({ page }) => {
    await injectAuthToken(page)
    await page.goto('/budgets')
  })

  test('create budget', async ({ page }) => {
    await page.getByPlaceholder('e.g. Food').fill('E2E Test Food')
    await page.getByPlaceholder('500').fill('300')
    await page.getByRole('button', { name: /add budget/i }).click()

    await expect(page.getByText('E2E Test Food')).toBeVisible({ timeout: 10000 })
    await expect(page.getByText('/ $300.00')).toBeVisible()
  })
})
