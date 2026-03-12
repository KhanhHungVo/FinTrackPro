# FinTrackPro — Database Schema

Database: SQL Server. ORM: EF Core 10. Migrations assembly: `FinTrackPro.Infrastructure`.

## Tables

### Users
| Column | Type | Constraints |
|---|---|---|
| Id | uniqueidentifier | PK |
| KeycloakUserId | nvarchar(200) | NOT NULL, UNIQUE INDEX |
| Email | nvarchar(200) | NOT NULL, UNIQUE INDEX |
| DisplayName | nvarchar(100) | NOT NULL |
| Provider | nvarchar(50) | NOT NULL |
| CreatedAt | datetime2 | NOT NULL |

---

### Transactions
| Column | Type | Constraints |
|---|---|---|
| Id | uniqueidentifier | PK |
| UserId | uniqueidentifier | FK → Users, CASCADE DELETE |
| Type | int | NOT NULL (0=Income, 1=Expense) |
| Amount | decimal(18,2) | NOT NULL |
| Category | nvarchar(100) | NOT NULL |
| Note | nvarchar(500) | nullable |
| BudgetMonth | nvarchar(7) | NOT NULL (YYYY-MM) |
| CreatedAt | datetime2 | NOT NULL |

---

### Budgets
| Column | Type | Constraints |
|---|---|---|
| Id | uniqueidentifier | PK |
| UserId | uniqueidentifier | FK → Users, CASCADE DELETE |
| Category | nvarchar(100) | NOT NULL |
| LimitAmount | decimal(18,2) | NOT NULL |
| Month | nvarchar(7) | NOT NULL (YYYY-MM) |
| CreatedAt | datetime2 | NOT NULL |
| — | — | UNIQUE INDEX (UserId, Category, Month) |

---

### Trades
| Column | Type | Constraints |
|---|---|---|
| Id | uniqueidentifier | PK |
| UserId | uniqueidentifier | FK → Users, CASCADE DELETE |
| Symbol | nvarchar(20) | NOT NULL (uppercased) |
| Direction | int | NOT NULL (0=Long, 1=Short) |
| EntryPrice | decimal(18,8) | NOT NULL |
| ExitPrice | decimal(18,8) | NOT NULL |
| PositionSize | decimal(18,8) | NOT NULL |
| Fees | decimal(18,8) | NOT NULL |
| Notes | nvarchar(1000) | nullable |
| CreatedAt | datetime2 | NOT NULL |

> `Result` (P&L) is a **computed property** on the entity: `(ExitPrice - EntryPrice) × PositionSize - Fees`. Not persisted. EF config has `builder.Ignore(t => t.Result)`.

---

### WatchedSymbols
| Column | Type | Constraints |
|---|---|---|
| Id | uniqueidentifier | PK |
| UserId | uniqueidentifier | FK → Users, CASCADE DELETE |
| Symbol | nvarchar(20) | NOT NULL (uppercased) |
| CreatedAt | datetime2 | NOT NULL |
| — | — | UNIQUE INDEX (UserId, Symbol) |

---

### Signals
| Column | Type | Constraints |
|---|---|---|
| Id | uniqueidentifier | PK |
| UserId | uniqueidentifier | FK → Users, CASCADE DELETE |
| Symbol | nvarchar(20) | NOT NULL |
| SignalType | int | NOT NULL (enum: RsiOversold=0, RsiOverbought=1, VolumeSpike=2, FundingRate=3, EmaCross=4, BbSqueeze=5) |
| Message | nvarchar(500) | NOT NULL |
| Value | decimal(18,8) | |
| Timeframe | nvarchar(10) | |
| IsNotified | bit | NOT NULL |
| CreatedAt | datetime2 | NOT NULL |
| — | — | INDEX (UserId, CreatedAt) |

---

### NotificationPreferences
| Column | Type | Constraints |
|---|---|---|
| Id | uniqueidentifier | PK |
| UserId | uniqueidentifier | FK → Users, CASCADE DELETE, UNIQUE INDEX |
| Channel | int | NOT NULL (0=Telegram, 1=Email) |
| TelegramChatId | nvarchar(100) | nullable |
| Email | nvarchar(200) | nullable |
| IsEnabled | bit | NOT NULL |
| CreatedAt | datetime2 | NOT NULL |

---

## Running Migrations

```bash
# From backend/ directory
dotnet ef migrations add InitialCreate \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API

dotnet ef database update \
  --startup-project src/FinTrackPro.API
```

Connection string is read from `appsettings.json → ConnectionStrings:DefaultConnection`.
