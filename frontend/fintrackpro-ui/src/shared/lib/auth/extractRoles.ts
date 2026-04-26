export const ADMIN_ROLE = 'Admin'
export type AuthProviderName = 'keycloak' | 'auth0'

const AUTH0_ROLES_CLAIM = 'https://fintrackpro.dev/roles'
const WS_ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

function normalizeRoles(value: unknown): string[] {
  if (Array.isArray(value)) return value.filter((r): r is string => typeof r === 'string')

  if (typeof value !== 'string') return []

  try {
    const parsed: unknown = JSON.parse(value)
    if (Array.isArray(parsed)) return parsed.filter((r): r is string => typeof r === 'string')
  } catch {
    // Plain string role claim, e.g. role: "Admin".
  }

  return [value]
}

/**
 * Extract roles from a decoded JWT payload for the given provider.
 * Returns an empty array on any unexpected shape; never throws.
 */
export function extractRoles(
  payload: Record<string, unknown>,
  provider: AuthProviderName,
): string[] {
  if (provider === 'keycloak') {
    const ra = payload['realm_access']
    if (ra !== null && typeof ra === 'object' && !Array.isArray(ra)) {
      const roles = (ra as Record<string, unknown>)['roles']
      return normalizeRoles(roles)
    }
    return []
  }

  // Auth0: prefer the custom namespaced claim injected by the Post-Login Action.
  const flat =
    payload[AUTH0_ROLES_CLAIM] ??
    payload[WS_ROLE_CLAIM] ??
    payload['roles'] ??
    payload['role']
  return normalizeRoles(flat)
}
