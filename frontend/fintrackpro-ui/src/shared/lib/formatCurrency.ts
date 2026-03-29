/**
 * Formats a numeric amount as a currency string using the browser's Intl API.
 * @param amount   The numeric value to format
 * @param currency ISO 4217 currency code (e.g. "USD", "VND", "EUR")
 * @param locale   BCP 47 locale string (e.g. "en", "vi")
 */
export function formatCurrency(
  amount: number,
  currency: string,
  locale = 'en',
): string {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 2,
  }).format(amount)
}
