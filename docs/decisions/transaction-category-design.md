# Transaction Category System

## Context

`Transaction.Category` was a free-text `string` field, causing three problems: inconsistent analytics ("Food" / "food & bev" / "Ăn uống" counted separately), brittle budget matching (exact-string dependency), and no bilingual label or icon support.

This document describes the structured `TransactionCategory` entity with 17 system-seeded defaults and user-custom categories, including the three-phase migration strategy that preserves all existing data and budget-matching logic throughout the transition.

---

## 1. System Architecture

```
Domain:         TransactionCategory entity · ITransactionCategoryRepository
Application:    CQRS (GetTransactionCategories, CreateTransactionCategory,
                      UpdateTransactionCategory, DeleteTransactionCategory)
                Modified CreateTransactionCommand (string Category → Guid CategoryId)
Infrastructure: TransactionCategoryConfiguration · TransactionCategoryRepository
                TransactionCategoryDataSeeder (startup, idempotent)
API:            TransactionCategoriesController — GET/POST/PATCH/DELETE /api/transaction-categories
Frontend:       entities/transaction-category/
                features/select-transaction-category/   — dropdown in transaction + budget forms
                features/manage-transaction-categories/ — CRUD UI in Settings
```

**Key invariants:**
- `Transaction.Category` (string slug) is kept and always populated from the resolved category — zero breakage to budget matching or existing rows.
- `Transaction.CategoryId` (nullable FK) is additive; old rows have `NULL`.
- System categories are seeded by `TransactionCategoryDataSeeder` on startup (idempotent) — not via EF `HasData()`.
- System categories cannot be modified or deleted — domain entity throws `AuthorizationException` → HTTP 403.
- `AppUser` custom categories are scoped by `UserId`; system categories have `UserId = NULL`.

---

## 2. Data Dictionary

Full schema DDL and column types: [docs/architecture/database.md — TransactionCategories](../architecture/database.md#transactioncategories).

Key field design decisions:

| Field | Design intent |
|-------|---------------|
| `UserId` | `NULL` = system category (globally unique slug); non-null = user-owned (slug unique per user). |
| `Slug` | Stable machine key written into `Transaction.Category` for budget-matching compatibility. Never changes after creation. |
| `LabelEn` / `LabelVi` | Bilingual display labels — UI renders the one matching `useLocaleStore` language. |
| `IsSystem` | Guards against mutation/deletion at the domain layer; enforced in `UpdateLabels()` and `SoftDelete()`. |
| `IsActive` | Soft-delete flag. Hard deletes are never used for user categories — preserves historical transaction display. |
| `SortOrder` | System categories are ordered by this field; custom categories follow system ones alphabetically. |
| `Transactions.CategoryId` | Nullable FK — old rows have `NULL` (Phase 1 additive migration). `Transaction.Category` string slug is always populated for budget matching regardless of whether `CategoryId` is set. |

---

## 3. System Categories

17 categories seeded at startup. Cannot be modified or deleted by any user.

### Income (5)

| Slug         | LabelEn      | LabelVi         | Icon | Order |
|--------------|--------------|-----------------|------|-------|
| salary       | Salary       | Lương           | 💰   | 1     |
| bonus        | Bonus        | Thưởng          | 🎁   | 2     |
| investment   | Investment   | Đầu tư          | 📈   | 3     |
| freelance    | Freelance    | Công việc tự do | 💻   | 4     |
| other_income | Other Income | Thu nhập khác   | ➕   | 5     |

### Expense (12)

| Slug           | LabelEn         | LabelVi       | Icon | Order |
|----------------|-----------------|---------------|------|-------|
| food_beverage  | Food & Beverage | Ăn uống       | 🍜   | 1     |
| transportation | Transportation  | Di chuyển     | 🚗   | 2     |
| rent           | Rent            | Thuê nhà      | 🏠   | 3     |
| utilities      | Utilities       | Tiện ích      | 💡   | 4     |
| shopping       | Shopping        | Mua sắm       | 🛍️  | 5     |
| entertainment  | Entertainment   | Giải trí      | 🎬   | 6     |
| healthcare     | Healthcare      | Sức khỏe      | 🏥   | 7     |
| education      | Education       | Giáo dục      | 📚   | 8     |
| travel         | Travel          | Du lịch       | ✈️   | 9     |
| family         | Family          | Gia đình      | 👨‍👩‍👧 | 10    |
| children       | Children        | Trẻ em        | 🧒   | 11    |
| other_expense  | Other Expense   | Chi tiêu khác | 📦   | 12    |

---

## 4. API Contract

### GET /api/transaction-categories?type=Income|Expense
Returns system categories + caller's active custom categories, sorted system-first then by `SortOrder`. `type` is optional.

**Response 200:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "slug": "food_beverage",
    "labelEn": "Food & Beverage",
    "labelVi": "Ăn uống",
    "icon": "🍜",
    "type": "Expense",
    "isSystem": true,
    "sortOrder": 1
  }
]
```

### POST /api/transaction-categories
```json
// Request
{ "type": "Expense", "slug": "pet_care", "labelEn": "Pet Care", "labelVi": "Thú cưng", "icon": "🐶" }
// Response 201: Guid
```
`slug` must match `^[a-z][a-z0-9_]{1,98}$` and be unique within the user's scope (409 on conflict).

### PATCH /api/transaction-categories/{id}
```json
{ "labelEn": "Pets", "labelVi": "Thú cưng", "icon": "🐾" }
// Response 204. Returns 403 if IsSystem = true.
```

### DELETE /api/transaction-categories/{id}
Soft delete — sets `IsActive = false`. Response 204. Returns 403 if `IsSystem = true`.

---

## 5. Backend Components

**Domain**
- `TransactionCategory.cs` — `BaseEntity` subclass; `Create()`, `UpdateLabels()`, `SoftDelete()`; throws `AuthorizationException` on system-category mutation.
- `Transaction.cs` — add `Guid? CategoryId`; `Create()` accepts optional `categoryId`.
- `ITransactionCategoryRepository.cs` — `GetByUserAsync(userId, type?)`, `GetByIdAsync`, `SlugExistsForUserAsync`, `Add`.

**Application**
- `TransactionCategories/` — four CQRS pairs (Get query + DTO, Create/Update/Delete commands with FluentValidation validators).
- `IDataSeeder.cs` — `Task SeedAsync(CancellationToken)` interface in `Common/Interfaces/`.
- `CreateTransactionCommand` — `string Category` replaced by `Guid CategoryId`; handler resolves category, writes `category.Slug` to the legacy column.
- `TransactionDto` — `Guid? CategoryId` added.

**Infrastructure**
- `TransactionCategoryConfiguration.cs` — column lengths, unique index, cascade FK.
- `TransactionConfiguration.cs` — nullable `CategoryId` FK with `ON DELETE SET NULL`.
- `TransactionCategoryRepository.cs` — queries `WHERE IsActive AND (UserId IS NULL OR UserId = @userId)`, ordered system-first.
- `TransactionCategoryDataSeeder.cs` — idempotent; checks existing slugs before inserting.
- `DependencyInjection.cs` — registers `ITransactionCategoryRepository` and `IDataSeeder`.

**API**
- `TransactionCategoriesController.cs` — `[Authorize(Roles = UserRole.User)]`; thin 4-endpoint controller.
- `Program.cs` — calls `seeder.SeedAsync()` after `db.Database.MigrateAsync()`.

**Migration:** `AddTransactionCategories` — adds `TransactionCategories` table and `Transactions.CategoryId` nullable FK.

---

## 6. Frontend Components

**`entities/transaction-category/`**
- `model/types.ts` — `TransactionCategory`, `CreateTransactionCategoryPayload`, `UpdateTransactionCategoryPayload`.
- `api/transactionCategoryApi.ts` — `useTransactionCategories(type?)` (staleTime: 5 min), `useCreateTransactionCategory`, `useUpdateTransactionCategory`, `useDeleteTransactionCategory`. React Query key: `['transaction-categories', type]`.

**`features/select-transaction-category/`**
- `model/useLastUsedTransactionCategory.ts` — reads/writes `fintrackpro-last-transaction-category-id` in localStorage.
- `ui/TransactionCategorySelector.tsx` — `<select>` with `<optgroup>` for system vs custom categories; emoji + bilingual label; last-used marked ★; "Manage categories →" link navigates to `/settings`.

**`features/manage-transaction-categories/`** *(new)*
- `ui/ManageCategoriesSection.tsx` — settings card listing the user's custom categories (system categories excluded); each row shows emoji, label in current language, both language names in gray, type badge; inline Edit / Delete buttons; "New category" button.
- `ui/CategoryFormModal.tsx` — modal for create and edit; bilingual name inputs (LabelEn + LabelVi); emoji picker (28 curated icons); type toggle (Expense/Income — disabled in edit mode); live slug preview (create mode only); form validates both names non-empty before submit.

**Modified files**
| File | Change |
|------|--------|
| `entities/transaction/model/types.ts` | Add `categoryId?: string` |
| `features/add-transaction/ui/AddTransactionForm.tsx` | Replace free-text category input with `<TransactionCategorySelector>`; send `categoryId`; call `setLastUsedId` on success |
| `features/add-budget/ui/AddBudgetForm.tsx` | Same selector; resolve `slug` from selected category for budget API |
| `pages/settings/ui/SettingsPage.tsx` | Add `<ManageCategoriesSection>` |
| `shared/i18n/en.ts` | Add `transactionCategories` and `settings.myCategories` namespace keys |
| `shared/i18n/vi.ts` | Same (Vietnamese) |

---

## 7. UI/UX

### TransactionCategorySelector (Add Transaction / Add Budget forms)

```
┌──────────────────────────────────────┐
│ 🍜 Ăn uống                       ▾  │  ← selected (icon + label in current lang)
└──────────────────────────────────────┘
           ↓ expanded
┌──────────────────────────────────────┐
│ ── Categories ──────────────────     │
│  💰 Lương                            │
│  📈 Đầu tư                    ★      │  ← ★ = last used
│  💻 Công việc tự do                  │
│ ── My Categories ───────────────     │
│  🐶 Thú cưng                         │
│ ─────────────────────────────────    │
│  + Manage categories →               │  ← navigates to /settings
└──────────────────────────────────────┘
```

"My Categories" optgroup is hidden when the user has no custom categories. Loading: select disabled with placeholder.

---

### ManageCategoriesSection (Settings page)

```
  My Categories                      [ + New category ]
  ─────────────────────────────────────────────────────
  🍜  Food & Beverage                          [Edit] [Delete]
      Food & Beverage · Ăn uống   Expense
  🐶  Pet Care                                 [Edit] [Delete]
      Pet Care · Thú cưng         Expense

  — empty state —

  🗂️
  No custom categories yet.
  Create your first category to personalise your tracking.
                   [ + Create first category ]
```

System categories are not shown here — they appear only in the selector dropdown and cannot be managed.

---

### CategoryFormModal (Create / Edit)

```
  ┌──────────────────────────────────────────┐
  │  New Category                        [×] │
  │  ──────────────────────────────────────  │
  │  Name (English)  [ Pet Care          ]   │
  │  Name (Tiếng Việt) [ Thú cưng        ]   │
  │                                          │
  │  Icon                                    │
  │  🍜 🚗 🏠 💡 🛍️ 🎬 🏥 📚 ✈️ 👨‍👩‍👧 🧒 📦    │
  │  💰 🎁 📈 💻 ➕ 🐶 🌿 🎮 🏋️ 🎵 ❤️ 📌   │
  │                                          │
  │  Type     ● Expense   ○ Income           │  ← disabled in edit mode
  │                                          │
  │  Slug preview: pet_care                  │  ← auto-derived from English name; create only
  │                                          │
  │     [ Cancel ]    [ Create category ]    │
  └──────────────────────────────────────────┘
```

Both name fields are required. Slug is auto-derived (lowercase, underscores) and shown as a read-only preview — the user never types it. Type is immutable after creation.

---

## 8. Migration Strategy

**Phase 1 — Additive (implemented)**
- `Transactions.CategoryId` nullable FK added.
- `CreateTransactionCommand` takes `Guid CategoryId`; handler resolves category and writes its slug to `Transaction.Category`.
- Old rows: `CategoryId = NULL`, `Category = "old text"` — untouched and valid.
- Budget matching unaffected: both old and new transactions have a valid `Category` slug.

**Phase 2 — Backfill (future Hangfire job)**
- `CategoryBackfillJob`: fuzzy-match old `Transaction.Category` strings against `TransactionCategory.Slug` / `LabelEn`, set `CategoryId` in batches of 500.

**Phase 3 — Cleanup (after backfill is 100%)**
- Make `CategoryId NOT NULL`.
- Drop or archive the legacy `Category` column.

---

## 9. Testing

| File | Type |
|------|------|
| `Domain.UnitTests/Finance/TransactionCategoryTests.cs` | Domain unit — Create validation, UpdateLabels/SoftDelete authorization |
| `Application.UnitTests/TransactionCategories/GetTransactionCategoriesHandlerTests.cs` | App unit |
| `Application.UnitTests/TransactionCategories/CreateTransactionCategoryHandlerTests.cs` | App unit |
| `Application.UnitTests/TransactionCategories/UpdateTransactionCategoryHandlerTests.cs` | App unit |
| `Application.UnitTests/TransactionCategories/DeleteTransactionCategoryHandlerTests.cs` | App unit |
| `Application.UnitTests/Validators/CreateTransactionCategoryCommandValidatorTests.cs` | App unit — slug regex, required fields |
| `Api.IntegrationTests/Features/Finance/TransactionCategoriesTests.cs` | Integration — 11 cases; asserts 17 system + 12 expense categories |
| `Tests.Common/Builders/TransactionCategoryRequestBuilder.cs` | Shared builder |
| `entities/transaction-category/api/transactionCategoryApi.test.ts` | Vitest — hook contracts |
| `features/select-transaction-category/ui/TransactionCategorySelector.test.tsx` | Vitest component |
| `features/manage-transaction-categories/ui/CategoryFormModal.test.tsx` | Vitest component |
| `features/manage-transaction-categories/ui/ManageCategoriesSection.test.tsx` | Vitest component |
| `tests/e2e/transaction-categories.spec.ts` | Playwright E2E |

---

## 10. Verification Checklist

- [ ] `dotnet build` passes with zero errors
- [ ] Migration generates `TransactionCategories` table + `Transactions.CategoryId` nullable FK
- [ ] `dotnet run` → seeder inserts 17 system categories (idempotent on restart)
- [ ] `GET /api/transaction-categories` returns 17 categories
- [ ] `POST /api/transactions` with `categoryId` → 201; response includes both `categoryId` and `category` (slug)
- [ ] `POST /api/transaction-categories` with duplicate slug → 409
- [ ] `PATCH /api/transaction-categories/{systemId}` → 403
- [ ] `DELETE /api/transaction-categories/{systemId}` → 403
- [ ] Frontend: `TransactionCategorySelector` renders system + custom optgroups; language toggle switches labels en↔vi; last-used marked ★
- [ ] Frontend: `ManageCategoriesSection` in Settings lists custom categories; Create/Edit/Delete work; system categories not shown
- [ ] `CategoryFormModal`: slug preview updates live from English name; type disabled in edit mode
- [ ] Budget form sends category slug correctly; existing budget-match logic unaffected
- [ ] `dotnet test --filter "Category!=Integration"` passes
- [ ] `npm test` passes
