import { test, expect } from '@playwright/test'
import { injectAuthToken } from './auth.setup'

test.describe('Transactions', () => {
  test.beforeEach(async ({ page }) => {
    await injectAuthToken(page)
    await page.goto('/transactions')
  })

  test('create expense transaction', async ({ page }) => {
    await page.locator('input[type="number"]').fill('85.50')
    // Selects on page: [0]=month, [1]=currency, [2]=category (TransactionCategorySelector)
    await page.locator('select').nth(2).selectOption({ label: '🍜 Food & Beverage' })
    await page.getByRole('button', { name: /add transaction/i }).click()

    // TransactionsPage renders resolveCategoryLabel(slug) → "🍜 Food & Beverage"
    const row = page.locator('li').filter({ hasText: '🍜 Food & Beverage' }).filter({ hasText: '-$85.50' })
    await expect(row.first()).toBeVisible({ timeout: 10000 })
  })

  test('create income transaction', async ({ page }) => {
    // Toggle to Income — the only button named "Income" on the page is the type toggle in the form
    await page.getByRole('button', { name: 'Income' }).click()
    await page.locator('input[type="number"]').fill('1000')
    // After toggling Income, category select loads income categories
    await page.locator('select').nth(2).selectOption({ label: '💰 Salary' })
    await page.getByRole('button', { name: /add transaction/i }).click()

    const row = page.locator('li').filter({ hasText: '💰 Salary' }).filter({ hasText: '+$1,000.00' })
    await expect(row.first()).toBeVisible({ timeout: 10000 })
  })

  test('delete transaction', async ({ page }) => {
    // Use note field as a unique identifier to handle repeated test runs
    await page.locator('input[type="number"]').fill('50')
    await page.locator('select').nth(2).selectOption({ label: '🛍️ Shopping' })
    await page.getByPlaceholder('Note').fill('e2e-delete')
    await page.getByRole('button', { name: /add transaction/i }).click()

    const deleteRow = page.locator('li').filter({ hasText: 'e2e-delete' })
    await expect(deleteRow.first()).toBeVisible({ timeout: 10000 })
    const before = await deleteRow.count()

    // ✕ button has title="Delete" (no aria-label; text content is ✕)
    await deleteRow.first().getByTitle('Delete').click()
    await expect(deleteRow).toHaveCount(before - 1, { timeout: 10000 })
  })
})
