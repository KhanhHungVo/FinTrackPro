import Keycloak from 'keycloak-js'
import { env } from '@/shared/config/env'
import type { AuthProfile, IAuthAdapter } from './IAuthAdapter'

class KeycloakAdapter implements IAuthAdapter {
  private readonly kc: Keycloak

  constructor() {
    this.kc = new Keycloak({
      url: env.KEYCLOAK_URL,
      realm: env.KEYCLOAK_REALM,
      clientId: env.KEYCLOAK_CLIENT_ID,
    })
  }

  async init(): Promise<AuthProfile> {
    await this.kc.init({ onLoad: 'login-required', pkceMethod: 'S256' })

    this.kc.onTokenExpired = () => {
      this.kc.updateToken(30).catch(() => this.kc.login())
    }

    return {
      displayName:
        this.kc.idTokenParsed?.name ??
        this.kc.idTokenParsed?.preferred_username ??
        '',
      email: this.kc.idTokenParsed?.email ?? '',
    }
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
}

export const keycloakAdapter = new KeycloakAdapter()
