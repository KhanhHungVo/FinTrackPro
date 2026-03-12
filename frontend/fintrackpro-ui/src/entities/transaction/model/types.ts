export type TransactionType = 'Income' | 'Expense'

export interface Transaction {
  id: string
  type: TransactionType
  amount: number
  category: string
  note: string | null
  budgetMonth: string
  createdAt: string
}
