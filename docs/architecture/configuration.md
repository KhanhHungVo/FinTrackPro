# Configuration Reference

Complete reference for every key in `appsettings.json` (backend) and every `VITE_*` variable in `frontend/fintrackpro-ui/.env` (frontend). Sensitive values must never be committed — use `dotnet user-secrets` locally for the backend and environment variables in production for both.

## Database

| Key | Default | Required | Notes |
|---|---|---|---|
| `DatabaseProvider:Provider` | `"postgresql"` | Yes | `"postgresql"` or `"sqlserver"` |
| `ConnectionStrings:DefaultConnection` | (local PG) | Yes | Full ADO.NET connection string for the selected provider |

## Identity Provider

| Key | Default | Required | Notes |
|---|---|---|---|
| `IdentityProvider:Provider` | `"keycloak"` | Yes | `"keycloak"` or `"auth0"` — selects IAM adapter at startup |
| `IdentityProvider:Audience` | `"https://api.fintrackpro.dev"` | Yes | Expected `aud` claim in every JWT |
| `IdentityProvider:AdminClientId` | `"fintrackpro-api"` | Yes* | M2M client ID used to call the IAM admin API (*only needed for `IamUserSyncJob`) |
| `IdentityProvider:AdminClientSecret` | `""` | No† | M2M client secret — †only needed for `IamUserSyncJob`; use secrets manager, never commit |

### Keycloak (when `Provider = "keycloak"`)

| Key | Default | Required | Notes |
|---|---|---|---|
| `Keycloak:Authority` | `"http://localhost:8080/realms/fintrackpro"` | Yes | Validates the `iss` claim in tokens |
| `Keycloak:MetadataAddress` | `"http://localhost:8080/realms/fintrackpro/.well-known/openid-configuration"` | Yes | Where the API fetches JWKS signing keys; override in Docker to use container hostname |

### Auth0 (when `Provider = "auth0"`)

| Key | Default | Required | Notes |
|---|---|---|---|
| `Auth0:Domain` | `""` | Yes | Auth0 tenant domain, e.g. `"your-tenant.us.auth0.com"` |

## External Services

### Telegram

| Key | Default | Required | Notes |
|---|---|---|---|
| `Telegram:BotToken` | (placeholder) | No | If absent or placeholder, a no-op notification channel is registered and startup succeeds |

### Binance

| Key | Default | Required | Notes |
|---|---|---|---|
| `Binance:BaseUrl` | `"https://api.binance.com"` | Yes | Override for staging/testing |

### CoinGecko

| Key | Default | Required | Notes |
|---|---|---|---|
| `CoinGecko:BaseUrl` | `"https://api.coingecko.com"` | Yes | Override for staging/testing |
| `CoinGecko:ApiKey` | `""` | No† | †Required only for the `/market/trending` endpoint (Demo or Pro key) |

### Fear & Greed Index

| Key | Default | Required | Notes |
|---|---|---|---|
| `FearGreed:BaseUrl` | `"https://api.alternative.me"` | Yes | Override for staging/testing |

### ExchangeRate-API

| Key | Default | Required | Notes |
|---|---|---|---|
| `ExchangeRate:BaseUrl` | `"https://v6.exchangerate-api.com/v6/"` | Yes | Override for staging/testing |
| `ExchangeRate:ApiKey` | `""` | No† | †Required for live fiat rate sync; if absent, `FallbackRates` are used |
| `ExchangeRate:SupportedCurrencies` | `["USD","VND"]` | Yes | Currencies the app can convert between |
| `ExchangeRate:PreloadCurrencies` | `["USD","VND"]` | Yes | Currencies pre-loaded into cache on startup |
| `ExchangeRate:FallbackRates:USD` | `1` | Yes | Static fallback rate when API key is absent |
| `ExchangeRate:FallbackRates:VND` | `26000` | Yes | Static fallback rate when API key is absent |

## Payment Gateway

| Key | Default | Required | Notes |
|---|---|---|---|
| `PaymentGateway:Provider` | `"stripe"` | Yes | Payment provider; only `"stripe"` is currently implemented |
| `PaymentGateway:PriceId` | `""` | Yes* | Provider-neutral Pro plan price identifier (*required to initiate checkout) |
| `Stripe:SecretKey` | `""` | Yes* | Stripe API secret key (*required for payment flows); use secrets manager |
| `Stripe:WebhookSecret` | `""` | Yes* | Stripe webhook endpoint signing secret (*required to verify webhook events); use secrets manager |

## Subscription Plans

Plan limits are enforced by `SubscriptionLimitService`. A value of `-1` means unlimited.

| Key | Free default | Pro default | Notes |
|---|---|---|---|
| `SubscriptionPlans:{tier}:MonthlyTransactionLimit` | `50` | `-1` | Max transactions per calendar month |
| `SubscriptionPlans:{tier}:TransactionHistoryDays` | `60` | `-1` | How far back the user can query transactions |
| `SubscriptionPlans:{tier}:ActiveBudgetLimit` | `3` | `-1` | Max active budgets at once |
| `SubscriptionPlans:{tier}:TotalTradeLimit` | `20` | `-1` | Max total trade records |
| `SubscriptionPlans:{tier}:WatchlistSymbolLimit` | `1` | `-1` | Max watchlist symbols |
| `SubscriptionPlans:{tier}:SignalHistoryDays` | `7` | `-1` | How far back the user can query market signals |
| `SubscriptionPlans:{tier}:TelegramNotificationsEnabled` | `false` | `true` | Whether Telegram push notifications are sent for this tier |

## CORS

| Key | Default | Required | Notes |
|---|---|---|---|
| `Cors:Origins` | `"http://localhost:5173"` | Yes | Comma-separated allowed origins; set to the frontend URL in production |

## Hangfire Dashboard

| Key | Default | Required | Notes |
|---|---|---|---|
| `Hangfire:Username` | `"hangfire-admin"` | Yes | Basic-auth username for `/hangfire` (Admin role required) |
| `Hangfire:Password` | `""` | Yes | Basic-auth password; use secrets manager |

## Observability

| Key | Default | Required | Notes |
|---|---|---|---|
| `LoggingBehavior:SlowHandlerThresholdMs` | `500` | No | MediatR handlers exceeding this threshold emit a `Warning` log |
| `HttpLogging:MaskSensitiveData` | `true` | No | Masks sensitive fields in HTTP request/response logs |
| `Serilog:MinimumLevel:Default` | `"Information"` | No | Root log level |
| `Serilog:MinimumLevel:Override:Microsoft` | `"Warning"` | No | Suppresses verbose ASP.NET Core framework logs |
| `Serilog:MinimumLevel:Override:System` | `"Warning"` | No | Suppresses verbose System logs |

## HTTP Resilience (Polly)

Applied to all outbound HTTP clients (Binance, CoinGecko, FearGreed, ExchangeRate).

| Key | Default | Notes |
|---|---|---|
| `HttpResilience:RetryCount` | `3` | Number of retry attempts on transient failure |
| `HttpResilience:RetryBaseDelayMs` | `500` | Base delay for exponential back-off (ms) |
| `HttpResilience:TimeoutSeconds` | `30` | Per-request timeout |
| `HttpResilience:CircuitBreakerFailurePercent` | `50` | Failure rate (%) that trips the circuit breaker |
| `HttpResilience:CircuitBreakerBreakDurationSeconds` | `30` | How long the circuit stays open after tripping |
| `HttpResilience:CircuitBreakerSamplingDurationSeconds` | `60` | Rolling window used to measure failure rate |
| `HttpResilience:CircuitBreakerMinimumThroughput` | `5` | Minimum requests in the window before the breaker can trip |

## Frontend Environment (`.env`)

Copy `frontend/fintrackpro-ui/.env.example` → `.env` before first run. Access all vars via `shared/config/env.ts` — never read `import.meta.env` directly.

### Core

| Variable | Default | Required | Notes |
|---|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5018` | Yes | Backend REST API base URL |
| `VITE_AUTH_PROVIDER` | `"keycloak"` | Yes | `"keycloak"` or `"auth0"` — selects the auth adapter at runtime |

### Keycloak (when `VITE_AUTH_PROVIDER=keycloak`)

| Variable | Example | Required | Notes |
|---|---|---|---|
| `VITE_KEYCLOAK_URL` | `http://localhost:8080` | Yes | Keycloak server base URL |
| `VITE_KEYCLOAK_REALM` | `fintrackpro` | Yes | Realm name |
| `VITE_KEYCLOAK_CLIENT_ID` | `fintrackpro-spa` | Yes | Public SPA client ID |

### Auth0 (when `VITE_AUTH_PROVIDER=auth0`)

| Variable | Example | Required | Notes |
|---|---|---|---|
| `VITE_AUTH0_DOMAIN` | `your-tenant.us.auth0.com` | Yes | Auth0 tenant domain |
| `VITE_AUTH0_CLIENT_ID` | `your-spa-client-id` | Yes | SPA application client ID |
| `VITE_AUTH0_AUDIENCE` | `https://api.fintrackpro.dev` | Yes | API identifier — must match `IdentityProvider:Audience` on the backend |

### Payment / Bank Transfer UI

| Variable | Default | Required | Notes |
|---|---|---|---|
| `VITE_BANK_NAME` | `Techcombank` | Yes | Bank name shown in transfer modal |
| `VITE_BANK_ACCOUNT_NUMBER` | — | Yes | Account number shown in transfer modal |
| `VITE_BANK_ACCOUNT_NAME` | — | Yes | Account holder name shown in transfer modal |
| `VITE_BANK_TRANSFER_AMOUNT` | `99000` | Yes | Monthly Pro price in VND |
| `VITE_BANK_QR_URL` | `""` | No | Externally hosted QR code image URL; leave empty to hide QR |

### Admin Contact

| Variable | Example | Required | Notes |
|---|---|---|---|
| `VITE_ADMIN_TELEGRAM` | `your_telegram_handle` | No | Telegram handle shown in bank transfer modal |
| `VITE_ADMIN_EMAIL` | `admin@fintrackpro.dev` | No | Admin email shown in bank transfer modal |
