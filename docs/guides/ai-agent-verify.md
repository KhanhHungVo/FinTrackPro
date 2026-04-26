# AI Agent Verification Checklist

Run this checklist after every implementation task before reporting completion.
Fix all failures before marking the task done — do not skip steps or report
partial success.

---

## 1. Backend

### 1a. Build

```bash
cd backend
dotnet build
```

All projects must compile with **zero errors**. Warnings are acceptable but
investigate any new ones introduced by the current task.

### 1b. Unit tests

```bash
cd backend
dotnet test --filter "Category!=Integration"
```

All tests must pass. If a test fails:
1. Read the failure message and stack trace carefully.
2. Determine whether the test or the implementation is wrong.
3. Fix the root cause — do not comment out or delete failing tests.

### 1c. Integration tests

> **Prerequisites:** PostgreSQL running locally; `TEST_DB_CONNECTION_STRING`
> environment variable set.  
> Skip this step only when the local database is genuinely unavailable — note
> the skip explicitly in your summary.

```bash
cd backend
dotnet test --filter "Category=Integration"
```

All integration tests must pass. Fix failures before proceeding.

### 1d. API E2E tests (Newman)

> **Prerequisites:**
> - Docker up: `docker compose up -d postgres keycloak`
> - API running on `http://localhost:5018`
> - Newman installed: `npm install -g newman`
>
> Skip this step only when the full stack cannot be started locally — note the
> skip explicitly in your summary.

```bash
# Full suite
bash scripts/api-e2e-local.sh

# Scope to the affected feature folder only (faster, use when confident)
bash scripts/api-e2e-local.sh --folder "<FolderName>"

# Verbose output for debugging failures
bash scripts/api-e2e-local.sh --verbose
```

If a Newman test fails:
1. Identify the failing request and assertion.
2. Check whether the API response, the Postman test script, or the environment
   variable is wrong.
3. Fix the root cause (API code **or** collection update) — both are valid fixes.

---

## 2. Frontend

### 2a. Type-check and build

```bash
cd frontend/fintrackpro-ui
npm run build
```

Zero TypeScript errors. Zero build errors. Fix all before proceeding.

### 2b. Lint

```bash
cd frontend/fintrackpro-ui
npm run lint
```

Zero lint errors. Warnings are acceptable; do not suppress rules to silence
them.

### 2c. Unit tests (Vitest)

```bash
cd frontend/fintrackpro-ui
npm test
```

All tests must pass. Apply the same fix-or-update rule as backend tests.

### 2d. Frontend E2E tests (Playwright)

> **Prerequisites:**
> - Docker up: `docker compose up -d postgres keycloak`
> - API running on `http://localhost:5018`
> - Frontend dev server running: `npm run dev` in `frontend/fintrackpro-ui`
>
> Skip this step only when the full stack cannot be started locally — note the
> skip explicitly in your summary.

```bash
# Full suite (mints Keycloak token automatically)
bash scripts/e2e-local.sh

# Single spec (scope to affected feature)
bash scripts/e2e-local.sh tests/e2e/<spec-file>.spec.ts

# Interactive UI mode (useful for debugging)
bash scripts/e2e-local.sh --ui
```

If a Playwright test fails:
1. Read the error and any screenshot/trace artifacts in `test-results/`.
2. Determine whether the UI, the API, or the test assertion is wrong.
3. Fix the root cause — do not skip or mark tests as flaky without evidence.

---

## 3. Documentation sync

After any change to public API endpoints, request/response shapes,
configuration keys, environment variables, or project structure:

- [ ] `docs/architecture/api-spec.md` — updated if endpoints changed
- [ ] `docs/architecture/database.md` — updated if schema changed
- [ ] `CLAUDE.md` key-configuration table — updated if env vars changed
- [ ] `README.md` / `backend/README.md` / `frontend/fintrackpro-ui/README.md` — updated if setup steps changed
- [ ] Postman collection (`docs/postman/FinTrackPro.e2e.postman_collection.json`) — updated if API contract changed

---

## 4. Summary format

When all steps pass, report using this format:

```
## Verification summary

| Step                        | Result  | Notes                        |
|-----------------------------|---------|------------------------------|
| BE build                    | ✅ pass  |                              |
| BE unit tests               | ✅ pass  | 142 tests                    |
| BE integration tests        | ✅ pass  | 18 tests                     |
| BE API E2E (Newman)         | ✅ pass  | 34 requests                  |
| FE build + type-check       | ✅ pass  |                              |
| FE lint                     | ✅ pass  |                              |
| FE unit tests               | ✅ pass  | 56 tests                     |
| FE E2E (Playwright)         | ⏭ skip  | Stack not running locally    |
| Docs sync                   | ✅ done  | api-spec.md updated          |
```

If any step was skipped, state the reason. If any step required a fix, briefly
describe what was wrong and what was changed.
