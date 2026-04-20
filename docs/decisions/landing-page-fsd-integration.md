# Landing Page — FSD Integration & Public Route Auth

## Context

The app previously redirected every visitor to Keycloak/Auth0 on first load, making it impossible to show a public marketing page at `/`. This document describes how the landing page was integrated into the React/Vite FSD codebase and how the auth layer was extended to support unauthenticated public routes.

---

## Key Decisions

### `check-sso` on public routes — no forced redirect

When `window.location.pathname === '/'`, `AuthProvider` passes `{ publicRoute: true }` to `authAdapter.init()`:

- **Keycloak:** uses `onLoad: 'check-sso'` with a `silentCheckSsoRedirectUri` pointing to `public/silent-check-sso.html`. This silently restores an existing session without ever redirecting the browser. Returns `null` if no session exists.
- **Auth0:** calls `isAuthenticated()` silently after SDK init. Does not call `loginWithRedirect()`. Returns `null` if unauthenticated.

On any other route the original `login-required` / `loginWithRedirect()` behaviour is preserved.

### Explicit `login()` method on adapters

The landing page CTA buttons trigger the IAM provider directly — `init()` is a one-shot SDK setup and must not be called again. Both adapters expose a `login(options?)` method:

- **Keycloak:** calls `this.kc.login()`. Keycloak does not support a native signup-hint; both login and signup flows use the same endpoint.
- **Auth0:** calls `this.client.loginWithRedirect({ authorizationParams: { screen_hint: 'signup' | undefined } })`.

`LoginOptions.screen` — `'login'` (default) or `'signup'` — lets callers distinguish the two flows.

### `RequireAuth` route guard

A thin `RequireAuth` layout wrapper reads `useAuthStore(s => s.isAuthenticated)`. When the user is not authenticated it renders `<Navigate to="/" replace />`. This replaces the previous implicit enforcement where `AuthProvider` itself forced a redirect — which is no longer safe now that `/` is a public route.

### Landing page sections are standalone — not shared with `/pricing`

Each visual section is a component under `pages/landing/ui/sections/`. The landing `PricingSection` reads limits from `env.*` and renders a purely static card; the authenticated `/pricing` page is subscription-aware and renders upgrade/manage flows. The two are intentionally separate.

### Pricing limits come from env vars with safe defaults

`src/shared/config/env.ts` exposes `FREE_*` and `PRO_*` numeric constants populated from `VITE_FREE_*` / `VITE_PRO_*` env vars. All defaults match the values enforced by the backend, so the landing page is accurate without any configuration.

---

## Architecture

```
AuthProvider
  ├── publicRoute=true  (path === '/')
  │     └── authAdapter.init({ publicRoute: true })
  │           ├── Keycloak → check-sso → null if no session
  │           └── Auth0   → isAuthenticated() → null if no session
  │     └── profile === null → setInitialized(true), no token set
  │
  └── publicRoute=false (all other paths)
        └── authAdapter.init() → login-required / loginWithRedirect (original)

App.tsx routes
  ├── <Route path="/"  element={<LandingPage />} />          ← public
  └── <Route element={<RequireAuth />}>                       ← guard
        └── <Route element={<AppLayout />}>
              ├── /dashboard, /transactions, /budgets, /trades
              ├── /market, /settings, /pricing, /about
              └── * → <NotFoundPage />

LandingPage
  ├── isAuthenticated → <Navigate to="/dashboard" replace />
  ├── handleLogin()  → authAdapter.login({ screen: 'login' })
  └── handleSignup() → authAdapter.login({ screen: 'signup' })
```

---

## Files Changed

| File | Action |
|------|--------|
| `src/shared/lib/auth/IAuthAdapter.ts` | Add `InitOptions`, `LoginOptions`, `login()` method; `init()` returns `AuthProfile \| null` |
| `src/shared/lib/auth/KeycloakAdapter.ts` | Add `check-sso` branch in `init()`; add `login()` |
| `src/shared/lib/auth/Auth0Adapter.ts` | Add silent-only branch in `init()`; add `login()` with `screen_hint` |
| `src/shared/lib/auth/index.ts` | Re-export `LoginOptions` |
| `src/app/providers/AuthProvider.tsx` | Pass `publicRoute`, handle `null` profile, suppress error on public provider failure |
| `src/app/App.tsx` | Add `/` route; wrap protected routes in `RequireAuth` |
| `src/shared/config/env.ts` | Add `FREE_*` / `PRO_*` pricing limit constants |
| `frontend/fintrackpro-ui/.env.example` | Document new pricing limit vars |
| `public/silent-check-sso.html` | Minimal Keycloak silent SSO iframe |
| `src/pages/landing/index.ts` | Barrel export |
| `src/pages/landing/ui/LandingPage.tsx` | Auth-aware root; composes all sections |
| `src/pages/landing/ui/sections/LandingNav.tsx` | Nav with Login / Sign-up CTAs |
| `src/pages/landing/ui/sections/HeroSection.tsx` | Hero with primary CTA |
| `src/pages/landing/ui/sections/PainPointsSection.tsx` | Problem framing |
| `src/pages/landing/ui/sections/DashboardMockupSection.tsx` | Product mockup |
| `src/pages/landing/ui/sections/OutcomeSpotlightsSection.tsx` | Outcome highlights |
| `src/pages/landing/ui/sections/FeaturesSection.tsx` | Feature grid |
| `src/pages/landing/ui/sections/PricingSection.tsx` | Static pricing cards (reads `env.*` limits) |
| `src/pages/landing/ui/sections/HowItWorksSection.tsx` | Steps overview |
| `src/pages/landing/ui/sections/LandingFooter.tsx` | Footer |
| `src/pages/landing/ui/LandingPage.module.css` | Scoped CSS design tokens + fade-up animation |
| `src/pages/landing/lib/useFadeUp.ts` | `IntersectionObserver` scroll-fade hook |

---

## Verification

| Scenario | Expected behaviour |
|----------|--------------------|
| Unauthenticated visit to `/` | Landing page renders; browser does **not** redirect to IAM provider |
| Authenticated visit to `/` | `<Navigate to="/dashboard" replace />` — landing page never renders |
| "Start for Free" / "Upgrade to Pro" click | `authAdapter.login({ screen: 'signup' })` → IAM registration screen |
| "Log in" click | `authAdapter.login({ screen: 'login' })` → IAM login screen |
| Unauthenticated direct visit to `/dashboard` | `RequireAuth` redirects to `/` |
| Authenticated visit to any protected route | Route renders normally |
| IAM provider unreachable on `/` | Landing page still renders; provider failure does not block the public route |
| `npm run build` | Passes with zero TypeScript errors |
