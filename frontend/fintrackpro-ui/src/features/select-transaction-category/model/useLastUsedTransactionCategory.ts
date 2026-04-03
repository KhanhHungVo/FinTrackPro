const STORAGE_KEY = 'fintrackpro-last-transaction-category-id'

export function useLastUsedTransactionCategory() {
  const get = (): string | null => localStorage.getItem(STORAGE_KEY)
  const set = (id: string) => localStorage.setItem(STORAGE_KEY, id)
  return { getLastUsedId: get, setLastUsedId: set }
}
