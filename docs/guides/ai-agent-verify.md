# AI Agent Verification — Reusable Prompts

Paste the relevant prompt directly into a Claude Code session after finishing an implementation task.

---

## Prompt A — Backend tests

```
Run all backend tests and fix any failures.

1. cd backend && dotnet build                                         # must be zero errors
2. dotnet test --filter "Category!=Integration"                       # unit tests
3. dotnet test --filter "Category=Integration"                        # integration (needs TEST_DB_CONNECTION_STRING)
4. bash scripts/api-e2e-local.sh                                      # Newman API E2E (needs Docker + API on :5018)

For each failure: state root cause in one sentence, fix it, re-run the affected suite to confirm green.
Skip any suite whose infrastructure is not running — say so explicitly with the reason.

Report using this table:
| Step                  | Result       | Notes |
|-----------------------|--------------|-------|
| BE build              | ✅/❌        |       |
| BE unit tests         | ✅/❌/⏭ skip |       |
| BE integration tests  | ✅/❌/⏭ skip |       |
| BE API E2E (Newman)   | ✅/❌/⏭ skip |       |
List every file changed and why.
```

---

## Prompt B — Frontend tests

```
Run all frontend tests and fix any failures.

1. cd frontend/fintrackpro-ui && npm run build                        # zero TS + build errors
2. npm run lint                                                        # zero lint errors
3. npm test                                                            # Vitest unit tests
4. bash scripts/e2e-local.sh                                          # Playwright E2E (needs Keycloak + API on :5018)

For each failure: state root cause in one sentence, fix it, re-run the affected suite to confirm green.
Do not suppress lint rules or delete failing tests.
Skip any suite whose infrastructure is not running — say so explicitly with the reason.

Report using this table:
| Step                  | Result       | Notes |
|-----------------------|--------------|-------|
| FE build + type-check | ✅/❌        |       |
| FE lint               | ✅/❌        |       |
| FE unit tests         | ✅/❌/⏭ skip |       |
| FE E2E (Playwright)   | ✅/❌/⏭ skip |       |
List every file changed and why.
```

---

## Prompt C — Backend documentation review

```
Review and update backend documentation to match the current code. Do not rewrite for style — only fix factual inaccuracies or missing content.

Docs to check:
- README.md                      # project overview, tech stack, quick start
- CLAUDE.md                      # commands, architecture summary, config table
- backend/CLAUDE.md              # BE commands, architecture, testing conventions
- backend/README.md              # BE stack, commands, config, project structure
- docs/features.md               # feature registry — add/remove/update BE features
- docs/architecture/             # overview, api-spec, database, background-jobs, auth
- docs/postman/api-e2e-plan.md   # Newman collection structure and CI diagram

For each file:
1. Read the doc.
2. Read the relevant source (controllers, commands, entities, migrations, job classes) to verify accuracy.
3. Update only sections that are factually wrong or missing.
4. Skip files that are already accurate.

Report: list every change made and the code evidence that required it. Do not add speculative or planned content.
```

---

## Prompt D — Frontend documentation review

```
Review and update frontend documentation to match the current code. Do not rewrite for style — only fix factual inaccuracies or missing content.

Docs to check:
- README.md                      # project overview, tech stack, quick start
- CLAUDE.md                      # commands, architecture summary, config table
- frontend/fintrackpro-ui/CLAUDE.md   # commands, FSD layers, key patterns
- frontend/fintrackpro-ui/README.md   # stack, env vars, FSD layers, commands
- docs/features.md                    # feature registry — add/remove/update FE features
- docs/architecture/ui-flows.md       # user flows and page structure

For each file:
1. Read the doc.
2. Read the relevant source (pages, features, entities, shared — especially env.ts, routing, auth adapter) to verify accuracy.
3. Update only sections that are factually wrong or missing.
4. Skip files that are already accurate.

Report: list every change made and the code evidence that required it. Do not add speculative or planned content.
```
