export interface AuthProfile {
  displayName: string
  email: string
}

export interface InitOptions {
  /** When true, do not force a login redirect — allow unauthenticated access. */
  publicRoute?: boolean
}

export interface LoginOptions {
  /** Hint the provider to show the registration screen ('signup') or login screen ('login'). */
  screen?: 'login' | 'signup'
}

export interface IAuthAdapter {
  /**
   * Initialize the provider SDK.
   * - publicRoute=false (default): redirect to login if unauthenticated.
   * - publicRoute=true: check session silently; return null if unauthenticated.
   */
  init(options?: InitOptions): Promise<AuthProfile | null>
  /**
   * Redirect to the IAM provider login or signup screen.
   * Must only be called after init() has completed (guaranteed by AuthProvider).
   */
  login(options?: LoginOptions): void
  /** Return the current valid access token (refresh silently if expired). */
  getToken(): Promise<string>
  /** Force a silent token refresh; return the new token or throw. */
  refreshToken(): Promise<string>
  /** Trigger provider logout and redirect. */
  logout(redirectUri?: string): void
}
