export const env = {
  API_BASE_URL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000',
  // Active IAM provider: "keycloak" (default) | "auth0"
  AUTH_PROVIDER: import.meta.env.VITE_AUTH_PROVIDER ?? 'keycloak',
  // Keycloak (used when AUTH_PROVIDER=keycloak)
  KEYCLOAK_URL: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
  KEYCLOAK_REALM: import.meta.env.VITE_KEYCLOAK_REALM ?? 'fintrackpro',
  KEYCLOAK_CLIENT_ID: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'fintrackpro-spa',
  // Auth0 (used when AUTH_PROVIDER=auth0)
  AUTH0_DOMAIN: import.meta.env.VITE_AUTH0_DOMAIN ?? '',
  AUTH0_CLIENT_ID: import.meta.env.VITE_AUTH0_CLIENT_ID ?? '',
  AUTH0_AUDIENCE: import.meta.env.VITE_AUTH0_AUDIENCE ?? '',
  // Admin contact (bank transfer payment flow)
  ADMIN_TELEGRAM: import.meta.env.VITE_ADMIN_TELEGRAM ?? '',
  ADMIN_EMAIL: import.meta.env.VITE_ADMIN_EMAIL ?? '',
  // Bank transfer details
  BANK_NAME: import.meta.env.VITE_BANK_NAME ?? '',
  BANK_ACCOUNT_NUMBER: import.meta.env.VITE_BANK_ACCOUNT_NUMBER ?? '',
  BANK_ACCOUNT_NAME: import.meta.env.VITE_BANK_ACCOUNT_NAME ?? '',
  BANK_TRANSFER_AMOUNT: import.meta.env.VITE_BANK_TRANSFER_AMOUNT ?? '99000',
  BANK_QR_URL: import.meta.env.VITE_BANK_QR_URL ?? '',
  // Pricing tier limits — shown on the public landing page pricing section
  FREE_TRANSACTIONS_LIMIT: Number(import.meta.env.VITE_FREE_TRANSACTIONS_LIMIT ?? '50'),
  FREE_HISTORY_DAYS: Number(import.meta.env.VITE_FREE_HISTORY_DAYS ?? '60'),
  FREE_BUDGETS_LIMIT: Number(import.meta.env.VITE_FREE_BUDGETS_LIMIT ?? '3'),
  FREE_TRADES_LIMIT: Number(import.meta.env.VITE_FREE_TRADES_LIMIT ?? '20'),
  FREE_WATCHLIST_LIMIT: Number(import.meta.env.VITE_FREE_WATCHLIST_LIMIT ?? '1'),
  PRO_TRANSACTIONS_LIMIT: Number(import.meta.env.VITE_PRO_TRANSACTIONS_LIMIT ?? '500'),
  PRO_BUDGETS_LIMIT: Number(import.meta.env.VITE_PRO_BUDGETS_LIMIT ?? '20'),
  PRO_TRADES_LIMIT: Number(import.meta.env.VITE_PRO_TRADES_LIMIT ?? '200'),
  PRO_WATCHLIST_LIMIT: Number(import.meta.env.VITE_PRO_WATCHLIST_LIMIT ?? '20'),
} as const
