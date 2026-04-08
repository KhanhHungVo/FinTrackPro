/**
 * Converts a stored amount to the user's preferred display currency.
 *
 * Each record stores its original amount and the exchange rate at creation time
 * (rateToUsd = units of currency per 1 USD, i.e. USD amount = amount / rateToUsd).
 *
 * When the record's original currency matches the preferred display currency the
 * stored value is returned as-is, avoiding drift from the USD round-trip.
 *
 * @param amount             The amount in the record's original currency
 * @param recordRateToUsd    Rate at the time the record was created
 * @param preferredRateToUsd Rate for the user's currently selected display currency
 * @param recordCurrency     ISO code of the currency the record was saved in
 * @param preferredCurrency  ISO code of the user's selected display currency
 * @returns Amount expressed in the user's preferred currency
 */
export function convertAmount(
  amount: number,
  recordRateToUsd: number,
  preferredRateToUsd: number,
  recordCurrency?: string,
  preferredCurrency?: string,
): number {
  if (recordCurrency && preferredCurrency && recordCurrency === preferredCurrency) return amount
  if (recordRateToUsd === 0 || preferredRateToUsd === 0) return 0
  const amountInUsd = amount / recordRateToUsd
  return amountInUsd * preferredRateToUsd
}
