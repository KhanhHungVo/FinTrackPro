export type SubscriptionPlan = 'Free' | 'Pro'

export interface SubscriptionStatus {
  plan: SubscriptionPlan
  isActive: boolean
  expiresAt: string | null
}

export interface CreateCheckoutSessionPayload {
  successUrl: string
  cancelUrl: string
}

export interface CreatePortalSessionPayload {
  returnUrl: string
}

export interface AdminUser {
  id: string
  email: string | null
  displayName: string
  plan: SubscriptionPlan
  subscriptionExpiresAt: string | null
  isActive: boolean
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export type BillingPeriod = 'Monthly' | 'Yearly'

export interface AdminActivatePayload {
  period: BillingPeriod
}
