# Health Checks — External Services

## Background: Health Check vs Smoke Test

Two separate concepts, often confused:

| | Health Check (`/health`) | Smoke Test |
|---|---|---|
| **Purpose** | Runtime signal: is the app alive and can it serve traffic? | Post-deploy validation: did this deployment work end-to-end? |
| **Who calls it** | Load balancer / orchestrator (Render, K8s) — continuously | CD pipeline — once, right after `deploy` completes |
| **Failure action** | Route traffic away / roll back deploy | Block promotion to next environment / alert |
| **How deep** | Shallow ping (`SELECT 1`, `GET /ping`) | Can be a real API call through the full stack |
| **Runs in** | Production, continuously | CI/CD pipeline, post-deploy only |

**Current state:**
- `render.yaml` and `infra/terraform/render.tf` both set `healthCheckPath: /health` — Render probes this during every deploy to decide whether to promote or roll back.
- `/health` currently only checks the DB. External services (Binance, CoinGecko, Telegram) can be down and Render will still promote.

---

## Proposed: Extend `/health` with External Service Checks

### Rule: `Degraded` (not `Unhealthy`) for external services

Only mark `Unhealthy` for things that make the app completely non-functional:

| Service | Probe | Failure status | Reason |
|---|---|---|---|
| PostgreSQL | `SELECT 1` (already done) | `Unhealthy` | App cannot function at all |
| Binance | `GET /api/v3/ping` — no auth, no cost | `Degraded` | Trading features impaired, core app works |
| CoinGecko | `GET /api/v3/ping` — no API key needed | `Degraded` | Market data impaired, core app works |
| Fear & Greed | `GET /fng/?limit=1` — free, no key | `Degraded` | Signals impaired, core app works |
| Telegram | `GET /bot{token}/getMe` — no message sent | `Degraded` | Notifications fail, core app works; skip if no token |

`Degraded` → Render still promotes (no false rollbacks on third-party blips), but the status is visible in monitoring.

### Optional: split liveness vs readiness endpoints

```
/health/live   → always 200, no checks (process is running)
/health/ready  → DB + external checks (ready to serve traffic)
/health        → alias for /ready (keeps Render config unchanged)
```

---

## Implementation Plan

### 1. New files (all in `backend/src/FinTrackPro.Infrastructure/HealthChecks/`)

| File | Probe |
|---|---|
| `BinanceHealthCheck.cs` | `GET /api/v3/ping` |
| `CoinGeckoHealthCheck.cs` | `GET /api/v3/ping` |
| `FearGreedHealthCheck.cs` | `GET /fng/?limit=1` |
| `TelegramHealthCheck.cs` | `GET /bot{token}/getMe` |

Each implements `IHealthCheck`. Inject `IHttpClientFactory` directly — do **not** depend on `IBinanceService` etc. to avoid coupling health check logic to service logic.

### 2. Register in DI

**`backend/src/FinTrackPro.Infrastructure/DependencyInjection.cs`** — after the existing DB health checks (line ~165):

```csharp
hc.AddCheck<BinanceHealthCheck>("binance", failureStatus: HealthStatus.Degraded,
    tags: ["external"]);
hc.AddCheck<CoinGeckoHealthCheck>("coingecko", failureStatus: HealthStatus.Degraded,
    tags: ["external"]);
hc.AddCheck<FearGreedHealthCheck>("feargreed", failureStatus: HealthStatus.Degraded,
    tags: ["external"]);
// Only register if token is configured (Telegram is optional in this app)
if (!string.IsNullOrEmpty(configuration["Telegram:BotToken"]))
    hc.AddCheck<TelegramHealthCheck>("telegram", failureStatus: HealthStatus.Degraded,
        tags: ["external"]);
```

### 3. Rich JSON response

**`backend/src/FinTrackPro.API/Program.cs`** — replace the plain `MapHealthChecks` call:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();
```

Requires adding `AspNetCore.HealthChecks.UI.Client` to `FinTrackPro.Infrastructure.csproj` (lightweight — no dashboard, just the JSON writer).

---

## About Smoke Tests (future work)

Smoke tests are not yet in this project. When added, they would:
- Run in the CD pipeline after `render deploy` completes
- Call `GET /health` and assert overall status is not `Unhealthy`
- Optionally call a real authenticated endpoint with a test-user token to verify end-to-end flow

This is separate from health check work.

---

## Verification

```bash
cd backend
dotnet build

# Start API (requires Docker DB up or user-secrets set)
dotnet run --project src/FinTrackPro.API

# Inspect per-check results
curl http://localhost:5018/health | jq .
# Expected: { "status": "Healthy", "checks": { "postgresql": "Healthy", "binance": "Healthy", ... } }
```
