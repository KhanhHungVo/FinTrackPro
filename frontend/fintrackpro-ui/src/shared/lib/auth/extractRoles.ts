export const ADMIN_ROLE = 'Admin'
export type AuthProviderName = 'keycloak' | 'auth0'

/**
 * Extract roles from a decoded JWT payload for the given provider.
 * Returns an empty array on any unexpected shape — never throws.
 */
export function extractRoles(
  payload: Record<string, unknown>,
  provider: AuthProviderName,
): string[] {
  if (provider === 'keycloak') {
    const ra = payload['realm_access']
    if (ra !== null && typeof ra === 'object' && !Array.isArray(ra)) {
      const roles = (ra as Record<string, unknown>)['roles']
      if (Array.isArray(roles)) return roles.filter((r): r is string => typeof r === 'string')
    }
    return []
  }
  // auth0: flat claims — WS-Federation URI takes precedence
  const flat =
    payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
    payload['roles'] ??
    payload['role']
  if (Array.isArray(flat)) return flat.filter((r): r is string => typeof r === 'string')
  if (typeof flat === 'string') return [flat]
  return []
}
