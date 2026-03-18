import { createAuth0Client, type Auth0Client } from '@auth0/auth0-spa-js'
import { env } from '@/shared/config/env'
import type { AuthProfile, IAuthAdapter } from './IAuthAdapter'

class Auth0Adapter implements IAuthAdapter {
  private client: Auth0Client | null = null

  async init(): Promise<AuthProfile> {
    this.client = await createAuth0Client({
      domain: env.AUTH0_DOMAIN,
      clientId: env.AUTH0_CLIENT_ID,
      authorizationParams: {
        redirect_uri: window.location.origin,
        audience: env.AUTH0_AUDIENCE,
      },
    })

    // Handle the redirect callback after Auth0 redirects back to the app
    if (
      window.location.search.includes('code=') &&
      window.location.search.includes('state=')
    ) {
      await this.client.handleRedirectCallback()
      window.history.replaceState({}, document.title, window.location.pathname)
    }

    const isAuthenticated = await this.client.isAuthenticated()
    if (!isAuthenticated) {
      await this.client.loginWithRedirect()
      // loginWithRedirect triggers a full page redirect — execution stops here
      return new Promise(() => {}) as Promise<AuthProfile>
    }

    const user = await this.client.getUser()
    return {
      displayName: user?.name ?? user?.email ?? '',
      email: user?.email ?? '',
    }
  }

  async getToken(): Promise<string> {
    if (!this.client) throw new Error('Auth0 not initialized')
    return this.client.getTokenSilently()
  }

  async refreshToken(): Promise<string> {
    if (!this.client) throw new Error('Auth0 not initialized')
    return this.client.getTokenSilently({ cacheMode: 'off' })
  }

  logout(redirectUri?: string): void {
    this.client?.logout({
      logoutParams: { returnTo: redirectUri ?? window.location.origin },
    })
  }
}

export const auth0Adapter = new Auth0Adapter()
