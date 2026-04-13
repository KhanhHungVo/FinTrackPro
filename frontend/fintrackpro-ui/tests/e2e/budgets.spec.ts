import { test, expect } from '@playwright/test'
import { injectAuthToken } from './auth.setup'

test.describe('Budgets', () => {
  test.beforeEach(async ({ page }) => {
    await injectAuthToken(page)
    await page.goto('/budgets')
  })

  test('create budget', async ({ page }) => {
    // Use Education — less likely to already exist, to avoid 409 conflicts from prior runs
    // Selects on page: [0]=month, [1]=category (TransactionCategorySelector), [2]=currency
    await page.locator('select').nth(1).selectOption({ label: '📚 Education' })
    await page.getByPlaceholder('500').fill('300')
    await page.getByRole('button', { name: /add budget/i }).click()

    // Scope both assertions to the same li to avoid matching pre-existing budgets
    const budgetRow = page.locator('li').filter({ hasText: '📚 Education' }).filter({ hasText: '/ $300.00' })
    await expect(budgetRow.first()).toBeVisible({ timeout: 10000 })
  })

  test('delete budget', async ({ page }) => {
    // Use Rent + $999 as distinctive identifiers to avoid collision with create test
    await page.locator('select').nth(1).selectOption({ label: '🏠 Rent' })
    await page.getByPlaceholder('500').fill('999')
    await page.getByRole('button', { name: /add budget/i }).click()

    // BudgetsPage renders resolveCategoryLabel() → "🏠 Rent", and the spend/limit display
    const deleteRow = page.locator('li').filter({ hasText: '🏠 Rent' }).filter({ hasText: '/ $999' })
    await expect(deleteRow.first()).toBeVisible({ timeout: 10000 })
    const before = await deleteRow.count()

    // ✕ button has aria-label="Delete"; clicking it opens a confirmation dialog
    await deleteRow.first().getByRole('button', { name: 'Delete' }).click()
    // Confirm deletion in the dialog
    await page.getByRole('button', { name: 'Delete' }).last().click()
    await expect(deleteRow).toHaveCount(before - 1, { timeout: 10000 })
  })
})
