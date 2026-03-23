# FinTrackPro — Security & Performance Hardening Guide

This document consolidates all recommended hardening measures for the Render free-tier deployment: rate limiting, response caching, HTTP security headers, and Cloudflare setup. Review this guide before implementing any changes.

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

### Service registration — add before `var app = builder.Build()`

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

    // Named policy for expensive market endpoints: 10 req / 60 s per IP
    options.AddFixedWindowLimiter("market", o =>
    {
        o.PermitLimit          = 10;
        o.Window               = TimeSpan.FromSeconds(60);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit           = 0;
    });

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

### Apply stricter policy to MarketController

```csharp
[EnableRateLimiting("market")]
public class MarketController : BaseApiController { ... }
```

The `/hangfire` dashboard is protected by the Admin role filter and shares the global 60/min policy — no additional attribute is needed.

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

## 5. Verification Checklist

After implementing these changes, verify each area:

### Rate limiting
```bash
# Health check must never be rate-limited
curl -I https://api.fintrackpro.dev/health
# → HTTP 200, no Retry-After header

# Market endpoint: 11th request within 60 s should be rejected
for i in $(seq 1 11); do curl -s -o /dev/null -w "%{http_code}\n" https://api.fintrackpro.dev/market/trending; done
# → First 10: 200, 11th: 429
```

### Security headers (API)
```bash
curl -I https://api.fintrackpro.dev/health
# → X-Frame-Options: DENY
# → X-Content-Type-Options: nosniff
# → Referrer-Policy: strict-origin-when-cross-origin
# → Permissions-Policy: geolocation=(), ...
```

### Security headers (frontend)
```bash
curl -I https://fintrackpro.dev
# → Content-Security-Policy: default-src 'self'; ...
# → X-Frame-Options: DENY
# → X-Content-Type-Options: nosniff

curl -I https://fintrackpro.dev/assets/index.abc123.js
# → Cache-Control: public, max-age=31536000, immutable
```

### AllowedHosts
```bash
curl -H "Host: evil.com" https://api.fintrackpro.dev/health
# → HTTP 400 Bad Request
```

### CoinGecko cache
Check Render logs — within a 5-minute window, only one outbound HTTP request to `api.coingecko.com` should appear per endpoint, regardless of how many frontend users load the market page.

---

## 6. Implementation Order

When ready to implement, apply changes in this sequence to keep each step independently testable:

1. `CoinGeckoService.cs` — cache TTL (one line, zero risk)
2. `Program.cs` — add `AddRateLimiter` registration and `UseRateLimiter()` middleware
3. `MarketController.cs` — add `[EnableRateLimiting("market")]`
4. `Program.cs` — add security headers `app.Use(...)` block
5. `appsettings.json` — tighten `AllowedHosts`
6. `render.yaml` — add `headers:` block; redeploy static site
7. `infra/terraform/render.tf` — add comment about headers limitation
8. Cloudflare — DNS, rules, and monitoring setup
