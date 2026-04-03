export type TransactionType = 'Income' | 'Expense'

export interface Transaction {
  id: string
  type: TransactionType
  amount: number
  currency: string
  rateToUsd: number
  category: string
  categoryId?: string
  note: string | null
  budgetMonth: string
  createdAt: string
}
