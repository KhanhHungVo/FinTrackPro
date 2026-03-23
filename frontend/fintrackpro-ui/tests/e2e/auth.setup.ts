/**
 * E2E auth setup helpers.
 *
 * These tests bypass the real IAM login by injecting a pre-issued JWT directly
 * into localStorage and the Zustand authStore before each test. This is only
 * safe in dev/test environments.
 *
 * Required env vars:
 *   E2E_TOKEN  — a valid JWT for the running API (copy from browser devtools after login)
 *   E2E_BASE_URL — (optional) default is http://localhost:5173
 *
 * How to get a token for local dev (Keycloak):
 *   curl -s -X POST http://localhost:8080/realms/fintrackpro/protocol/openid-connect/token \
 *     -d "grant_type=password" -d "client_id=fintrackpro-e2e" \
 *     -d "username=admin@fintrackpro.dev" -d "password=Admin1234!" \
 *     | jq -r .access_token
 */

import type { Page } from '@playwright/test'

export async function injectAuthToken(page: Page) {
  const token = process.env.E2E_TOKEN
  if (!token) {
    throw new Error(
      'E2E_TOKEN env var is required. ' +
        'Set it to a valid JWT before running e2e tests.\n' +
        'Example: E2E_TOKEN=$(curl ...) npx playwright test',
    )
  }

  // Inject token + bypass flag into localStorage BEFORE any page script runs.
  // AuthProvider checks e2e_bypass on mount and skips the Keycloak SDK init,
  // going straight to degraded mode with the cached token instead of redirecting.
  await page.addInitScript((t) => {
    localStorage.setItem('access_token', t)
    localStorage.setItem('e2e_bypass', '1')
  }, token)
}
