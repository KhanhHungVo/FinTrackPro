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
