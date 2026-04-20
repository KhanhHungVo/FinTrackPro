import { env } from '@/shared/config/env'
import { auth0Adapter } from './Auth0Adapter'
import { keycloakAdapter } from './KeycloakAdapter'
import type { IAuthAdapter, AuthProfile, LoginOptions } from './IAuthAdapter'

export const authAdapter: IAuthAdapter =
  env.AUTH_PROVIDER === 'auth0' ? auth0Adapter : keycloakAdapter

export type { IAuthAdapter, AuthProfile, LoginOptions }
