import { test, expect } from '@playwright/test'
import { injectAuthToken } from './auth.setup'

test.describe('Budgets', () => {
  test.beforeEach(async ({ page }) => {
    await injectAuthToken(page)
    await page.goto('/budgets')
  })

  test('create budget', async ({ page }) => {
    // Selects on page: [0]=month, [1]=category (TransactionCategorySelector), [2]=currency
    await page.locator('select').nth(1).selectOption({ label: '🍜 Food & Beverage' })
    await page.getByPlaceholder('500').fill('300')
    await page.getByRole('button', { name: /add budget/i }).click()

    // BudgetsPage renders budget.category (slug), not label
    await expect(page.getByText('food_beverage')).toBeVisible({ timeout: 10000 })
    await expect(page.getByText('/ $300.00')).toBeVisible()
  })

  test('delete budget', async ({ page }) => {
    // Use Rent + $999 as distinctive identifiers to avoid collision with create test
    await page.locator('select').nth(1).selectOption({ label: '🏠 Rent' })
    await page.getByPlaceholder('500').fill('999')
    await page.getByRole('button', { name: /add budget/i }).click()

    // BudgetsPage shows slug "rent" and "/ $999"
    const deleteRow = page.locator('li').filter({ hasText: 'rent' }).filter({ hasText: '/ $999' })
    await expect(deleteRow.first()).toBeVisible({ timeout: 10000 })
    const before = await deleteRow.count()

    // × button has aria-label="Delete"
    await deleteRow.first().getByRole('button', { name: 'Delete' }).click()
    await expect(deleteRow).toHaveCount(before - 1, { timeout: 10000 })
  })
})
