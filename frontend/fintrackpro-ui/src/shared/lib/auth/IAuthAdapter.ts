export interface AuthProfile {
  displayName: string
  email: string
}

export interface IAuthAdapter {
  /** Initialize the provider SDK and redirect to login if unauthenticated. */
  init(): Promise<AuthProfile>
  /** Return the current valid access token (refresh silently if expired). */
  getToken(): Promise<string>
  /** Force a silent token refresh; return the new token or throw. */
  refreshToken(): Promise<string>
  /** Trigger provider logout and redirect. */
  logout(redirectUri?: string): void
}
