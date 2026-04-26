import { describe, it, expect } from 'vitest'
import { extractRoles, ADMIN_ROLE } from './extractRoles'

const WS_ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

describe('extractRoles — keycloak', () => {
  it('returns roles from realm_access.roles', () => {
    const payload = { realm_access: { roles: ['User', 'Admin'] } }
    expect(extractRoles(payload, 'keycloak')).toEqual(['User', 'Admin'])
  })

  it('includes ADMIN_ROLE when present', () => {
    const payload = { realm_access: { roles: ['User', 'Admin'] } }
    expect(extractRoles(payload, 'keycloak').includes(ADMIN_ROLE)).toBe(true)
  })

  it('returns [] when realm_access has no roles key', () => {
    expect(extractRoles({ realm_access: {} }, 'keycloak')).toEqual([])
  })

  it('returns [] when realm_access.roles is empty', () => {
    expect(extractRoles({ realm_access: { roles: [] } }, 'keycloak')).toEqual([])
  })

  it('returns [] when realm_access is absent', () => {
    expect(extractRoles({}, 'keycloak')).toEqual([])
  })

  it('filters out non-string values from roles array', () => {
    const payload = { realm_access: { roles: ['Admin', 42, null, true] } }
    expect(extractRoles(payload, 'keycloak')).toEqual(['Admin'])
  })

  it('returns [] when realm_access is a string (malformed JWT)', () => {
    expect(extractRoles({ realm_access: 'bad' }, 'keycloak')).toEqual([])
  })

  it('returns [] when realm_access is null', () => {
    expect(extractRoles({ realm_access: null }, 'keycloak')).toEqual([])
  })

  it('returns [] when realm_access is an array (malformed)', () => {
    expect(extractRoles({ realm_access: ['Admin'] }, 'keycloak')).toEqual([])
  })
})

describe('extractRoles — auth0', () => {
  it('reads roles from WS-Federation URI claim', () => {
    const payload = { [WS_ROLE_CLAIM]: ['Admin'] }
    expect(extractRoles(payload, 'auth0')).toEqual(['Admin'])
  })

  it('reads roles from flat roles array', () => {
    const payload = { roles: ['User', 'Admin'] }
    expect(extractRoles(payload, 'auth0')).toEqual(['User', 'Admin'])
  })

  it('wraps single string role in array', () => {
    expect(extractRoles({ role: 'Admin' }, 'auth0')).toEqual(['Admin'])
  })

  it('returns [] when no role claims present', () => {
    expect(extractRoles({}, 'auth0')).toEqual([])
  })

  it('returns [] when roles is an empty array', () => {
    expect(extractRoles({ roles: [] }, 'auth0')).toEqual([])
  })

  it('WS-Federation claim takes precedence over roles', () => {
    const payload = { [WS_ROLE_CLAIM]: ['Admin'], roles: ['User'] }
    expect(extractRoles(payload, 'auth0')).toEqual(['Admin'])
  })

  it('filters out non-string values from roles array', () => {
    const payload = { roles: ['Admin', 99, null] }
    expect(extractRoles(payload, 'auth0')).toEqual(['Admin'])
  })
})

describe('extractRoles — both providers empty payload', () => {
  it('keycloak: empty payload → []', () => {
    expect(extractRoles({}, 'keycloak')).toEqual([])
  })

  it('auth0: empty payload → []', () => {
    expect(extractRoles({}, 'auth0')).toEqual([])
  })
})
