# FinTrackPro — Database Schema

Database: PostgreSQL (local Docker + Render production). SQL Server supported as optional provider. ORM: EF Core 10. Migrations assembly: `FinTrackPro.Infrastructure`.

> **Provider selection:** set `DatabaseProvider:Provider` to `"postgresql"` (default) or `"sqlserver"` (optional). The migrations are provider-agnostic — EF Core applies the correct column types for the active provider at runtime.
>
> **Production:** PostgreSQL 18 on Render free tier, provisioned by Terraform. See [render-deploy.md](../guides/render-deploy.md).

Column types below show PostgreSQL (production). SQL Server equivalents: `uuid` → `uniqueidentifier`, `character varying(n)` → `nvarchar(n)`, `boolean` → `bit`, `timestamp` → `datetime2`.

## Tables

### Users
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| Email | character varying(200) | nullable, INDEX |
| DisplayName | character varying(100) | NOT NULL |
| CreatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| UpdatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| IsActive | boolean | NOT NULL, DEFAULT true |
| PreferredLanguage | character varying(10) | NOT NULL, DEFAULT 'en' |
| PreferredCurrency | character varying(3) | NOT NULL, DEFAULT 'USD' |
| Plan | integer | NOT NULL, DEFAULT 0 (0=Free, 1=Pro) |
| PaymentCustomerId | character varying(100) | nullable, INDEX (`IX_AppUsers_PaymentCustomerId`) |
| PaymentSubscriptionId | character varying(100) | nullable |
| SubscriptionExpiresAt | timestamp | nullable |

> **Subscription fields** — added by migration `AddSubscriptionFieldsToAppUser`. All existing users default to `Plan = Free (0)`. `PaymentCustomerId` is indexed for fast webhook lookup. `PaymentCustomerId` is retained on `CancelSubscription()` so re-subscription reuses the same customer record.
>
> Plan state is the single source of truth for access control. Payment gateway webhooks are the only path that changes `Plan` — never a direct API call.

---

### UserIdentities
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| ExternalUserId | character varying(200) | NOT NULL |
| Provider | character varying(200) | NOT NULL |
| UserId | uuid | FK → Users, CASCADE DELETE |
| CreatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| — | — | UNIQUE INDEX (ExternalUserId, Provider) |

> One `AppUser` can have multiple `UserIdentity` rows — one per IAM provider (e.g., Keycloak + Auth0 Google). `UserContextMiddleware` resolves the local user via this table on every authenticated request.

---

### Transactions
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | FK → Users, CASCADE DELETE |
| Type | integer | NOT NULL (0=Income, 1=Expense) |
| Amount | numeric(18,2) | NOT NULL |
| Currency | character varying(3) | NOT NULL, DEFAULT 'USD' |
| RateToUsd | numeric(18,8) | NOT NULL, DEFAULT 1.0 |
| Category | character varying(100) | NOT NULL (slug from resolved `TransactionCategory`) |
| CategoryId | uuid | nullable, FK → TransactionCategories, SET NULL on DELETE |
| Note | character varying(500) | nullable |
| BudgetMonth | character varying(7) | NOT NULL (YYYY-MM) |
| CreatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| UpdatedAt | timestamp | NOT NULL, DEFAULT NOW() |

> `Currency` and `RateToUsd` are stored at creation time. `RateToUsd` = units of the record's currency per 1 USD. Display conversion: `displayAmount = (amount / rateToUsd) × preferredRateToUsd`.
> `Category` always holds the resolved slug from `TransactionCategory.Slug` for budget-matching compatibility. `CategoryId` is nullable — old rows have `NULL` (Phase 1 migration strategy).

---

### TransactionCategories
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | nullable, FK → Users, CASCADE DELETE |
| Type | integer | NOT NULL (0=Income, 1=Expense) |
| Slug | character varying(100) | NOT NULL |
| LabelEn | character varying(100) | NOT NULL |
| LabelVi | character varying(100) | NOT NULL |
| Icon | character varying(50) | NOT NULL |
| IsSystem | boolean | NOT NULL, DEFAULT false |
| IsActive | boolean | NOT NULL, DEFAULT true |
| SortOrder | integer | NOT NULL, DEFAULT 0 |
| CreatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| UpdatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| — | — | UNIQUE INDEX (UserId, Slug) |

> `UserId = NULL` = system category (globally unique slug). `UserId = <user>` = user-owned custom category (slug unique per user). System categories are seeded idempotently by `TransactionCategoryDataSeeder` on every startup — 5 Income + 12 Expense = 17 total. System categories cannot be updated or deleted (HTTP 403 via domain guard). Custom categories are soft-deleted (`IsActive = false`).

---

### Budgets
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | FK → Users, CASCADE DELETE |
| Category | character varying(100) | NOT NULL |
| LimitAmount | numeric(18,2) | NOT NULL |
| Currency | character varying(3) | NOT NULL, DEFAULT 'USD' |
| RateToUsd | numeric(18,8) | NOT NULL, DEFAULT 1.0 |
| Month | character varying(7) | NOT NULL (YYYY-MM) |
| CreatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| UpdatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| — | — | UNIQUE INDEX (UserId, Category, Month) |

---

### Trades
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | FK → Users, CASCADE DELETE |
| Symbol | character varying(20) | NOT NULL (uppercased) |
| Direction | integer | NOT NULL (0=Long, 1=Short) |
| Status | integer | NOT NULL, DEFAULT 1 (0=Open, 1=Closed); all pre-existing rows backfilled to Closed |
| EntryPrice | numeric(18,8) | NOT NULL |
| ExitPrice | numeric(18,8) | nullable — required by app validation when Status=Closed |
| CurrentPrice | numeric(18,8) | nullable — only meaningful when Status=Open |
| PositionSize | numeric(18,8) | NOT NULL |
| Fees | numeric(18,8) | NOT NULL |
| Currency | character varying(3) | NOT NULL, DEFAULT 'USD' |
| RateToUsd | numeric(18,8) | NOT NULL, DEFAULT 1.0 |
| Notes | character varying(1000) | nullable |
| CreatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| UpdatedAt | timestamp | NOT NULL, DEFAULT NOW() |

> `Result` (realized P&L) is a **computed property** on the entity: `(ExitPrice - EntryPrice) × PositionSize - Fees` for Long Closed trades (direction-aware). Not persisted.
> `UnrealizedResult` is computed from `CurrentPrice` for Open trades with a current price. Not persisted.
> Both are ignored in EF config via `builder.Ignore(...)`.

---

### WatchedSymbols
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | FK → Users, CASCADE DELETE |
| Symbol | character varying(20) | NOT NULL (uppercased) |
| CreatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| — | — | UNIQUE INDEX (UserId, Symbol) |

---

### Signals
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | FK → Users, CASCADE DELETE |
| Symbol | character varying(20) | NOT NULL |
| SignalType | integer | NOT NULL (enum: RsiOversold=0, RsiOverbought=1, VolumeSpike=2, FundingRate=3, EmaCross=4, BbSqueeze=5) |
| Message | character varying(500) | NOT NULL |
| Value | numeric(18,8) | |
| Timeframe | character varying(10) | |
| IsNotified | boolean | NOT NULL |
| CreatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| DismissedAt | timestamptz | NULL (set on user dismiss) |
| — | — | INDEX (UserId, CreatedAt) |
| — | — | PARTIAL INDEX (DismissedAt) WHERE DismissedAt IS NOT NULL — `IX_Signals_DismissedAt` |

---

### NotificationPreferences
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | FK → Users, CASCADE DELETE, UNIQUE INDEX |
| Channel | integer | NOT NULL (0=Telegram, 1=Email) |
| TelegramChatId | character varying(100) | nullable |
| Email | character varying(200) | nullable |
| IsEnabled | boolean | NOT NULL |
| CreatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| UpdatedAt | timestamp | NOT NULL, DEFAULT NOW() |

---

### BudgetAlertLogs
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | FK → Users, CASCADE DELETE |
| Category | character varying(100) | NOT NULL |
| Month | character varying(7) | NOT NULL (YYYY-MM) |
| CreatedAt | timestamp | NOT NULL, DEFAULT NOW() |
| — | — | UNIQUE INDEX (UserId, Category, Month) |

> Internal dedup marker used by `BudgetOverrunJob`. Records that an overrun alert was sent for a given user / category / month. Never exposed via API. The unique index prevents duplicate rows even under concurrent job runs.

---

## Running Migrations

Migrations are **applied automatically at startup** via `db.Database.MigrateAsync()` in `Program.cs`. No manual `dotnet ef database update` is needed in CI or production.

To generate a new migration after a schema change (developer action only):

```bash
# From backend/ directory
dotnet ef migrations add <MigrationName> \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API
```

For multi-instance deployments, run migrations before instances start using the `--migrate-only` flag:

```bash
dotnet run --project src/FinTrackPro.API -- --migrate-only
```

The active provider is determined by `DatabaseProvider:Provider` (`appsettings.json` or env var). Set via user-secrets for targeting production:

```bash
# DatabaseProvider:Provider is not sensitive — set as an env var or in appsettings.Development.json
export DatabaseProvider__Provider=postgresql

# Connection string contains credentials — use user-secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<external-url>" --project src/FinTrackPro.API
```

The external PostgreSQL URL is available via `terraform output -raw db_external_url` or the Render dashboard → Database → Connections.
