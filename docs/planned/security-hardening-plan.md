# FinTrackPro — Security & Performance Hardening Guide

This document consolidates all recommended hardening measures for the Render free-tier deployment: rate limiting, response caching, HTTP security headers, HTTPS enforcement, input validation (including XSS rejection), per-user rate limiting, daily data quotas to protect the PostgreSQL tier, authentication token storage, and Cloudflare setup. Review this guide before implementing any changes.

---

## 1. Rate Limiting (ASP.NET Core built-in)

### Why
The Render free tier has fixed CPU and memory quotas. A public link shared on social media, a bot scan, or a small DDoS will wake the sleeping instance on every request, exhaust the quota, and trigger throttling at the platform level. Application-level rate limiting rejects excess requests cheaply — before authentication, database access, or external API calls are attempted.

### How — no new NuGet packages
`System.Threading.RateLimiting` and `Microsoft.AspNetCore.RateLimiting` are part of `Microsoft.AspNetCore.App` (built into .NET 7+). No additional dependency is required.

### Recommended policies

| Policy | Limit | Applied to |
|---|---|---|
| Global (per IP) | 60 requests / 60 s | All endpoints |
| `"market"` (per IP) | 10 requests / 60 s | `MarketController` (CoinGecko / Binance calls) |
| Exempt | — | `/health` |

### Middleware order — `Program.cs` after `var app = builder.Build()`

Rate limiting must come **before** `UseAuthentication` so unauthenticated bots are rejected before JWT validation overhead is incurred.

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();  // keep first
app.UseCors("AllowFrontend");                      // keep
app.UseRateLimiter();                              // ADD HERE — before auth
// ... security headers (see Section 3) ...
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Exempt health check from rate limiting
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .AllowAnonymous()
   .DisableRateLimiting();                         // ADD .DisableRateLimiting()
```

### All named policies — full `AddRateLimiter` block

The code block below is the complete `AddRateLimiter` registration. All named policies live here together so there is one place to adjust limits.

```csharp
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    // Global fixed-window: 60 req / 60 s per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 60,
                Window               = TimeSpan.FromSeconds(60),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // Market endpoints: 10 req / 60 s per IP (CoinGecko / Binance calls)
    options.AddFixedWindowLimiter("market", o =>
    {
        o.PermitLimit          = 10;
        o.Window               = TimeSpan.FromSeconds(60);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit           = 0;
    });

    // Hangfire dashboard: 10 req / 60 s per IP — brute-force protection (OWASP A06)
    options.AddFixedWindowLimiter("hangfire", o =>
    {
        o.PermitLimit          = 10;
        o.Window               = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit           = 0;
    });

    // Write endpoints: 30 req / 60 s per authenticated user ID (falls back to IP for anonymous)
    options.AddPolicy<HttpContext, string>("user-write",
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 30,
                Window               = TimeSpan.FromSeconds(60),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0
            }));

    // 429 response with Retry-After header
    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            ctx.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        await ctx.HttpContext.Response.WriteAsync(
            "{\"title\":\"Too many requests. Please retry later.\"}", token);
    };
});
```

### Apply policies to controllers and dashboard

```csharp
// MarketController.cs — expensive external API calls
[EnableRateLimiting("market")]
public class MarketController : BaseApiController { ... }

// TransactionsController, BudgetsController, TradesController — write actions only
[EnableRateLimiting("user-write")]
[HttpPost]
public async Task<IActionResult> Create(...) { ... }

// Hangfire dashboard mapping in Program.cs
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireBasicAuthFilter(app.Configuration)]
}).RequireRateLimiting("hangfire");
```

Apply `[EnableRateLimiting("user-write")]` to: `TransactionsController` (POST), `BudgetsController` (POST, PUT), `TradesController` (POST, PUT, DELETE).

### Summary of all policies

| Policy | Partition key | Limit | Applied to |
|---|---|---|---|
| Global | IP | 60 req / 60 s | All endpoints |
| `"market"` | IP | 10 req / 60 s | `MarketController` |
| `"hangfire"` | IP | 10 req / 60 s | `/hangfire` dashboard |
| `"user-write"` | User ID (fallback: IP) | 30 req / 60 s | Write actions on transactions, budgets, trades |
| Exempt | — | — | `/health` |

---

## 2. Response Caching

### Why
The CoinGecko and Fear & Greed endpoints call paid external APIs on every request. Each call counts against API quotas and adds latency. Caching responses in memory means repeated requests within the TTL window are served instantly, with zero external network calls — even from background jobs.

### Current state — caching already exists

`IMemoryCache` is already registered in `DependencyInjection.cs` via `services.AddMemoryCache()`. Both external service implementations already cache their results:

| Service | Cache key | Current TTL | Recommended TTL |
|---|---|---|---|
| `CoinGeckoService.GetTrendingCoinsAsync` | `"coingecko_trending"` | 15 minutes | **5 minutes** |
| `FearGreedService.GetIndexAsync` | `"fear_greed_index"` | 1 hour | 1 hour (appropriate — index updates once daily) |

### Change needed

In `backend/src/FinTrackPro.Infrastructure/ExternalServices/CoinGeckoService.cs`:

```csharp
// Change from:
_cache.Set(CacheKey, coins, TimeSpan.FromMinutes(15));

// To:
_cache.Set(CacheKey, coins, TimeSpan.FromMinutes(5));
```

### Why the service layer (not controller/OutputCache) is correct

`MarketSignalJob` and other Hangfire background jobs call `ICoinGeckoService` directly — they bypass controllers entirely. If caching were applied at the HTTP response layer (via `[ResponseCache]` or `OutputCache` middleware), background jobs would bypass the cache and hit CoinGecko on every run. Because the cache lives inside the service, all callers — controllers and background jobs alike — share the same in-memory result.

**Do not add `UseResponseCaching()` or `UseOutputCache()` middleware** — they would add complexity without benefiting the background job layer.

---

## 3. HTTP Security Headers

### Backend API (ASP.NET Core)

Add an inline middleware to `Program.cs` immediately after `UseRateLimiter()` and before `UseAuthentication()`. This ensures every response — including 401, 403, and 429 — carries the headers.

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"]        = "DENY";
    context.Response.Headers["Referrer-Policy"]        = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"]     = "geolocation=(), camera=(), microphone=()";
    await next(context);
});
```

| Header | Value | Purpose |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options` | `DENY` | Blocks clickjacking via iframes |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leakage on cross-origin navigations |
| `Permissions-Policy` | `geolocation=(), camera=(), microphone=()` | Disables browser APIs the app never uses |

**Do NOT add these to the API:**
- `Strict-Transport-Security` — Render's load balancer already sends HSTS from the edge. Adding it in the app is redundant and can cause issues in local development.
- `Content-Security-Policy` — CSP is meaningful for HTML documents, not for JSON API responses.

### Frontend static site (render.yaml)

Add a `headers:` block to the `fintrackpro-ui` static site entry in `render.yaml`, after the `routes:` block:

```yaml
    headers:
      - path: /*
        name: X-Frame-Options
        value: DENY
      - path: /*
        name: X-Content-Type-Options
        value: nosniff
      - path: /*
        name: Referrer-Policy
        value: strict-origin-when-cross-origin
      - path: /*
        name: Permissions-Policy
        value: "geolocation=(), camera=(), microphone=()"
      - path: /*
        name: Content-Security-Policy
        value: "default-src 'self'; connect-src 'self' https://api.fintrackpro.dev https://*.auth0.com; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self';"
      # Cache immutable hashed assets forever — Vite content-hash filenames make this safe
      - path: /assets/*
        name: Cache-Control
        value: public, max-age=31536000, immutable
      - path: /*.js
        name: Cache-Control
        value: public, max-age=31536000, immutable
      - path: /*.css
        name: Cache-Control
        value: public, max-age=31536000, immutable
```

**CSP notes:**
- `connect-src` must include the API origin and Auth0 domain — the SPA calls both at runtime.
- `script-src 'unsafe-inline'` is currently required because Vite injects inline scripts for ES module preloading. This can be eliminated later by adding a Vite nonce plugin and switching to `script-src 'nonce-{nonce}'`.
- The `/assets/*` `immutable` cache rule is safe because Vite appends a content hash to every asset filename (e.g., `main.a3f9c2.js`). A changed file gets a new hash, so cached stale files are never served for updated content.

**Terraform limitation:** The `render-oss/render ~> 1.3` Terraform provider does not expose a `headers` block on `render_static_site`. The headers above must live in `render.yaml` (Blueprint) or be set manually in the Render dashboard until the provider adds support. Add a comment to `infra/terraform/render.tf` to document this gap.

### Tighten AllowedHosts (appsettings.json)

Currently set to `"*"`, which allows host-header injection attacks. Change to:

```json
"AllowedHosts": "api.fintrackpro.dev;localhost;*.onrender.com"
```

---

## 4. Cloudflare Free Tier Setup

Cloudflare sits in front of Render and provides edge-level DDoS protection, a WAF, and CDN caching — all for free. It complements (does not replace) the application-level rate limiting above.

### 4.1 Add Site to Cloudflare

1. Sign in at [cloudflare.com](https://cloudflare.com) and click **Add a Site**.
2. Enter your custom domain (e.g., `fintrackpro.dev`) and choose the **Free** plan.
3. Cloudflare scans existing DNS records — review the imported records.
4. Update your domain registrar's nameservers to the two Cloudflare nameservers shown. Propagation takes up to 24 hours (usually under 1 hour).

### 4.2 Point Custom Domain to Render via CNAME

In the Cloudflare DNS dashboard, create:

| Type | Name | Target | Proxy |
|---|---|---|---|
| CNAME | `api` | `fintrackpro-api.onrender.com` | Orange cloud ON |
| CNAME | `@` (or `www`) | `fintrackpro-ui.onrender.com` | Orange cloud ON |

The orange cloud (proxy ON) routes traffic through Cloudflare's network, activating DDoS protection and caching. **Never use grey cloud (DNS only) for publicly shared URLs.**

In the **Render dashboard**, add the custom domains to each service so Render provisions TLS certificates for them.

After adding custom domains, update your environment configuration:
- `Cors__Origins` → `https://fintrackpro.dev`
- `VITE_API_BASE_URL` → `https://api.fintrackpro.dev`
- `AllowedHosts` in `appsettings.json` → remove `*.onrender.com`, keep `api.fintrackpro.dev;localhost`

### 4.3 DDoS Protection

Cloudflare free tier provides always-on L3/L4 DDoS mitigation by default — no configuration required once the orange cloud is enabled.

For application-layer (L7) protection:

| Mode | When to use |
|---|---|
| **Medium** (default) | Normal operation |
| **High** | When you see unusual traffic spikes in Analytics |
| **I'm Under Attack** | Only during an active targeted attack — this mode adds a JS challenge to every request, which will break the React SPA and API for all users until disabled |

Set in: **Security → Settings → Security Level**.

### 4.4 Bot Protection and Firewall Rules (free: 5 custom rules)

Navigate to **Security → WAF → Custom Rules**.

**Rule 1 — Block verified bad bots:**
```
(cf.client.bot) and not (cf.verified_bot_category in {"Search Engine Crawlers"})
```
Action: **Block**

**Rule 2 — Edge rate limiting** (Security → WAF → Rate Limiting Rules; free: 1 rule):

Limit to 100 requests per 10 seconds per IP across the entire domain. This catches volumetric floods before they reach Render, complementing the application-level 60/min policy.

### 4.5 Page Rules for Caching Static Assets (free: 3 rules)

Navigate to **Caching → Cache Rules → Add Rule**.

**Rule 1 — Cache Vite hashed assets:**
- Match: `fintrackpro.dev/assets/*`
- Cache Level: Cache Everything
- Edge Cache TTL: 1 month

**Rule 2 — Bypass cache for API:**
- Match: `api.fintrackpro.dev/*`
- Cache Level: Bypass

This ensures API responses are never served from Cloudflare's cache (critical for auth endpoints and dynamic data).

### 4.6 Protect the Render Origin URL

The `*.onrender.com` URLs are publicly resolvable and bypass Cloudflare if accessed directly. To harden the origin:

1. **Do not advertise or CORS-allow the `*.onrender.com` URLs.** Set `Cors__Origins` to the custom domain only.
2. **Set `AllowedHosts` to the custom domain** (not `*.onrender.com`) to reject requests that bypass Cloudflare.
3. **Optional — origin secret header:** Add a secret header (e.g., `X-Origin-Key: <secret>`) injected by a Cloudflare Transform Rule into every proxied request. Validate this header in the API middleware and reject requests without it. This prevents direct access to the Render origin URL entirely. This requires a paid Cloudflare feature (Transform Rules), not available on the free plan.

### 4.7 Monitoring

- **Cloudflare Analytics → Traffic** — shows bot vs. human traffic split over time.
- **Security → Events** — shows every blocked/challenged request with IP, rule, and country.
- **Notifications → Add Notification → Security Alert** — email alerts for DDoS events and spikes.

---

## 5. HTTPS Enforcement (Production)

### Why

`UseHttpsRedirection()` is currently inside the `IsDevelopment()` guard in `Program.cs` — it is **not applied in production**. HSTS is also absent. On Render, the load balancer terminates TLS at the edge, but the application-layer redirect is still the defence-in-depth backstop for any direct or misconfigured access. Render's edge already sends `Strict-Transport-Security` so adding HSTS in the app is redundant — but the redirect must be unconditional.

### Fix — `Program.cs`

Move `UseHttpsRedirection()` outside the `IsDevelopment` block:

```csharp
// Before (Development only):
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseHttpsRedirection();   // ← WRONG placement
}

// After (unconditional):
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();       // ← Move here, outside the if block
```

**Do NOT add `UseHsts()`** — Render's edge already handles HSTS headers. Adding it in the app would double-set the header and can cause issues in local HTTP development.

---

## 6. Input Validation — Missing MaxLength on Free-Text Fields

### Why

`Category` fields in `CreateTransactionCommand` and `CreateBudgetCommand` have no maximum-length constraint. An oversized value will reach the database layer and cause an unhandled EF Core / PostgreSQL exception (column overflow) instead of a clean `400 Validation failed` response.

### Fix — two validator files

**`CreateTransactionCommandValidator.cs`**

```csharp
RuleFor(v => v.Category)
    .NotEmpty().WithMessage("Category is required.")
    .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");
```

**`CreateBudgetCommandValidator.cs`**

```csharp
RuleFor(v => v.Category)
    .NotEmpty().WithMessage("Category is required.")
    .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");
```

Verify the `Category` column definition in the EF Core entity and migration — the `MaximumLength` value here should match `HasMaxLength(N)` on the column (or be smaller).

---

## 7. JWT Token Storage — Remove localStorage

### Why (OWASP A07)

The access token is stored in `localStorage` via `authStore.ts` and `AuthProvider.tsx`. Any XSS payload running in the page can read `localStorage` — tokens stored there are as exposed as the XSS surface allows.

### Recommended approach

Store the token **in memory only** (Zustand store, no persistence) and rely on the IAM SDK's silent refresh to rehydrate it across page reloads. Both Keycloak JS and Auth0 SPA SDK support silent token refresh via a hidden iframe / background call.

```ts
// authStore.ts — remove localStorage persistence

setToken: (token) => {
  // localStorage.setItem('access_token', token)  ← remove this line
  set({ accessToken: token, isAuthenticated: true })
},

logout: () => {
  // localStorage.removeItem('access_token')  ← remove this line
  set({ accessToken: null, displayName: null, email: null, isAuthenticated: false })
  authAdapter.logout(window.location.origin)
},
```

Also remove `getCachedToken()` and its `localStorage.getItem('access_token')` call from `AuthProvider.tsx`.

**E2E bypass path:** The Playwright `e2e_bypass` mechanism injects `access_token` and `e2e_bypass` into `localStorage` for test runs. Once production storage moves to memory, the E2E setup will need to inject the token directly into the Zustand store (via `page.evaluate`) rather than `localStorage`. Update `auth.setup.ts` accordingly.

> This is the highest-priority frontend security fix. The CSP in Section 3 (frontend headers) reduces — but does not eliminate — the XSS surface that could exploit a `localStorage`-stored token.

---

## 8. Auth Failure Logging

### Why (OWASP A09)

JWT authentication failures (expired token, invalid signature, wrong audience) are handled silently by the ASP.NET Core JWT middleware — they return `401` with no application-level log entry. Without logging, repeated brute-force or token-replay attempts are invisible in Serilog / Render log streams.

### Fix — register `JwtBearerEvents` in `Program.cs`

Add an `Events` block inside the `AddJwtBearer` options (applies equally to the `auth0` and `keycloak` branches):

```csharp
options.Events = new JwtBearerEvents
{
    OnAuthenticationFailed = ctx =>
    {
        var logger = ctx.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();
        logger.LogWarning(
            "JWT authentication failed from {IP} on {Path}: {Error}",
            ctx.HttpContext.Connection.RemoteIpAddress,
            ctx.HttpContext.Request.Path,
            ctx.Exception.GetType().Name);
        return Task.CompletedTask;
    }
};
```

**Do not log `ctx.Exception.Message`** — it may contain fragments of the raw token.

---

## 9. CI — Add `npm audit` Step

### Why (OWASP A03)

The frontend CI job uses `npm ci` (good — lockfile-pinned), but there is no automated step to surface known CVEs in the dependency tree. A new vulnerability in a transitive dependency would go undetected until the next manual audit.

### Fix — `.github/workflows/ci.yml`

Add one step to the `frontend` job, after `Install` and before `Unit Tests`:

```yaml
- name: Audit dependencies
  run: npm audit --audit-level=high
```

`--audit-level=high` fails the job only for High and Critical severity CVEs — it does not block on Low/Moderate findings, which avoids alert fatigue from noise.

---

## 10. XSS Input Sanitization (WSTG-INPV-01)

### Why (OWASP A03)

FluentValidation enforces structural constraints (type, length, pattern) but does **not** strip or encode HTML/script content. A `Category` or `Note` value containing `<script>alert(1)</script>` passes current validators and is stored verbatim in the database. If that string is ever rendered without encoding on the frontend, it becomes a stored XSS vector.

### Current risk level

**Low-to-medium for this application.** The React frontend uses JSX's automatic HTML encoding — `{transaction.category}` never renders raw HTML. The risk is only realized if a `dangerouslySetInnerHTML` call, a third-party component, or a future Telegram notification template renders the string without encoding. Defence-in-depth requires validation at the API boundary, but the strictness level must match the field's semantic domain to avoid breaking legitimate input.

### Validation strategy — field-specific, informed by WSTG

[OWASP WSTG-INPV-01](https://owasp.org/www-project-web-security-testing-guide/latest/4-Web_Application_Security_Testing/07-Input_Validation_Testing/README) and the [Input Validation Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Input_Validation_Cheat_Sheet.html) recommend allowlisting over denylisting wherever the field domain is narrow enough to define precisely. For broad free-text fields, they acknowledge that output encoding is the primary XSS control — input validation adds depth but must not block legitimate data.

This leads to two distinct strategies for the two field types in this application:

| Field type | Strategy | Rationale |
|---|---|---|
| `Category` | **Allowlist** — define what is valid | Short labels; domain is well-defined; a strict allowlist covers all real use without false positives |
| `Note` / `Notes` | **Targeted denylist** (`<` and `>` only) + MaxLength | Free prose; characters like `$`, `&`, `"`, `/` are legitimate in financial notes; blocking more than HTML tag delimiters causes false positives with no security gain |

### Fix — `Category` fields (allowlist)

```csharp
// CreateTransactionCommandValidator.cs, CreateBudgetCommandValidator.cs, UpdateBudgetCommandValidator.cs
RuleFor(v => v.Category)
    .NotEmpty().WithMessage("Category is required.")
    .MaximumLength(100).WithMessage("Category must not exceed 100 characters.")
    .Matches(@"^[\p{L}\p{N}\s\-\/\.\&\(\)]+$")
    .WithMessage("Category must contain only letters, numbers, spaces, and common punctuation (- / . & ( )).");
```

**What `\p{L}\p{N}` covers:** any Unicode letter or digit — Latin, Arabic, CJK, accented characters (e.g., `café`, `日用品`, `Médicos`) all pass. This avoids breaking non-ASCII user data.

**What is allowed:** letters · digits · space · `-` `/` `.` `&` `(` `)` — sufficient for labels like `Food & Drink`, `Rent/Mortgage`, `401(k)`, `U.S. Bonds`.

**What is blocked:** `< > " ' ; = \` ` and any character outside the allowlist — sufficient to prevent tag injection in a short label.

### Fix — `Note` / `Notes` fields (targeted denylist + MaxLength)

```csharp
// CreateTransactionCommandValidator.cs, UpdateTradeCommandValidator.cs
RuleFor(v => v.Note)
    .MaximumLength(1000).WithMessage("Note must not exceed 1000 characters.")
    .Matches(@"^[^<>]*$")
    .WithMessage("Note must not contain angle brackets (< >).")
    .When(v => !string.IsNullOrEmpty(v.Note));
```

The denylist here is intentionally narrow: only `<` and `>` are rejected because they are the delimiters required to form any HTML or script tag. Characters like `$`, `"`, `&`, `→`, and `%` remain valid because users legitimately write things like `"Bought $1,200 EUR → USD"` or `"P&L rebalance +3.5%"`. Output encoding (JSX interpolation) handles the rest.

Apply to:

| File | Field(s) | Strategy |
|---|---|---|
| `CreateTransactionCommandValidator.cs` | `Category` | Allowlist |
| `CreateTransactionCommandValidator.cs` | `Note` | Denylist `<>` + MaxLength |
| `CreateBudgetCommandValidator.cs` | `Category` | Allowlist |
| `UpdateBudgetCommandValidator.cs` | `Category` | Allowlist |
| `UpdateTradeCommandValidator.cs` | `Notes` | Denylist `<>` + MaxLength |

**Unicode normalization:** Normalize to NFC before validation to prevent homoglyph bypass (e.g., U+FF1C `＜` normalizes to `<`). Apply in the MediatR `ValidationBehavior` or at the top of each validator before rules fire:

```csharp
// At the point of reading the field value
var normalized = value?.Normalize(NormalizationForm.FormC);
```

**Important:** Do NOT sanitize (strip/replace) the input — reject it with a `400` so the client is forced to send clean data. Sanitizing silently changes user intent.

> Input validation is **not** the primary XSS defence — it is one layer. The primary defence is output encoding. JSX's automatic HTML encoding is what actually prevents a stored `<script>` tag from executing. Both layers together provide the defence-in-depth WSTG recommends.

### Frontend — no `dangerouslySetInnerHTML` rule

Enforce in code review and lint:

- Never use `dangerouslySetInnerHTML` with user-supplied data
- All user strings must be rendered via JSX interpolation (`{value}`) — JSX encodes `< > &` automatically

---

## 11. Per-Authenticated-User Rate Limiting

### Why

The current global rate limiter (Section 1) partitions by **IP address**. This is correct for unauthenticated bot traffic, but has two gaps:

1. Multiple users behind a corporate NAT or shared IPv4 share the same partition — one heavy user can starve others at the same IP.
2. A single authenticated user could rotate through IP addresses (VPN, mobile networks) and effectively bypass the IP-based limit.

For endpoints that write to or read from the database (transactions, budgets, trades), a per-user-ID partition provides a tighter guarantee.

### Recommended policy

| Policy | Limit | Applied to |
|---|---|---|
| Global per IP (existing) | 60 req / 60 s | All endpoints |
| `"user-write"` per authenticated user | 30 req / 60 s | POST/PUT/DELETE on transactions, budgets, trades |
| `"market"` per IP (existing) | 10 req / 60 s | `MarketController` |

### Implementation

The `"user-write"` policy and its controller annotations are defined in Section 1 alongside all other rate-limit policies. See the **Summary of all policies** table in §1 for the complete picture.

---

## 12. Daily Data Quota — Protecting the 1 GB Free PostgreSQL Tier

### Why

Render's free PostgreSQL tier enforces a **1 GB storage limit**. A single user inserting large volumes of transactions, trades, or notes can exhaust the quota before other users have meaningful data. There is no platform-level per-user quota — this must be enforced at the application layer.

### Recommended limits (User role)

| Entity | Suggested daily write limit | Rationale |
|---|---|---|
| Transactions | 100 per user per day | Normal personal finance use is well under 20/day |
| Trades | 50 per user per day | Active traders rarely log more than this manually |
| Budget entries | 12 per user per month | One per expense category — practical ceiling |
| Watched symbols | 20 total (not daily) | Watchlist cap prevents unbounded list growth |

These are starting points — adjust based on observed usage once analytics are in place.

### Implementation approach — Application layer quota check

Add a quota check inside each `CreateXxx` command handler, **before** calling the repository `AddAsync`:

```csharp
// Example: CreateTransactionCommandHandler.cs
private const int DailyTransactionLimit = 100;

var today = DateTime.UtcNow.Date;
var todayCount = await _transactionRepository.CountByUserAndDateAsync(
    request.UserId, today, cancellationToken);

if (todayCount >= DailyTransactionLimit)
    throw new DomainException(
        $"Daily transaction limit of {DailyTransactionLimit} reached. Try again tomorrow.");
```

Add a `CountByUserAndDateAsync(Guid userId, DateTime date, CancellationToken ct)` method to `ITransactionRepository` and its EF Core implementation:

```csharp
public async Task<int> CountByUserAndDateAsync(Guid userId, DateTime date, CancellationToken ct)
    => await _db.Transactions
        .CountAsync(t => t.UserId == userId
                      && t.CreatedAt.Date == date.Date, ct);
```

> **Index required:** Add a composite index on `(UserId, CreatedAt)` in the EF Core entity config to keep this count query fast as data grows:
> ```csharp
> entity.HasIndex(t => new { t.UserId, t.CreatedAt });
> ```

### Budget entry monthly cap

Budget entries are monthly by nature (`BudgetMonth` = `YYYY-MM`). Add a uniqueness check in `CreateBudgetCommandHandler`: one budget per user per category per month already (or enforce via a unique index on `(UserId, Category, BudgetMonth)`). A `MaximumLength(100)` on `Category` (§6) combined with the unique index prevents category-proliferation abuse.

### Configuration — make limits configurable

Hard-coding limits in handlers is brittle. Bind them from `appsettings.json` via an options class so you can tune them without a redeploy:

```json
"Quotas": {
  "DailyTransactionLimit": 100,
  "DailyTradeLimit": 50,
  "MaxWatchedSymbols": 20
}
```

```csharp
// In DependencyInjection.cs (Application layer)
services.Configure<QuotaOptions>(configuration.GetSection("Quotas"));
```

Inject `IOptions<QuotaOptions>` into each handler.

### Quota exhaustion response

Return `429 Too Many Requests` (not `400`) so clients can distinguish quota exhaustion from validation failure. Map `QuotaExceededException` (a new subclass of `DomainException`) to `429` in `ExceptionHandlingMiddleware`:

```csharp
QuotaExceededException e => (StatusCodes.Status429TooManyRequests,
    "quota_exceeded", e.Message),
```

---

## 13. Verification Checklist

Run these checks after completing all sections. Tests are grouped by implementation phase — verify Phase 1 before moving to Phase 2, and so on.

### Phase 1 — Rate limiting, caching, headers, Cloudflare (§1–4)

```bash
# Health check must never be rate-limited
curl -I https://api.fintrackpro.dev/health
# → HTTP 200, no Retry-After header

# Market endpoint: 11th request within 60 s should be rejected
for i in $(seq 1 11); do curl -s -o /dev/null -w "%{http_code}\n" https://api.fintrackpro.dev/market/trending; done
# → First 10: 200, 11th: 429

# Hangfire: 11th request within 60 s should be rejected
for i in $(seq 1 11); do curl -s -o /dev/null -w "%{http_code}\n" https://api.fintrackpro.dev/hangfire; done
# → First 10: 401 (auth required), 11th: 429

# AllowedHosts — reject spoofed Host header
curl -H "Host: evil.com" https://api.fintrackpro.dev/health
# → HTTP 400 Bad Request

# Security headers (API)
curl -I https://api.fintrackpro.dev/health
# → X-Frame-Options: DENY
# → X-Content-Type-Options: nosniff
# → Referrer-Policy: strict-origin-when-cross-origin
# → Permissions-Policy: geolocation=(), ...

# Security headers (frontend)
curl -I https://fintrackpro.dev
# → Content-Security-Policy: default-src 'self'; ...
# → X-Frame-Options: DENY
# → X-Content-Type-Options: nosniff

# Immutable asset caching
curl -I https://fintrackpro.dev/assets/index.abc123.js
# → Cache-Control: public, max-age=31536000, immutable

# CoinGecko cache — check Render logs
# Within a 5-minute window, only one outbound request to api.coingecko.com should appear
# per endpoint regardless of how many frontend users load the market page.
```

### Phase 2 — HTTPS, validation, JWT storage, auth logging, CI (§5–9)

```bash
# HTTPS redirect
curl -I http://api.fintrackpro.dev/health
# → HTTP 301 Location: https://api.fintrackpro.dev/health

# Input validation — MaxLength (§6)
curl -X POST https://api.fintrackpro.dev/api/transactions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"type":1,"amount":100,"category":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","budgetMonth":"2026-03"}'
# → HTTP 400 {"title":"Validation failed",...}

# JWT auth failure logging
curl -H "Authorization: Bearer invalid.jwt.token" https://api.fintrackpro.dev/api/transactions
# Check Render log stream for:
# [WRN] JWT authentication failed from x.x.x.x on /api/transactions: SecurityTokenSignatureKeyNotFoundException

# npm audit (CI) — check GitHub Actions frontend job
# The "Audit dependencies" step should appear and return exit 0 on a clean run.
```

### Phase 3 — XSS validation, per-user rate limiting, daily quotas (§10–12)

```bash
# Category: character outside allowlist → 400
curl -X POST https://api.fintrackpro.dev/api/transactions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"type":1,"amount":50,"category":"<script>x</script>","budgetMonth":"2026-03"}'
# → HTTP 400 {"title":"Validation failed",...}

# Category: valid label with common punctuation → 201
curl -X POST https://api.fintrackpro.dev/api/transactions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"type":1,"amount":50,"category":"Food & Drink","budgetMonth":"2026-03"}'
# → HTTP 201

# Note: angle brackets → 400
curl -X POST https://api.fintrackpro.dev/api/transactions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"type":1,"amount":50,"category":"Income","note":"<img src=x>","budgetMonth":"2026-03"}'
# → HTTP 400 {"title":"Validation failed",...}

# Note: $ and & are allowed → 201
curl -X POST https://api.fintrackpro.dev/api/transactions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"type":1,"amount":50,"category":"Income","note":"P&L rebalance +$1,200","budgetMonth":"2026-03"}'
# → HTTP 201

# Per-user write rate limit: 31st rapid POST should return 429
for i in $(seq 1 31); do
  curl -s -o /dev/null -w "%{http_code}\n" \
    -X POST https://api.fintrackpro.dev/api/transactions \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"type":1,"amount":1,"category":"test","budgetMonth":"2026-03"}'
done
# → First 30: 201, 31st: 429

# Daily quota — test in staging with DailyTransactionLimit lowered to 3 for faster iteration
# After limit is reached, the next POST should return 429 with "quota_exceeded" error code.
```

---

## 14. Implementation Order

When ready to implement, apply changes in this sequence to keep each step independently testable:

1. `CoinGeckoService.cs` — cache TTL (one line, zero risk) (§2)
2. `CreateTransactionCommandValidator.cs` + `CreateBudgetCommandValidator.cs` — add `MaximumLength(100)` on `Category` (§6)
3. All free-text validators — `Category`: allowlist `[\p{L}\p{N}\s\-\/\.\&\(\)]+`; `Note`/`Notes`: denylist `^[^<>]*$` + `MaximumLength` (§10)
4. `.github/workflows/ci.yml` — add `npm audit --audit-level=high` step (§9)
5. `Program.cs` — move `UseHttpsRedirection()` outside the Development guard (§5)
6. `Program.cs` — add complete `AddRateLimiter` block: global + `"market"` + `"hangfire"` + `"user-write"` policies + `OnRejected` handler; add `UseRateLimiter()` middleware (§1)
7. `Program.cs` — add `OnAuthenticationFailed` logging to `JwtBearerEvents` (§8)
8. `MarketController.cs` — add `[EnableRateLimiting("market")]` (§1)
9. `TransactionsController`, `BudgetsController`, `TradesController` — add `[EnableRateLimiting("user-write")]` on write actions (§1, §11)
10. `Program.cs` — wire up Hangfire dashboard with `.RequireRateLimiting("hangfire")` (§1)
11. **Quota domain setup** (§12):
    - a. `Domain` — add `QuotaExceededException` as a subclass of `DomainException`
    - b. `Application` — add `QuotaOptions` record; register `services.Configure<QuotaOptions>(...)` in `DependencyInjection.cs`
    - c. `Infrastructure` — add `CountByUserAndDateAsync` to `ITransactionRepository` and its EF Core implementation; add composite index `HasIndex(t => new { t.UserId, t.CreatedAt })`
    - d. `Application` — inject `IOptions<QuotaOptions>` into each `CreateXxx` handler; add quota check before `AddAsync`
12. `ExceptionHandlingMiddleware.cs` — map `QuotaExceededException` → 429 with `"quota_exceeded"` error code (§12)
13. `appsettings.json` — add `Quotas` section + tighten `AllowedHosts` (§3, §12)
14. `Program.cs` — add security headers `app.Use(...)` block (§3)
15. `render.yaml` — add `headers:` block; redeploy static site (§3)
16. `infra/terraform/render.tf` — add comment about headers limitation (§3)
17. `authStore.ts` + `AuthProvider.tsx` — remove `localStorage` token persistence; update `auth.setup.ts` to inject token via `page.evaluate` into Zustand store instead of `localStorage` (§7) — **coordinate with E2E test run before deploying**
18. Cloudflare — DNS, rules, and monitoring setup (§4)
