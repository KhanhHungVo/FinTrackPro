# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
npm run dev        # Start Vite dev server
npm run build      # Type-check + production build
npm run preview    # Preview production build
npm run lint       # Run ESLint
npm test           # Run Vitest unit tests
npm run test:e2e   # Run Playwright E2E tests (requires E2E_TOKEN — use scripts/e2e-local.sh)
```

Run a single Vitest file:
```bash
npx vitest run src/path/to/file.test.ts
```

Run E2E tests (from repo root — mints token automatically):  
See root `CLAUDE.md → E2E tests (Playwright)` for commands. Full prerequisites and troubleshooting: `docs/guides/dev-setup.md` (Mode E).

## Environment Setup

Copy `.env.example` to `.env` and set:
- `VITE_API_BASE_URL` — backend REST API base URL (`http://localhost:5018`)
- `VITE_AUTH_PROVIDER` — `"keycloak"` (default) or `"auth0"`
- Keycloak mode: `VITE_KEYCLOAK_URL`, `VITE_KEYCLOAK_REALM`, `VITE_KEYCLOAK_CLIENT_ID`
- Auth0 mode: `VITE_AUTH0_DOMAIN`, `VITE_AUTH0_CLIENT_ID`, `VITE_AUTH0_AUDIENCE`

Access env vars via `shared/config/env.ts` (never read `import.meta.env` directly).

## Architecture: Feature-Sliced Design (FSD)

The codebase follows strict FSD layering. Layers can only import from layers **below** them:

```
app → pages → widgets → features → entities → shared
```

| Layer | Purpose |
|-------|---------|
| `app/` | Router setup, global providers, global styles |
| `pages/` | Route-level components; compose widgets and features |
| `widgets/` | Self-contained composite UI blocks (navbar, charts) |
| `features/` | User interactions and use cases (forms, mutations) |
| `entities/` | Business domain types + React Query hooks for each entity |
| `shared/` | Reusable utilities: API client, env config, `cn()` helper, UI primitives (`Button`, `IconButton`, `Pagination`, `SortableColumnHeader`, `ConfirmDeleteDialog`, `AuthDegradedBanner`, `AuthErrorScreen`, `Tooltip`, `BottomSheet`, `TruncatedText`, `RowHoverCard`), hooks (`useClickOutside`, `useEscapeKey`, `useDebounce`, `useGuardedMutation`), i18n setup (EN / VI via i18next), icons (`AppSpinner`) |

## Key Patterns

**Adding a new entity** (e.g. `invoice`):
1. Types → `entities/invoice/model/types.ts`
2. React Query hooks → `entities/invoice/api/invoiceApi.ts`
3. Hooks follow the pattern: `useInvoices()` (GET), `useCreateInvoice()` (POST), `useDeleteInvoice()` (DELETE)
4. Mutations call `queryClient.invalidateQueries` on success to keep cache fresh

**Adding a new feature** (form/interaction):
1. Component → `features/[feature-name]/ui/[ComponentName].tsx`
2. Import entity hooks for data fetching/mutations
3. Keep local UI state in `useState`; no Zustand for form state

**Styling:**
- Use TailwindCSS v4 utility classes
- Use `cn()` from `shared/lib/cn.ts` to merge conditional class names

**Auth state** is managed by a Zustand store in `features/auth/`. The Axios instance in `shared/api/client.ts` automatically injects the Bearer token and triggers re-login via `authAdapter` on 401. The active adapter (`KeycloakAdapter` or `Auth0Adapter`) is selected at runtime from `VITE_AUTH_PROVIDER`.

**Path alias:** `@/` maps to `src/` — use it for all non-relative imports.
