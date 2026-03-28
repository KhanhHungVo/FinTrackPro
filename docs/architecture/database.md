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
| CreatedAt | timestamp | NOT NULL |
| IsActive | boolean | NOT NULL, DEFAULT true |

---

### UserIdentities
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| ExternalUserId | character varying(200) | NOT NULL |
| Provider | character varying(200) | NOT NULL |
| UserId | uuid | FK → Users, CASCADE DELETE |
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
| Category | character varying(100) | NOT NULL |
| Note | character varying(500) | nullable |
| BudgetMonth | character varying(7) | NOT NULL (YYYY-MM) |
| CreatedAt | timestamp | NOT NULL |

---

### Budgets
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | FK → Users, CASCADE DELETE |
| Category | character varying(100) | NOT NULL |
| LimitAmount | numeric(18,2) | NOT NULL |
| Month | character varying(7) | NOT NULL (YYYY-MM) |
| CreatedAt | timestamp | NOT NULL |
| — | — | UNIQUE INDEX (UserId, Category, Month) |

---

### Trades
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | FK → Users, CASCADE DELETE |
| Symbol | character varying(20) | NOT NULL (uppercased) |
| Direction | integer | NOT NULL (0=Long, 1=Short) |
| EntryPrice | numeric(18,8) | NOT NULL |
| ExitPrice | numeric(18,8) | NOT NULL |
| PositionSize | numeric(18,8) | NOT NULL |
| Fees | numeric(18,8) | NOT NULL |
| Notes | character varying(1000) | nullable |
| CreatedAt | timestamp | NOT NULL |

> `Result` (P&L) is a **computed property** on the entity: `(ExitPrice - EntryPrice) × PositionSize - Fees`. Not persisted. EF config has `builder.Ignore(t => t.Result)`.

---

### WatchedSymbols
| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| UserId | uuid | FK → Users, CASCADE DELETE |
| Symbol | character varying(20) | NOT NULL (uppercased) |
| CreatedAt | timestamp | NOT NULL |
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
| CreatedAt | timestamp | NOT NULL |
| — | — | INDEX (UserId, CreatedAt) |

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
| CreatedAt | timestamp | NOT NULL |

---

## Running Migrations

```bash
# From backend/ directory
dotnet ef migrations add <MigrationName> \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API

dotnet ef database update \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API
```

The active provider is determined by `DatabaseProvider:Provider` (`appsettings.json` or env var). Set via user-secrets for targeting production:

```bash
# DatabaseProvider:Provider is not sensitive — set as an env var or in appsettings.Development.json
export DatabaseProvider__Provider=postgresql

# Connection string contains credentials — use user-secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<external-url>" --project src/FinTrackPro.API
```

The external PostgreSQL URL is available via `terraform output -raw db_external_url` or the Render dashboard → Database → Connections.
