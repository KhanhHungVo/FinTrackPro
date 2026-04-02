# Transaction Category System

## Context

`Transaction.Category` is currently a free-text `string` field. This means:
- No consistency across users (e.g., "Food", "food & bev", "Ăn uống" are three separate values in analytics)
- Budget matching is brittle — `Budget.Category` must exactly match `Transaction.Category`
- No bilingual label support (en/vi)
- No icon display in the UI

This document describes the design for a structured `TransactionCategory` entity with system-seeded defaults and user-custom categories, with a three-phase migration strategy that keeps existing data and budget-matching logic intact throughout the transition.

---

## 1. System Architecture Overview

```
Domain:         TransactionCategory entity + ITransactionCategoryRepository
Application:    CQRS (GetTransactionCategories, CreateTransactionCategory, UpdateTransactionCategory, DeleteTransactionCategory)
                Modified CreateTransactionCommand (string Category → Guid CategoryId)
Infrastructure: TransactionCategoryConfiguration, TransactionCategoryRepository, TransactionCategoryDataSeeder
API:            TransactionCategoriesController (GET/POST/PATCH/DELETE /api/transaction-categories)
Frontend:       entities/transaction-category/ + features/select-transaction-category/TransactionCategorySelector
```

### Naming Convention

Entity name drives naming at every layer — consistent with the existing codebase pattern:

| Layer | Convention | Example |
|---|---|---|
| Domain entity | PascalCase noun | `TransactionCategory` |
| DB table | Plural of entity | `TransactionCategories` |
| Repository interface | `I{Entity}Repository` | `ITransactionCategoryRepository` |
| EF config | `{Entity}Configuration` | `TransactionCategoryConfiguration` |
| Application folder | `{Entity}` plural | `Application/TransactionCategories/` |
| Command/Query | `{Action}{Entity}Command` | `CreateTransactionCategoryCommand` |
| DTO | `{Entity}Dto` | `TransactionCategoryDto` |
| Controller | `{Entity}sController` | `TransactionCategoriesController` |
| API route | kebab-case plural | `/api/transaction-categories` |
| Frontend entity folder | kebab-case | `entities/transaction-category/` |
| React Query key | kebab-case | `['transaction-categories', type]` |

**Key invariants:**
- `Transaction.Category` (string) is kept and always populated from the resolved category slug — zero breakage to budget matching or existing rows
- `Transaction.CategoryId` (nullable FK) is additive; old rows have `NULL`
- System categories are seeded by `CategoryDataSeeder` on startup (idempotent) — NOT via EF `HasData()`
- System categories cannot be modified or deleted (domain entity throws `AuthorizationException` → HTTP 403)

---

## 2. Database Schema

```sql
CREATE TABLE "TransactionCategories" (
    "Id"        uuid          NOT NULL PRIMARY KEY,
    "UserId"    uuid          NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Type"      integer       NOT NULL,   -- 0=Income, 1=Expense
    "Slug"      varchar(100)  NOT NULL,   -- stable machine key e.g. "food_beverage"
    "LabelEn"   varchar(100)  NOT NULL,
    "LabelVi"   varchar(100)  NOT NULL,
    "Icon"      varchar(50)   NOT NULL,   -- emoji e.g. "🍜"
    "IsSystem"  boolean       NOT NULL DEFAULT false,
    "IsActive"  boolean       NOT NULL DEFAULT true,
    "SortOrder" integer       NOT NULL DEFAULT 0,
    "CreatedAt" timestamptz   NOT NULL
);

-- System categories: NULL UserId + unique slug = globally unique
-- User categories: unique slug scoped per user
CREATE UNIQUE INDEX "IX_TransactionCategories_UserId_Slug"
    ON "TransactionCategories"("UserId", "Slug");

-- Add nullable FK to Transactions (phase 1 migration)
ALTER TABLE "Transactions"
    ADD COLUMN "CategoryId" uuid NULL
        REFERENCES "TransactionCategories"("Id") ON DELETE SET NULL;
```

`Budget.Category` stays as a string slug — unchanged. Budget matching (`GetByUserCategoryMonthAsync`) continues to work because new transactions write the category's `Slug` value into `Transaction.Category`.

---

## 3. Default System Categories

### Income

| Slug         | LabelEn      | LabelVi             | Icon | Order |
|--------------|--------------|---------------------|------|-------|
| salary       | Salary       | Lương               | 💰   | 1     |
| bonus        | Bonus        | Thưởng              | 🎁   | 2     |
| investment   | Investment   | Đầu tư              | 📈   | 3     |
| freelance    | Freelance    | Công việc tự do     | 💻   | 4     |
| other_income | Other Income | Thu nhập khác       | ➕   | 5     |

### Expense

| Slug           | LabelEn             | LabelVi           | Icon | Order |
|----------------|---------------------|-------------------|------|-------|
| food_beverage  | Food & Beverage     | Ăn uống           | 🍜   | 1     |
| transportation | Transportation      | Di chuyển         | 🚗   | 2     |
| rent           | Rent                | Thuê nhà          | 🏠   | 3     |
| utilities      | Utilities           | Tiện ích          | 💡   | 4     |
| shopping       | Shopping            | Mua sắm           | 🛍️  | 5     |
| entertainment  | Entertainment       | Giải trí          | 🎬   | 6     |
| healthcare     | Healthcare          | Sức khỏe          | 🏥   | 7     |
| education      | Education           | Giáo dục          | 📚   | 8     |
| travel         | Travel              | Du lịch           | ✈️   | 9     |
| family_child   | Family & Children   | Gia đình & trẻ em | 👨‍👩‍👧 | 10    |
| other_expense  | Other Expense       | Chi tiêu khác     | 📦   | 11    |

---

## 4. API Contract

### GET /api/transaction-categories?type=Income|Expense
Returns system categories + caller's active custom categories, sorted system-first then by `SortOrder`. `type` is optional — omitting returns all.

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
**Request:**
```json
{
  "type": "Expense",
  "slug": "pet_care",
  "labelEn": "Pet Care",
  "labelVi": "Thú cưng",
  "icon": "🐶"
}
```
**Response 201:** `Guid`

Validation:
- `slug` must match `^[a-z][a-z0-9_]{1,98}$`
- `slug` must be unique within the user's scope (→ 409 on conflict)

### PATCH /api/transaction-categories/{id}
**Request:**
```json
{ "labelEn": "Pets", "labelVi": "Thú cưng", "icon": "🐾" }
```
**Response 204.** Returns HTTP 403 if `IsSystem = true`.

### DELETE /api/transaction-categories/{id}
Soft delete — sets `IsActive = false`.
**Response 204.** Returns HTTP 403 if `IsSystem = true`.

---

## 5. Migration Strategy (three phases, zero downtime)

### Phase 1 — Additive (implement now)
- Add nullable `CategoryId` FK to `Transactions`
- `CreateTransactionCommand` takes `Guid CategoryId` instead of `string Category`
- Handler resolves the category → populates `Transaction.Category` with `category.Slug`
- Old rows: `CategoryId = NULL`, `Category = "old text"` — untouched and valid
- Budget matching continues: `Budget.Category = slug`, `Transaction.Category = slug` for all new records

### Phase 2 — Backfill (future Hangfire job)
- `CategoryBackfillJob`: fuzzy-match old `Transaction.Category` strings against `TransactionCategory.Slug` / `LabelEn`, set `CategoryId` in batches of 500
- Run as a background job with progress tracking in Hangfire state

### Phase 3 — Cleanup (after backfill is 100%)
- Migration makes `CategoryId NOT NULL`
- Drop legacy `Category` column (or keep as an archived label)
- Update domain entity and EF configuration accordingly

---

## 6. Backend Implementation — File by File

### Domain
| File | Action |
|------|--------|
| `Domain/Entities/TransactionCategory.cs` | **Create** — `BaseEntity` subclass with factory `Create()`, `UpdateLabels()`, `SoftDelete()` |
| `Domain/Entities/Transaction.cs` | **Modify** — add `public Guid? CategoryId { get; private set; }`, update `Create()` to accept optional `categoryId` |
| `Domain/Repositories/ITransactionCategoryRepository.cs` | **Create** — `GetByUserAsync(userId, type?)`, `GetByIdAsync`, `SlugExistsForUserAsync`, `Add` |

### Application
| File | Action |
|------|--------|
| `Application/Common/Interfaces/IDataSeeder.cs` | **Create** — `Task SeedAsync(CancellationToken)` |
| `Application/TransactionCategories/Queries/GetTransactionCategories/GetTransactionCategoriesQuery.cs` | **Create** |
| `Application/TransactionCategories/Queries/GetTransactionCategories/TransactionCategoryDto.cs` | **Create** — explicit `operator` conversion from entity |
| `Application/TransactionCategories/Queries/GetTransactionCategories/GetTransactionCategoriesQueryHandler.cs` | **Create** |
| `Application/TransactionCategories/Commands/CreateTransactionCategory/` (3 files) | **Create** — command, validator (slug regex), handler |
| `Application/TransactionCategories/Commands/UpdateTransactionCategory/` (3 files) | **Create** — command, validator, handler |
| `Application/TransactionCategories/Commands/DeleteTransactionCategory/` (2 files) | **Create** — command, handler (calls `category.SoftDelete()`) |
| `Application/Finance/Commands/CreateTransaction/CreateTransactionCommand.cs` | **Modify** — replace `string Category` with `Guid CategoryId` |
| `Application/Finance/Commands/CreateTransaction/CreateTransactionCommandValidator.cs` | **Modify** — `RuleFor(v => v.CategoryId).NotEmpty()` |
| `Application/Finance/Commands/CreateTransaction/CreateTransactionCommandHandler.cs` | **Modify** — resolve category by `CategoryId`, pass `category.Slug` to `Transaction.Create()` |
| `Application/Finance/Queries/GetTransactions/TransactionDto.cs` | **Modify** — add `Guid? CategoryId` |

### Infrastructure
| File | Action |
|------|--------|
| `Infrastructure/Persistence/ApplicationDbContext.cs` | **Modify** — add `DbSet<TransactionCategory>` |
| `Infrastructure/Persistence/Configurations/TransactionCategoryConfiguration.cs` | **Create** — column lengths, unique index, cascade FK |
| `Infrastructure/Persistence/Configurations/TransactionConfiguration.cs` | **Modify** — add nullable `CategoryId` FK with `ON DELETE SET NULL` |
| `Infrastructure/Persistence/Repositories/TransactionCategoryRepository.cs` | **Create** — `GetByUserAsync` queries `WHERE IsActive AND (UserId IS NULL OR UserId = @userId)` |
| `Infrastructure/Persistence/Seeders/TransactionCategoryDataSeeder.cs` | **Create** — implements `IDataSeeder`, idempotent |
| `Infrastructure/DependencyInjection.cs` | **Modify** — register `ITransactionCategoryRepository`, `IDataSeeder` |

### API
| File | Action |
|------|--------|
| `API/Controllers/TransactionCategoriesController.cs` | **Create** — `[Authorize(Roles = UserRole.User)]`, 4 endpoints |
| `API/Program.cs` | **Modify** — call `seeder.SeedAsync()` after `db.Database.MigrateAsync()` |

**Generate migration:**
```bash
cd backend
dotnet ef migrations add AddTransactionCategories \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API
```

---

## 7. Key Implementation Details

### TransactionCategoryDataSeeder (idempotent startup seed)
```csharp
// Called in Program.cs after db.Database.MigrateAsync()
// Checks existing slugs before inserting — safe to run on every startup
var existingSlugs = await context.TransactionCategories
    .Where(c => c.IsSystem)
    .Select(c => c.Slug)
    .ToHashSetAsync();

var toAdd = SystemCategories
    .Where(s => !existingSlugs.Contains(s.Slug))
    .Select(s => TransactionCategory.Create(...))
    .ToList();
```

### TransactionCategoryRepository — single query for system + user categories
```csharp
context.TransactionCategories
    .Where(c => c.IsActive && (c.UserId == null || c.UserId == userId))
    .OrderBy(c => c.IsSystem ? 0 : 1)  // system first
    .ThenBy(c => c.SortOrder)
    .ThenBy(c => c.LabelEn)
```

### CreateTransactionCommandHandler — slug written to legacy column
```csharp
var category = await categoryRepository.GetByIdAsync(request.CategoryId, ct)
    ?? throw new NotFoundException(nameof(TransactionCategory), request.CategoryId);

if (category.UserId != null && category.UserId != user.Id)
    throw new AuthorizationException("Category does not belong to this user.");

var transaction = Transaction.Create(
    user.Id, request.Type, request.Amount,
    request.Currency, rateToUsd,
    category.Slug,          // populate legacy string column with slug
    request.Note, request.BudgetMonth,
    categoryId: request.CategoryId);
```

---

## 8. Frontend Implementation — File by File

### New entity: `entities/transaction-category/`
| File | Content |
|------|---------|
| `model/types.ts` | `TransactionCategory`, `CreateTransactionCategoryPayload`, `UpdateTransactionCategoryPayload` interfaces |
| `api/transactionCategoryApi.ts` | `useTransactionCategories(type?)` (staleTime: 5 min), `useCreateTransactionCategory`, `useUpdateTransactionCategory`, `useDeleteTransactionCategory`; React Query key: `['transaction-categories', type]` |
| `index.ts` | Barrel export |

### New feature: `features/select-transaction-category/`
| File | Content |
|------|---------|
| `model/useLastUsedTransactionCategory.ts` | Reads/writes `fintrackpro-last-transaction-category-id` in localStorage |
| `ui/TransactionCategorySelector.tsx` | `<select>` with `<optgroup>` for system vs custom, icon + label in current i18n language |
| `index.ts` | Barrel export |

### Modified files
| File | Change |
|------|--------|
| `entities/transaction/model/types.ts` | Add `categoryId?: string` |
| `features/add-transaction/ui/AddTransactionForm.tsx` | Replace free-text `category` input with `<TransactionCategorySelector>`, send `categoryId` in mutate body, call `setLastUsedId` on success |
| `features/add-budget/ui/AddBudgetForm.tsx` | Replace text input with `<TransactionCategorySelector>`, resolve `slug` from selected category to send to budget API |
| `shared/i18n/en.ts` | Add `transactionCategories` namespace |
| `shared/i18n/vi.ts` | Add `transactionCategories` namespace (Vietnamese) |

---

## 9. UI/UX — CategorySelector Component

```
┌─────────────────────────────────┐
│ 🍜 Ăn uống                  ▾   │  ← selected item (icon + label in current lang)
└─────────────────────────────────┘
         ↓ expanded
┌─────────────────────────────────┐
│ ── Categories ────────────────  │
│  💰 Lương                       │
│  🎁 Thưởng                      │
│  📈 Đầu tư              ★       │  ← ★ = last used
│  💻 Công việc tự do             │
│  ➕ Thu nhập khác               │
│ ── My Categories ─────────────  │
│  🐶 Thú cưng                    │
└─────────────────────────────────┘
```

- Native `<select>` + `<optgroup>` — no extra dependencies, matches existing form style
- Language toggle (en↔vi) in `useLocaleStore` re-renders labels instantly
- `<optgroup label="My Categories">` hidden if user has no custom categories
- Loading: disabled `<select>` with placeholder while React Query fetches
- ≤ 2 taps to select on mobile

---

## 10. Smart / Future-Ready Design

| Feature | How it's prepared |
|---------|------------------|
| Last-used default | `useLastUsedCategory` stores `categoryId` in localStorage; pre-selected on form mount |
| Keyword-to-category mapping | `Slug` is the stable key — future ML maps strings like "phở" → `food_beverage` |
| Usage frequency tracking | Add `UsageCount int` to `TransactionCategories` later; increment in `CreateTransactionCommandHandler` |
| AI suggestion endpoint | Reserve `POST /api/transaction-categories/suggest` — keyword rules first, ML model later |
| Budgeting by category | Already works via slug matching; can be enhanced with `CategoryId` FK join after Phase 2 backfill |

---

## 11. Testing

### Backend

Three test layers matching the existing project convention: xUnit + NSubstitute + FluentAssertions.

#### 11.1 Domain Unit Tests
**Project:** `FinTrackPro.Domain.UnitTests`
**New file:** `tests/FinTrackPro.Domain.UnitTests/Finance/TransactionCategoryTests.cs`

| Test | Assertion |
|---|---|
| `Create_ValidArguments_ReturnsCategory` | All fields set, `Id` non-empty, `IsActive = true`, `IsSystem = false` |
| `Create_BlankSlug_ThrowsDomainException` | `DomainException` message matches `*Slug*` |
| `Create_BlankLabelEn_ThrowsDomainException` | `DomainException` message matches `*label*` |
| `Create_SlugNormalizedToLowercase` | `Slug` is trimmed and lowercased |
| `UpdateLabels_UserCategory_UpdatesFields` | `LabelEn`, `LabelVi`, `Icon` all changed |
| `UpdateLabels_SystemCategory_ThrowsAuthorizationException` | `AuthorizationException` thrown |
| `SoftDelete_UserCategory_SetsIsActiveFalse` | `IsActive == false` |
| `SoftDelete_SystemCategory_ThrowsAuthorizationException` | `AuthorizationException` thrown |

**Updated file:** `tests/FinTrackPro.Domain.UnitTests/Finance/TransactionTests.cs`

| Test to update | Change |
|---|---|
| `Create_ValidArguments_ReturnsTransaction` | Add `categoryId` arg; assert `tx.CategoryId` is set |
| `Create_BlankCategory_ThrowsDomainException` | Still valid — `category` (slug string) remains required |

---

#### 11.2 Application Unit Tests
**Project:** `FinTrackPro.Application.UnitTests`

**New file:** `tests/FinTrackPro.Application.UnitTests/TransactionCategories/GetTransactionCategoriesHandlerTests.cs`

| Test | Assertion |
|---|---|
| `Handle_ValidQuery_ReturnsMappedDtos` | `TransactionCategoryDto` list returned, fields match entity |
| `Handle_WithTypeFilter_PassesTypeToRepository` | `GetByUserAsync` called with correct `type` argument |
| `Handle_UserNotFound_ThrowsNotFoundException` | `NotFoundException` thrown |

**New file:** `tests/FinTrackPro.Application.UnitTests/TransactionCategories/CreateTransactionCategoryHandlerTests.cs`

| Test | Assertion |
|---|---|
| `Handle_ValidCommand_ReturnsNewGuid` | `categoryRepository.Add()` called once, `SaveChangesAsync` called once |
| `Handle_DuplicateSlug_ThrowsConflictException` | `SlugExistsForUserAsync` returns `true` → `ConflictException` |
| `Handle_UserNotFound_ThrowsNotFoundException` | `NotFoundException` thrown |

**New file:** `tests/FinTrackPro.Application.UnitTests/TransactionCategories/UpdateTransactionCategoryHandlerTests.cs`

| Test | Assertion |
|---|---|
| `Handle_UserCategory_UpdatesAndSaves` | `SaveChangesAsync` called once |
| `Handle_CategoryNotFound_ThrowsNotFoundException` | `NotFoundException` thrown |
| `Handle_OtherUsersCategory_ThrowsAuthorizationException` | `AuthorizationException` thrown (ownership check) |
| `Handle_SystemCategory_ThrowsAuthorizationException` | Domain `AuthorizationException` propagated from `UpdateLabels()` |

**New file:** `tests/FinTrackPro.Application.UnitTests/TransactionCategories/DeleteTransactionCategoryHandlerTests.cs`

| Test | Assertion |
|---|---|
| `Handle_UserCategory_SetsIsActiveFalseAndSaves` | `SaveChangesAsync` called, `IsActive == false` |
| `Handle_SystemCategory_ThrowsAuthorizationException` | `AuthorizationException` thrown from `SoftDelete()` |
| `Handle_CategoryNotFound_ThrowsNotFoundException` | `NotFoundException` thrown |

**New file:** `tests/FinTrackPro.Application.UnitTests/Validators/CreateTransactionCategoryCommandValidatorTests.cs`

| Test | Assertion |
|---|---|
| `Validate_ValidCommand_Passes` | `IsValid == true` |
| `Validate_EmptySlug_Fails` | Error on `Slug` |
| `Validate_SlugWithUppercase_Fails` | Regex mismatch on `Slug` |
| `Validate_SlugWithSpaces_Fails` | Regex mismatch on `Slug` |
| `Validate_SlugStartingWithDigit_Fails` | Regex mismatch on `Slug` |
| `Validate_EmptyLabelEn_Fails` | Error on `LabelEn` |
| `Validate_InvalidType_Fails` | Error on `Type` |

**Updated file:** `tests/FinTrackPro.Application.UnitTests/Finance/CreateTransactionHandlerTests.cs`

- Add `ITransactionCategoryRepository _categoryRepository = Substitute.For<ITransactionCategoryRepository>()` to constructor
- Add a `TransactionCategory` fixture (system category, `UserId = null`)
- Update `Handle_ValidCommand_ReturnsNewGuid`: command now takes `CategoryId` (not `category` string); stub `_categoryRepository.GetByIdAsync()` to return the fixture
- Add `Handle_CategoryNotFound_ThrowsNotFoundException`
- Add `Handle_OtherUsersCategoryId_ThrowsAuthorizationException`

**Updated file:** `tests/FinTrackPro.Application.UnitTests/Validators/CreateTransactionCommandValidatorTests.cs`

- Replace `Valid()` factory: `category: "Food"` → `categoryId: Guid.NewGuid()`
- Replace `Validate_EmptyCategory_Fails` → `Validate_EmptyGuidCategoryId_Fails`: `CategoryId = Guid.Empty` → fails

---

#### 11.3 Integration Tests
**Project:** `FinTrackPro.Api.IntegrationTests`

**New file:** `tests/FinTrackPro.Api.IntegrationTests/Features/Finance/TransactionCategoriesTests.cs`

Decorated with `[Trait("Category", "Integration")]` and `[Collection(nameof(IntegrationTestCollection))]`.

| Test | Assertion |
|---|---|
| `GetTransactionCategories_Authenticated_Returns16SystemCategories` | Status 200, count == 16 |
| `GetTransactionCategories_WithTypeFilter_ReturnsOnlyExpense` | `?type=Expense` → only 11 expense categories |
| `GetTransactionCategories_Unauthenticated_Returns401` | Status 401 |
| `CreateTransactionCategory_ValidRequest_Returns201` | Status 201, body is valid `Guid` |
| `CreateTransactionCategory_DuplicateSlug_Returns409` | POST same slug twice → 409 on second |
| `CreateTransactionCategory_InvalidSlug_Returns400` | Slug `"My Category"` (spaces) → 400 |
| `UpdateTransactionCategory_OwnCustomCategory_Returns204` | Status 204 |
| `UpdateTransactionCategory_SystemCategory_Returns403` | Pass system category ID → 403 |
| `DeleteTransactionCategory_OwnCustomCategory_Returns204` | Status 204; subsequent GET excludes it |
| `DeleteTransactionCategory_SystemCategory_Returns403` | Status 403 |

**New file:** `tests/Tests.Common/Builders/TransactionCategoryRequestBuilder.cs`

```csharp
public static class TransactionCategoryRequestBuilder
{
    public static object Build(
        string? slug = null,
        string? labelEn = null,
        string? labelVi = null,
        string? icon = null,
        TransactionType? type = null) => new
    {
        slug    = slug    ?? $"custom_{Guid.NewGuid():N}"[..20],
        labelEn = labelEn ?? "Custom Category",
        labelVi = labelVi ?? "Danh mục tùy chỉnh",
        icon    = icon    ?? "📌",
        type    = type    ?? TransactionType.Expense
    };
}
```

**Updated file:** `tests/Tests.Common/Builders/TransactionRequestBuilder.cs`

- Replace the `Categories` string array and free-text `category` field with a `categoryId: Guid` parameter
- Callers in `TransactionsTests.cs` must fetch a real system category ID from `GET /api/transaction-categories` first and pass it in

**Updated file:** `tests/FinTrackPro.Api.IntegrationTests/Features/Finance/TransactionsTests.cs`

- Add `InitializeAsync` step: `GET /api/transaction-categories?type=Expense` → store first result ID as `_expenseCategoryId`
- Update all `TransactionRequestBuilder.Build()` calls to pass `categoryId: _expenseCategoryId`
- Update the raw JSON test `CreateTransaction_InvalidEnumType_Returns400` to include `categoryId` instead of `category`

---

#### Backend run commands

```bash
# Unit tests only (no DB required)
dotnet test --filter "Category!=Integration"

# Integration tests (requires Docker postgres)
dotnet test --filter "Category=Integration"

# Single test class
dotnet test --filter "FullyQualifiedName~CreateTransactionCategoryHandlerTests"
```

---

### Frontend

Two test layers matching the existing project convention: Vitest (unit + component) and Playwright (E2E).

#### 11.4 Vitest Unit Tests — Entity API Hooks
**New file:** `src/entities/transaction-category/api/transactionCategoryApi.test.ts`

Pattern mirrors `src/entities/transaction/api/transactionApi.test.ts`.

| Test | Assertion |
|---|---|
| `useTransactionCategories — fetches all without type filter` | `GET /api/transaction-categories` called with empty params |
| `useTransactionCategories — passes type param when provided` | Called with `{ params: { type: 'Expense' } }` |
| `useCreateTransactionCategory — posts to correct endpoint` | `POST /api/transaction-categories` called with body |
| `useUpdateTransactionCategory — patches correct id` | `PATCH /api/transaction-categories/{id}` called |
| `useDeleteTransactionCategory — deletes correct id` | `DELETE /api/transaction-categories/{id}` called |

Mock: `vi.mock('@/shared/api/client', ...)` — same pattern as `transactionApi.test.ts`.

---

#### 11.5 Vitest Component Tests — TransactionCategorySelector
**New file:** `src/features/select-transaction-category/ui/TransactionCategorySelector.test.tsx`

Pattern mirrors `src/features/add-trade/ui/AddTradeForm.test.tsx`.

```ts
vi.mock('@/entities/transaction-category', () => ({
  useTransactionCategories: () => ({
    data: [
      { id: 'sys-1', slug: 'food_beverage', labelEn: 'Food & Beverage',
        labelVi: 'Ăn uống', icon: '🍜', type: 'Expense', isSystem: true, sortOrder: 1 },
      { id: 'usr-1', slug: 'pet_care', labelEn: 'Pet Care',
        labelVi: 'Thú cưng', icon: '🐶', type: 'Expense', isSystem: false, sortOrder: 0 },
    ],
    isLoading: false,
  }),
}))
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ i18n: { language: 'en' } }),
}))
```

| Test | Assertion |
|---|---|
| `renders system categories in first optgroup` | `🍜 Food & Beverage` option in the DOM |
| `renders custom categories in second optgroup` | `🐶 Pet Care` option visible |
| `calls onChange with selected category id` | `fireEvent.change` → `onChange` called with `'sys-1'` |
| `renders Vietnamese labels when language is vi` | Mock `i18n.language = 'vi'` → `Ăn uống` rendered |
| `shows loading state when isLoading true` | Select is `disabled`, option text contains "Loading" |
| `hides My Categories optgroup when no custom categories` | `optgroup` with "My Categories" label absent |
| `marks last-used category with ★` | Seed localStorage with `fintrackpro-last-transaction-category-id = sys-1` → option text contains `★` |

---

#### 11.6 Vitest Component Tests — AddTransactionForm
**Updated file:** `src/features/add-transaction/ui/AddTransactionForm.test.tsx` (if it exists) or new file

Add mock for `features/select-transaction-category`:
```ts
vi.mock('@/features/select-transaction-category', () => ({
  TransactionCategorySelector: ({ onChange }: { onChange: (id: string) => void }) =>
    React.createElement('select', {
      'data-testid': 'category-selector',
      onChange: (e: React.ChangeEvent<HTMLSelectElement>) => onChange(e.target.value),
    }),
  useLastUsedTransactionCategory: () => ({ lastUsedId: null, setLastUsedId: vi.fn() }),
}))
```

| Test | Assertion |
|---|---|
| `renders TransactionCategorySelector instead of text input` | `data-testid="category-selector"` in DOM |
| `submit without selecting category shows validation error` | Error message appears |
| `submit with valid categoryId calls mutate with categoryId` | `mutate` called with `{ categoryId: 'sel-id', ... }` |
| `calls setLastUsedId on successful submission` | `setLastUsedId` mock called with the selected ID |

---

#### 11.7 Playwright E2E Tests
**Updated file:** `tests/e2e/transactions.spec.ts`

Replace the free-text `Category` placeholder fill with a `<select>` interaction:

```ts
test('create transaction', async ({ page }) => {
  await injectAuthToken(page)
  await page.goto('/transactions')

  await page.locator('input[type="number"]').fill('85.50')
  // Select category from dropdown instead of typing
  await page.locator('[data-testid="category-selector"]').selectOption({ label: /Food/i })
  await page.getByRole('button', { name: /add transaction/i }).click()

  const row = page.locator('li').filter({ hasText: /Food/i }).filter({ hasText: '-' })
  await expect(row.first()).toBeVisible({ timeout: 10000 })
})
```

**New file:** `tests/e2e/transaction-categories.spec.ts`

| Test | Assertion |
|---|---|
| `category selector shows grouped options` | System group visible; `🍜 Ăn uống` option exists |
| `language toggle switches category labels` | Change locale to `vi` → labels switch from English to Vietnamese |
| `create custom category and it appears in selector` | Navigate to settings or category management, create `pet_care`, go to add transaction, see it in dropdown |

---

#### Frontend run commands

```bash
# All Vitest unit + component tests
npm test

# Single file
npx vitest run src/entities/transaction-category/api/transactionCategoryApi.test.ts

# E2E (requires running API + Keycloak)
bash scripts/e2e-local.sh tests/e2e/transaction-categories.spec.ts
```

---

### Summary of new test files

| File | Type |
|---|---|
| `Domain.UnitTests/Finance/TransactionCategoryTests.cs` | Backend domain unit |
| `Application.UnitTests/TransactionCategories/GetTransactionCategoriesHandlerTests.cs` | Backend app unit |
| `Application.UnitTests/TransactionCategories/CreateTransactionCategoryHandlerTests.cs` | Backend app unit |
| `Application.UnitTests/TransactionCategories/UpdateTransactionCategoryHandlerTests.cs` | Backend app unit |
| `Application.UnitTests/TransactionCategories/DeleteTransactionCategoryHandlerTests.cs` | Backend app unit |
| `Application.UnitTests/Validators/CreateTransactionCategoryCommandValidatorTests.cs` | Backend app unit |
| `Api.IntegrationTests/Features/Finance/TransactionCategoriesTests.cs` | Backend integration |
| `Tests.Common/Builders/TransactionCategoryRequestBuilder.cs` | Backend shared |
| `src/entities/transaction-category/api/transactionCategoryApi.test.ts` | Frontend Vitest |
| `src/features/select-transaction-category/ui/TransactionCategorySelector.test.tsx` | Frontend Vitest |
| `tests/e2e/transaction-categories.spec.ts` | Frontend Playwright |

---

## 12. Documentation Updates Required

Update these files as part of the same implementation task:

### `docs/architecture/database.md`
- Add `TransactionCategories` table schema (columns, types, constraints, indexes)
- Add note to `Transactions` table: `CategoryId` nullable FK column added, `Category` kept as legacy slug
- Add seeding note: system categories seeded at startup (not per-user, not via EF `HasData()`)

### `docs/architecture/api-spec.md`
- Add new section **Transaction Categories** with all 4 endpoints:
  - `GET /api/transaction-categories?type=Income|Expense`
  - `POST /api/transaction-categories`
  - `PATCH /api/transaction-categories/{id}`
  - `DELETE /api/transaction-categories/{id}`
- Update `POST /api/transactions` request body: replace `category: string` with `categoryId: uuid`
- Update `GET /api/transactions` response body: add `categoryId: uuid | null` field, note `category` is now the resolved slug

### `docs/architecture/overview.md`
- Update the Application layer description to mention `TransactionCategories` as a new feature group
- Add `IDataSeeder` to the Infrastructure layer description (startup seeding pattern)

### `docs/architecture/ui-flows.md`
- Update **Add Transaction** screen: replace free-text category input description with grouped `TransactionCategorySelector` (system categories + user custom categories, icon + bilingual label)
- Update **Add Budget** screen: same selector replaces text input; slug is derived from selection
- Add note: category display language follows `useLocaleStore` language preference (en/vi)

### `docs/postman/api-e2e-plan.md`
- Add **Transaction Categories** folder to the Newman collection structure:
  - `GET /api/transaction-categories` — assert 16 system categories returned
  - `POST /api/transaction-categories` — create custom, assert 201
  - `PATCH /api/transaction-categories/{id}` — update custom, assert 204
  - `DELETE /api/transaction-categories/{id}` (custom) — assert 204
  - `DELETE /api/transaction-categories/{systemId}` — assert 403
  - `POST /api/transaction-categories` with duplicate slug — assert 409
- Update `POST /api/transactions` test: replace `category` string with `categoryId` (use ID captured from GET categories response)

### `docs/planned/transaction-category-system.md` (this file)
- Move to `docs/decisions/` once implementation is complete
- Update title to reflect it is an implemented decision, not a plan

---

## 13. Postman Collection Changes

**Guiding principle:** E2E collection covers only critical flows. Full CRUD + guard + negative tests belong in the Dev collection.

### E2E collection — `FinTrackPro.e2e.postman_collection.json`

The only impact is in the existing **"Budgets + Transactions — Spending flow"** folder: POST `/api/transactions` must now send `categoryId` (a GUID) instead of `category` (a string). A setup request at the top of that folder fetches a system category ID.

**Updated folder structure:**

```
Budgets + Transactions — Spending flow/
  └─ GET  /api/transaction-categories            → 200  ← NEW (setup only)
       test script: find food_beverage, set transactionCategoryId env var
  └─ POST /api/budgets                           → 201, capture budgetId
  └─ POST /api/transactions                      → 201, capture transactionId  ← body updated
  └─ GET  /api/transactions?month={{testMonth}}  → 200
  └─ PATCH /api/budgets/{{budgetId}}             → 204
  └─ DELETE /api/transactions/{{transactionId}}  → 204
  └─ DELETE /api/budgets/{{budgetId}}            → 204
```

Test script for the new GET setup request:
```javascript
pm.test('Status 200', () => pm.response.to.have.status(200));
pm.test('Captures transactionCategoryId', () => {
  const categories = pm.response.json();
  const food = categories.find(c => c.slug === 'food_beverage');
  pm.expect(food).to.exist;
  pm.environment.set('transactionCategoryId', food.id);
});
```

Updated POST `/api/transactions` request body:
```json
{
  "type": "Expense",
  "amount": 120.50,
  "categoryId": "{{transactionCategoryId}}",
  "note": "Grocery run",
  "budgetMonth": "{{testMonth}}",
  "currency": "USD"
}
```

**New environment variable:** `transactionCategoryId` — empty default, captured at runtime.

---

### Dev collection — `FinTrackPro.dev.postman_collection.json`

**New folder: `Transaction Categories`** (insert between "Watched Symbols" and "Market"):

```
Transaction Categories/
  └─ GET  /api/transaction-categories              → 200, assert array + schema, capture transactionCategoryId
  └─ GET  /api/transaction-categories?type=Expense → 200, assert all items have type=="Expense"
  └─ POST /api/transaction-categories              → 201, capture customCategoryId
       body: { type:"Expense", slug:"pet_care", labelEn:"Pet Care", labelVi:"Thú cưng", icon:"🐶" }
  └─ PATCH /api/transaction-categories/{{customCategoryId}}       → 204
       body: { labelEn:"Pets", labelVi:"Thú cưng", icon:"🐾" }
  └─ PATCH /api/transaction-categories/{{transactionCategoryId}}  → 403  (system — guard)
  └─ DELETE /api/transaction-categories/{{customCategoryId}}      → 204
  └─ DELETE /api/transaction-categories/{{transactionCategoryId}} → 403  (system — guard)
  └─ POST /api/transaction-categories (duplicate slug "pet_care") → 409
```

Assertions follow the dev collection convention (status code + schema shape):
```javascript
// GET list test script
pm.test('Status 200', () => pm.response.to.have.status(200));
pm.test('Array with expected shape', () => {
  const json = pm.response.json();
  pm.expect(json).to.be.an('array').with.lengthOf.at.least(16);
  ['id','slug','labelEn','labelVi','icon','type','isSystem','sortOrder']
    .forEach(k => pm.expect(json[0]).to.have.property(k));
  pm.environment.set('transactionCategoryId', json.find(c => c.slug === 'food_beverage').id);
});
```

**Updated existing requests in `Transactions` folder:**

Both `POST /api/transactions → 201` and the `Validation & Error Cases` POST with `amount=-50`:
```json
{ "type": "Expense", "amount": 85.50, "categoryId": "{{transactionCategoryId}}", ... }
```

The first request in the `Transaction Categories` folder populates `transactionCategoryId`. Run that folder before `Transactions`, or extend the collection-level pre-request script to cache the ID alongside `bearerToken`.

**New environment variable:** `customCategoryId` — empty default, captured at runtime.

---

## 12. Verification Checklist

- [ ] `dotnet build` passes with zero errors
- [ ] Migration generates `TransactionCategories` table + `Transactions.CategoryId` nullable FK
- [ ] `dotnet run` → startup seeder inserts 16 system categories (idempotent on restart)
- [ ] `GET /api/transaction-categories` returns 16 categories
- [ ] `POST /api/transactions` with `categoryId` → 201; GET shows both `categoryId` and `category` (slug)
- [ ] `POST /api/transaction-categories` with duplicate slug → 409
- [ ] `DELETE /api/transaction-categories/{systemCategoryId}` → 403
- [ ] Frontend: `TransactionCategorySelector` renders grouped dropdown; language toggle switches labels en↔vi
- [ ] Budget form sends category slug correctly; existing budget-match logic unaffected
- [ ] `dotnet test --filter "Category!=Integration"` — handler unit tests pass
- [ ] All docs listed in section 11 updated before marking complete
