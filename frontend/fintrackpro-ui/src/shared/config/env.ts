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
} as const
