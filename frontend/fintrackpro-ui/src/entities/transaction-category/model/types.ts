import type { TransactionType } from '@/entities/transaction/model/types'

export interface TransactionCategory {
  id: string
  slug: string
  labelEn: string
  labelVi: string
  icon: string
  type: TransactionType
  isSystem: boolean
  sortOrder: number
}

export interface CreateTransactionCategoryPayload {
  type: TransactionType
  slug: string
  labelEn: string
  labelVi: string
  icon: string
}

export interface UpdateTransactionCategoryPayload {
  labelEn: string
  labelVi: string
  icon: string
}
