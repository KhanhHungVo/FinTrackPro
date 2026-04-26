import Keycloak from 'keycloak-js'
import { env } from '@/shared/config/env'
import type { AuthProfile, IAuthAdapter, InitOptions, LoginOptions } from './IAuthAdapter'
import { extractRoles } from './extractRoles'

class KeycloakAdapter implements IAuthAdapter {
  private readonly kc: Keycloak
  private cachedPayload: Record<string, unknown> | null = null

  constructor() {
    this.kc = new Keycloak({
      url: env.KEYCLOAK_URL,
      realm: env.KEYCLOAK_REALM,
      clientId: env.KEYCLOAK_CLIENT_ID,
    })
  }

  async init(options?: InitOptions): Promise<AuthProfile | null> {
    if (options?.publicRoute) {
      // check-sso: silently restore session if one exists; never redirects
      await this.kc.init({
        onLoad: 'check-sso',
        silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
        pkceMethod: 'S256',
      })
      if (!this.kc.authenticated) return null
    } else {
      await this.kc.init({ onLoad: 'login-required', pkceMethod: 'S256' })
    }

    this.kc.onTokenExpired = () => {
      this.kc.updateToken(30).catch(() => this.kc.login())
    }

    this.cachedPayload = (this.kc.tokenParsed as Record<string, unknown>) ?? null

    return {
      displayName:
        this.kc.idTokenParsed?.name ??
        this.kc.idTokenParsed?.preferred_username ??
        '',
      email: this.kc.idTokenParsed?.email ?? '',
    }
  }

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  login(_options?: LoginOptions): void {
    // Keycloak does not support a native signup-hint; both flows use the same login endpoint
    this.kc.login()
  }

  async getToken(): Promise<string> {
    if (!this.kc.token) throw new Error('Not authenticated')
    return this.kc.token
  }

  async refreshToken(): Promise<string> {
    await this.kc.updateToken(0)
    if (!this.kc.token) throw new Error('Token refresh failed')
    return this.kc.token
  }

  logout(redirectUri?: string): void {
    this.kc.logout({ redirectUri: redirectUri ?? window.location.origin })
  }

  getRoles(): string[] {
    if (!this.cachedPayload) return []
    return extractRoles(this.cachedPayload, 'keycloak')
  }
}

export const keycloakAdapter = new KeycloakAdapter()
