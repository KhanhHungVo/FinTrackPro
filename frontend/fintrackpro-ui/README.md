# FinTrackPro UI

React 19 single-page application for FinTrackPro, structured with Feature-Sliced Design.

## Stack

| Component | Technology |
|---|---|
| UI framework | React 19 + TypeScript |
| Build tool | Vite 7 |
| Architecture | Feature-Sliced Design (FSD) |
| Styling | TailwindCSS v4 |
| Server state | TanStack React Query v5 |
| Client state | Zustand v5 |
| Routing | React Router v7 |
| Validation | Zod v4 |
| Charts | Recharts v3 |
| HTTP client | Axios |

## Prerequisites

- Node.js 22+
- npm 10+
- FinTrackPro API running on `http://localhost:5018`
- Keycloak on `http://localhost:8080` (Keycloak mode) **or** an Auth0 tenant (Auth0 mode)

## Getting Started

```bash
# Install dependencies
npm install

# Copy environment file and fill in values
cp .env.example .env

# Start development server
npm run dev
```

App runs at `http://localhost:5173`.

## Environment Variables

Create a `.env` file in this directory (copy from `.env.example`):

| Variable | Description | Example |
|---|---|---|
| `VITE_API_BASE_URL` | Base URL of the FinTrackPro API | `http://localhost:5018` |
| `VITE_AUTH_PROVIDER` | Active auth provider | `keycloak` or `auth0` |
| `VITE_KEYCLOAK_URL` | Keycloak server URL *(Keycloak mode only)* | `http://localhost:8080` |
| `VITE_KEYCLOAK_REALM` | Keycloak realm name *(Keycloak mode only)* | `fintrackpro` |
| `VITE_KEYCLOAK_CLIENT_ID` | Keycloak public client ID *(Keycloak mode only)* | `fintrackpro-spa` |
| `VITE_AUTH0_DOMAIN` | Auth0 tenant domain *(Auth0 mode only)* | `your-tenant.auth0.com` |
| `VITE_AUTH0_CLIENT_ID` | Auth0 SPA client ID *(Auth0 mode only)* | `abc123` |
| `VITE_AUTH0_AUDIENCE` | Auth0 API audience *(Auth0 mode only)* | `https://api.fintrackpro.dev` |

> All variables must be prefixed with `VITE_` to be accessible in the browser bundle.

## Feature-Sliced Design Layers

```
src/
├── app/        # App-level setup: providers, router, global styles
├── pages/      # Route-level page components
├── widgets/    # Composite UI blocks composed from features/entities
├── features/   # User interactions and use cases (e.g. add-transaction, filter-budget)
├── entities/   # Business objects and their UI (e.g. transaction, budget, account)
└── shared/     # Reusable UI kit, API client, utilities, types
```

| Layer | Depends on | Example |
|---|---|---|
| `app` | all layers | Router, QueryClientProvider |
| `pages` | widgets, features, entities, shared | DashboardPage, TransactionsPage |
| `widgets` | features, entities, shared | TransactionTable, BudgetSummary, TrendingCoinsWidget |
| `features` | entities, shared | AddTransactionForm, ExportButton |
| `entities` | shared | TransactionCard, BudgetProgress |
| `shared` | nothing above | Button, apiClient, formatCurrency |

## Commands

```bash
npm run dev        # Start dev server (hot reload)
npm run build      # Type-check + production build
npm run preview    # Preview production build locally
npm run lint       # Run ESLint
npm test           # Run Vitest unit tests
npm run test:e2e   # Run Playwright E2E tests (requires E2E_TOKEN env var — use scripts/e2e-local.sh)
```

### Running E2E tests locally

The Playwright suite requires a JWT minted from the `fintrackpro-e2e` Keycloak client. Use the
helper script from the repo root instead of calling `npm run test:e2e` directly:

```bash
# From repo root — mints token and runs all specs
bash scripts/e2e-local.sh

# Playwright UI mode
bash scripts/e2e-local.sh --ui

# Single spec
bash scripts/e2e-local.sh tests/e2e/budgets.spec.ts
```

See [docs/dev-setup.md — Mode E](../../docs/dev-setup.md#mode-e--running-playwright-e2e-tests-locally) for prerequisites and troubleshooting.

## Further Reading

- [../../docs/api-spec.md](../../docs/api-spec.md) — API endpoints consumed by this app
- [../../docs/architecture.md](../../docs/architecture.md) — Full system architecture
