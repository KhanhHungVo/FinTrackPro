export const env = {
  API_BASE_URL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000',
  KEYCLOAK_URL: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
  KEYCLOAK_REALM: import.meta.env.VITE_KEYCLOAK_REALM ?? 'fintrackpro',
  KEYCLOAK_CLIENT_ID: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'fintrackpro-spa',
} as const
