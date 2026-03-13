import Keycloak from 'keycloak-js'
import { env } from '@/shared/config/env'

export const keycloak = new Keycloak({
  url: env.KEYCLOAK_URL,
  realm: env.KEYCLOAK_REALM,
  clientId: env.KEYCLOAK_CLIENT_ID,
})
