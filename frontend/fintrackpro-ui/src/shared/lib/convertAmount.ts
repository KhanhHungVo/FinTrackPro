/**
 * Converts a stored amount to the user's preferred display currency.
 *
 * Each record stores its original amount and the exchange rate at creation time
 * (rateToUsd = units of currency per 1 USD, i.e. USD amount = amount / rateToUsd).
 *
 * @param amount             The amount in the record's original currency
 * @param recordRateToUsd    Rate at the time the record was created
 * @param preferredRateToUsd Rate for the user's currently selected display currency
 * @returns Amount expressed in the user's preferred currency
 */
export function convertAmount(
  amount: number,
  recordRateToUsd: number,
  preferredRateToUsd: number,
): number {
  if (recordRateToUsd === 0 || preferredRateToUsd === 0) return 0
  const amountInUsd = amount / recordRateToUsd
  return amountInUsd * preferredRateToUsd
}
