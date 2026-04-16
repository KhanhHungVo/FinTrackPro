# MCP Server for FinTrackPro

## Overview

A TypeScript MCP (Model Context Protocol) server that lets an AI agent (Claude or ChatGPT) interact with the FinTrackPro API — creating and updating transactions and trades. The primary use case is **statement parsing**: the user uploads a bank statement image, the AI reads it, maps categories, presents a markdown preview table, asks for confirmation, then bulk-creates the records.

---

## Approach

Build a TypeScript MCP server in a new `mcp/` directory at the repo root. It uses `@modelcontextprotocol/sdk`. Transport and auth depend on the deployment environment:

| Environment | Transport | Auth |
|-------------|-----------|------|
| Local dev (Claude Desktop / Claude Code) | **stdio** | Pre-obtained JWT via `FINTRACKPRO_API_TOKEN` env var |
| Production — Claude.ai | **Streamable HTTP** (`/mcp`) | **OAuth 2.1 Authorization Code + PKCE** via Auth0 |
| Production — ChatGPT | **SSE** (`/sse`) | **OAuth 2.1 Authorization Code + PKCE** via Auth0 |

Both production transports share the same auth middleware, token validation, and tool implementations. The Express app exposes both `/mcp` and `/sse` simultaneously.

The server is a thin adapter: it translates MCP tool calls into authenticated HTTP requests against the FinTrackPro REST API and formats responses for Claude.

---

## Directory Structure

```
mcp/
├── src/
│   ├── index.ts              # Entry point — detects transport (stdio vs http), starts server
│   ├── server.ts             # MCP server setup, registers all tools
│   ├── http-server.ts        # Express app: mounts auth middleware + both /mcp and /sse transports
│   ├── auth.ts               # JWKS token validation, token → userId
│   ├── api-client.ts         # Axios instance: auth headers, error normalization
│   ├── types.ts              # TypeScript types matching API DTOs
│   └── tools/
│       ├── transactions.ts   # list, create, batch_create, update, delete
│       ├── trades.ts         # list, create, batch_create, update, close, delete
│       └── categories.ts     # list_transaction_categories
├── package.json
├── tsconfig.json
└── .env.example
```

---

## Tools (12 total)

### Category tools

| Tool | API | Description |
|------|-----|-------------|
| `list_transaction_categories` | `GET /api/transaction-categories` | Returns all categories with IDs, slugs, labels, icons, and type. Agent calls this first when parsing statements to map names → valid `categoryId` GUIDs. |

### Transaction tools

| Tool | API | Description |
|------|-----|-------------|
| `list_transactions` | `GET /api/transactions` | Paginated list. Filters: page, pageSize, search, month (YYYY-MM), type (Income/Expense), categoryId, sortBy, sortDir. |
| `create_transaction` | `POST /api/transactions` | Create single. Fields: type, amount, currency (3-char), categoryId (guid), note?, budgetMonth (YYYY-MM). |
| `batch_create_transactions` | N× `POST /api/transactions` | Create multiple in parallel via `Promise.allSettled()`. Returns per-item success/failure + summary. Core tool for statement import. |
| `update_transaction` | `PATCH /api/transactions/{id}` | Update by ID. Fields: type, amount, currency, category (name), note?, categoryId?. |
| `delete_transaction` | `DELETE /api/transactions/{id}` | Delete by ID. |

### Trade tools

| Tool | API | Description |
|------|-----|-------------|
| `list_trades` | `GET /api/trades` | Paginated list. Filters: page, pageSize, search, status (Open/Closed), direction (Long/Short), dateFrom, dateTo, sortBy, sortDir. |
| `create_trade` | `POST /api/trades` | Create single. Fields: symbol, direction, status, entryPrice, exitPrice?, currentPrice?, positionSize, fees, currency, notes?. |
| `batch_create_trades` | N× `POST /api/trades` | Create multiple in parallel. Same pattern as `batch_create_transactions`. |
| `update_trade` | `PUT /api/trades/{id}` | Full replace by ID. |
| `close_trade` | `PATCH /api/trades/{id}/close` | Close a position. Fields: exitPrice, fees. Returns realized P&L. |
| `delete_trade` | `DELETE /api/trades/{id}` | Delete by ID. |

---

## Authentication

### Phase 1 — Local dev (stdio, Keycloak)

Used with **Claude Desktop** and **Claude Code**. Single-user, no browser redirect needed.

**Flow:**

```
User sets FINTRACKPRO_API_TOKEN in env
       │
       ▼
MCP server starts via stdio
       │
       ▼
Every tool call → api-client.ts injects token as Bearer header
       │
       ▼
FinTrackPro API validates JWT against local Keycloak
```

**Getting a token (local dev):**

Use the helper script at `scripts/get-mcp-token.sh` (wraps the Keycloak direct-grant flow):

```bash
# Export credentials first, then run:
export KEYCLOAK_USER=<your-username>
export KEYCLOAK_PASSWORD=<your-password>
eval $(bash scripts/get-mcp-token.sh)
# Sets FINTRACKPRO_API_TOKEN in your shell
```

The script itself (`scripts/get-mcp-token.sh`):
```bash
#!/usr/bin/env bash
set -euo pipefail

KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8080}"
REALM="fintrackpro"
CLIENT_ID="fintrackpro-frontend"

: "${KEYCLOAK_USER:?Set KEYCLOAK_USER before running this script}"
: "${KEYCLOAK_PASSWORD:?Set KEYCLOAK_PASSWORD before running this script}"

TOKEN=$(curl -sf -X POST \
  "${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/token" \
  -d "grant_type=password&client_id=${CLIENT_ID}&username=${KEYCLOAK_USER}&password=${KEYCLOAK_PASSWORD}" \
  | jq -r '.access_token')

echo "export FINTRACKPRO_API_TOKEN=${TOKEN}"
```

> Credentials are never defaulted in the script — `KEYCLOAK_USER` and `KEYCLOAK_PASSWORD` must be set in the environment. The local dev Keycloak credentials are documented in `docs/guides/dev-setup.md`.

**Environment variables (stdio mode):**

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `FINTRACKPRO_API_TOKEN` | Yes | — | JWT Bearer token from Keycloak (local dev) |
| `FINTRACKPRO_API_URL` | No | `http://localhost:5018` | API base URL |

---

### Phase 2 — Production (Streamable HTTP, Auth0)

Used with **Claude.ai** and **ChatGPT**. Multi-user — each user authenticates with their own Auth0 account. Follows the [MCP Authorization spec (2025-11-25)](https://modelcontextprotocol.io/specification/2025-11-25/basic/authorization/) using OAuth 2.1 Authorization Code + PKCE.

The Express app exposes **two transports on the same port**:
- **`/mcp`** — Streamable HTTP (MCP 2025-11-25, used by Claude.ai)
- **`/sse`** — Server-Sent Events (legacy transport, used by ChatGPT's Apps & Connectors UI)

Both transports share the same auth middleware and tool implementations.

#### Key spec changes vs 2025-03-26

| Area | Old (2025-03-26) | New (2025-11-25) |
|------|-----------------|-----------------|
| Resource server discovery | MCP server proxied AS metadata at `/.well-known/oauth-authorization-server` | MCP server **MUST** implement **OAuth 2.0 Protected Resource Metadata** ([RFC 9728](https://datatracker.ietf.org/doc/html/rfc9728)) — exposes its own metadata and points to the AS |
| Client registration | Dynamic Client Registration (RFC 7591) only | **Client ID Metadata Documents** is the preferred mechanism; Dynamic Client Registration is a fallback for backwards compatibility |
| `resource` parameter | Optional | **MUST** be included in every authorization and token request ([RFC 8707](https://www.rfc-editor.org/rfc/rfc8707.html)) |
| Token audience validation | Implicit | MCP server **MUST** explicitly validate that tokens were issued for it; token passthrough to upstream APIs is forbidden |
| Scope selection | Not specified | Defined priority: use `scope` from `WWW-Authenticate` → `scopes_supported` from Protected Resource Metadata → omit |
| Step-up auth | Not specified | Defined step-up flow for `insufficient_scope` (HTTP 403) errors at runtime |
| PKCE discovery | Not specified | Server **MUST** advertise `code_challenge_methods_supported`; client **MUST** refuse to proceed if absent |

#### Flow

```
User opens Claude.ai / ChatGPT
       │
       ▼
AI client sends unauthenticated request to MCP server
       │
       ▼
MCP server responds: 401 Unauthorized
WWW-Authenticate: Bearer resource_metadata="https://mcp.fintrackpro.dev/.well-known/oauth-protected-resource",
                         scope="openid profile"
       │
       ▼
AI client fetches Protected Resource Metadata
GET https://mcp.fintrackpro.dev/.well-known/oauth-protected-resource
→ { "resource": "https://mcp.fintrackpro.dev",
    "authorization_servers": ["https://<tenant>.auth0.com"],
    "scopes_supported": ["openid", "profile"],
    "bearer_methods_supported": ["header"] }
       │
       ▼
AI client discovers Auth0 metadata (tries in order):
1. GET https://<tenant>.auth0.com/.well-known/oauth-authorization-server
2. GET https://<tenant>.auth0.com/.well-known/openid-configuration  ← Auth0 responds here
→ Verifies code_challenge_methods_supported contains "S256" (required)
       │
       ▼
AI client determines client identity (priority order):
1. Pre-registered client ID (if available)
2. Client ID Metadata Document (if AS advertises client_id_metadata_document_supported)
3. Dynamic Client Registration (fallback)
       │
       ▼
AI client redirects user's browser to Auth0 login
https://<tenant>.auth0.com/authorize
  ?client_id=<mcp-client-id>
  &response_type=code
  &redirect_uri=https://claude.ai/mcp/callback        (Claude.ai)
               https://chatgpt.com/aip/mcp/callback   (ChatGPT — exact URL from connector setup)
  &code_challenge=<PKCE S256 challenge>
  &code_challenge_method=S256
  &scope=openid profile
  &resource=https://mcp.fintrackpro.dev        ← RFC 8707 resource parameter (REQUIRED)
       │
       ▼
User logs in with their FinTrackPro Auth0 credentials
       │
       ▼
Auth0 issues authorization code → redirects to AI client callback URL
       │
       ▼
AI client POSTs code + PKCE verifier + resource to Auth0 token endpoint
POST https://<tenant>.auth0.com/oauth/token
  code=<auth_code>&code_verifier=<verifier>
  &resource=https://mcp.fintrackpro.dev        ← RFC 8707 resource parameter (REQUIRED)
       │
       ▼
Auth0 returns access_token (JWT, aud=https://mcp.fintrackpro.dev) + refresh_token
       │
       ▼
AI client calls MCP server with Bearer token on every request
Authorization: Bearer <access_token>
       │
       ▼
MCP server validates JWT via Auth0 JWKS (cached)
GET https://<tenant>.auth0.com/.well-known/jwks.json
Checks: signature, iss, aud=https://mcp.fintrackpro.dev, exp, alg=RS256
MUST reject tokens not explicitly issued for this server (audience binding)
       │
       ▼
MCP tool executes → api-client.ts obtains a separate token for the REST API
(The MCP client token MUST NOT be forwarded to the REST API — token passthrough is forbidden)
api-client.ts uses client credentials flow: Auth0 M2M token for https://api.fintrackpro.dev
scoped to the authenticated user via the validated sub claim
       │
       ▼
FinTrackPro .NET API validates the M2M token (aud=https://api.fintrackpro.dev)
Data is scoped to the authenticated user automatically via sub claim
```

> **Token audience boundary:** The 2025-11-25 spec explicitly forbids token passthrough. The MCP client token (audience `https://mcp.fintrackpro.dev`) is validated on the MCP server and must not be forwarded to the REST API. The MCP server must obtain a separate token for API calls. The simplest compliant approach: the MCP server acts as an OAuth client itself and uses Auth0 client credentials + the validated `sub` to call the REST API on behalf of the user.

#### Auth0 Application setup (one-time)

| Application | Type | Purpose |
|-------------|------|---------|
| `FinTrackPro MCP` | Single Page Application (public client) | AI clients authorize against this — issues tokens with `aud=https://mcp.fintrackpro.dev` |
| `FinTrackPro MCP Server` | Machine-to-Machine | MCP server calls the REST API — issues tokens with `aud=https://api.fintrackpro.dev` |

**`FinTrackPro MCP` (SPA) settings:**

| Field | Value |
|-------|-------|
| Allowed Callback URLs | Claude.ai callback URL, ChatGPT callback URL (obtained from each connector's setup UI) |
| Allowed Web Origins | `https://claude.ai`, `https://chatgpt.com` |
| Authorized APIs | `https://mcp.fintrackpro.dev` (new MCP server audience) |

> **ChatGPT callback URL:** OpenAI does not publish a canonical callback URL. The exact URL is displayed when you add a custom connector in ChatGPT's Apps & Connectors settings. Copy it from there and add it to Auth0 before testing. The domain is typically `chatgpt.com` or `api.openai.com` depending on the integration type (consumer UI vs Responses API).

> **Claude.ai vs ChatGPT client registration:** Claude.ai follows the MCP 2025-11-25 spec and supports Client ID Metadata Documents / Dynamic Client Registration. ChatGPT acts as a **pre-registered client** — it presents its own `client_id` which must be whitelisted in Auth0. No dynamic registration is required on ChatGPT's side.

**API audiences required in Auth0:**

| Audience | Used by |
|----------|---------|
| `https://api.fintrackpro.dev` | Existing REST API (unchanged) |
| `https://mcp.fintrackpro.dev` | MCP server token validation |

#### Transport setup (`http-server.ts`)

The Express app exposes Protected Resource Metadata (RFC 9728), a shared auth middleware, and **both transports**:

```ts
import express from 'express';
import { StreamableHTTPServerTransport } from '@modelcontextprotocol/sdk/server/streamableHttp.js';
import { SSEServerTransport } from '@modelcontextprotocol/sdk/server/sse.js';
import { createMcpServer } from './server.js';
import { validateToken } from './auth.js';

const app = express();
app.use(express.json());

// ── RFC 9728 Protected Resource Metadata (required by MCP 2025-11-25 spec) ──
app.get('/.well-known/oauth-protected-resource', (_req, res) => {
  res.json({
    resource: process.env.MCP_SERVER_URL,           // "https://mcp.fintrackpro.dev"
    authorization_servers: [`https://${process.env.AUTH0_DOMAIN}`],
    scopes_supported: ['openid', 'profile'],
    bearer_methods_supported: ['header'],
  });
});

// ── Shared auth middleware — applied to both /mcp and /sse ──
async function requireAuth(req: express.Request, res: express.Response, next: express.NextFunction) {
  const token = req.headers.authorization?.replace('Bearer ', '');
  if (!token) {
    return res.status(401).set(
      'WWW-Authenticate',
      `Bearer resource_metadata="${process.env.MCP_SERVER_URL}/.well-known/oauth-protected-resource", scope="openid profile"`
    ).json({ error: 'Authorization required' });
  }
  try {
    (req as any).user = await validateToken(token);
    (req as any).token = token;
    next();
  } catch {
    res.status(401).json({ error: 'Invalid or expired token' });
  }
}

// ── Streamable HTTP transport — Claude.ai ──
app.all('/mcp', requireAuth, async (req, res) => {
  const server = createMcpServer();
  const transport = new StreamableHTTPServerTransport({ sessionIdGenerator: undefined });
  await server.connect(transport);
  await transport.handleRequest(req, res, req.body);
});

// ── SSE transport — ChatGPT Apps & Connectors ──
// GET /sse opens the event stream; POST /sse/message delivers client messages
const sseSessions = new Map<string, SSEServerTransport>();

app.get('/sse', requireAuth, async (req, res) => {
  const transport = new SSEServerTransport('/sse/message', res);
  sseSessions.set(transport.sessionId, transport);
  res.on('close', () => sseSessions.delete(transport.sessionId));

  const server = createMcpServer();
  await server.connect(transport);
  await transport.start();
});

app.post('/sse/message', requireAuth, async (req, res) => {
  const sessionId = req.query.sessionId as string;
  const transport = sseSessions.get(sessionId);
  if (!transport) return res.status(404).json({ error: 'Session not found' });
  await transport.handlePostMessage(req, res, req.body);
});

export { app };
```

> **Why two transports on one port:** Claude.ai implements the 2025-11-25 Streamable HTTP spec; ChatGPT's consumer connector UI currently uses the SSE transport. Sharing one Express app means one deployed service, one TLS certificate, and one Auth0 callback URL domain to register.

#### Token validation (`auth.ts`)

```ts
import jwksClient from 'jwks-rsa';
import jwt from 'jsonwebtoken';

const client = jwksClient({
  jwksUri: `https://${process.env.AUTH0_DOMAIN}/.well-known/jwks.json`,
  cache: true,
  rateLimit: true,
});

export async function validateToken(token: string): Promise<jwt.JwtPayload> {
  const getKey: jwt.GetPublicKeyOrSecret = (header, callback) => {
    client.getSigningKey(header.kid, (err, key) => {
      callback(err, key?.getPublicKey());
    });
  };
  return new Promise((resolve, reject) => {
    jwt.verify(token, getKey, {
      issuer: `https://${process.env.AUTH0_DOMAIN}/`,
      // MUST validate audience is this MCP server, not the REST API
      audience: process.env.MCP_SERVER_URL,   // "https://mcp.fintrackpro.dev"
      algorithms: ['RS256'],
    }, (err, decoded) => {
      if (err) reject(err);
      else resolve(decoded as jwt.JwtPayload);
    });
  });
}
```

#### API client — token isolation (`api-client.ts`)

The MCP server must not forward the MCP client token to the REST API. It must obtain its own token:

```ts
// Obtain a token for the REST API via Auth0 M2M client credentials
let cachedApiToken: { token: string; expiresAt: number } | null = null;

async function getApiToken(): Promise<string> {
  if (cachedApiToken && Date.now() < cachedApiToken.expiresAt - 30_000) {
    return cachedApiToken.token;
  }
  const response = await fetch(`https://${process.env.AUTH0_DOMAIN}/oauth/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      grant_type: 'client_credentials',
      client_id: process.env.AUTH0_M2M_CLIENT_ID,
      client_secret: process.env.AUTH0_M2M_CLIENT_SECRET,
      audience: process.env.AUTH0_AUDIENCE,   // "https://api.fintrackpro.dev"
    }),
  });
  const { access_token, expires_in } = await response.json();
  cachedApiToken = { token: access_token, expiresAt: Date.now() + expires_in * 1000 };
  return access_token;
}

// Create API client with the M2M token — never the MCP client token
export async function createApiClient() {
  const token = await getApiToken();
  return axios.create({
    baseURL: process.env.FINTRACKPRO_API_URL,
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

> The `.NET` backend must accept M2M tokens issued via client credentials. This is standard OAuth 2.1 and requires no changes if Auth0 is already configured with the M2M application authorized for the `https://api.fintrackpro.dev` audience.

#### Environment variables (HTTP mode)

| Variable | Required | Description |
|----------|----------|-------------|
| `MCP_TRANSPORT` | Yes | Set to `http` to enable HTTP mode |
| `MCP_PORT` | No | HTTP server port (default `3100`) |
| `MCP_SERVER_URL` | Yes | Public HTTPS URL of the MCP server, e.g. `https://mcp.fintrackpro.dev` |
| `AUTH0_DOMAIN` | Yes | Auth0 tenant domain, e.g. `<tenant>.auth0.com` |
| `AUTH0_AUDIENCE` | Yes | REST API audience, e.g. `https://api.fintrackpro.dev` |
| `AUTH0_M2M_CLIENT_ID` | Yes | M2M Application client ID (MCP server → REST API) |
| `AUTH0_M2M_CLIENT_SECRET` | Yes | M2M Application client secret |
| `FINTRACKPRO_API_URL` | Yes | FinTrackPro REST API base URL |

**`sub` claim format:** Auth0 uses `auth0|<id>` (or `google-oauth2|<id>` for social logins). The .NET backend stores this in `AppUser.ExternalUserId` with `AppUser.Provider = "auth0"` — no backend changes needed.

---

## Environment comparison

| | Local dev | Production — Claude.ai | Production — ChatGPT |
|---|---|---|---|
| Transport | stdio | Streamable HTTP (`/mcp`) | SSE (`/sse`) |
| Auth provider | Keycloak | Auth0 | Auth0 |
| Token source | Env var (`FINTRACKPRO_API_TOKEN`) | OAuth 2.1 PKCE (Claude.ai handles browser redirect) | OAuth 2.1 PKCE (ChatGPT handles browser redirect) |
| Multi-user | No | Yes | Yes |
| Token validation | None (trusted env var) | JWKS + iss + aud (`https://mcp.fintrackpro.dev`) + exp | JWKS + iss + aud (`https://mcp.fintrackpro.dev`) + exp |
| Token passthrough | N/A | Forbidden — MCP server uses separate M2M token | Forbidden — MCP server uses separate M2M token |
| Resource discovery | N/A | RFC 9728 `/.well-known/oauth-protected-resource` | RFC 9728 `/.well-known/oauth-protected-resource` |
| Client registration | N/A | Client ID Metadata Documents (preferred) → DCR (fallback) | Pre-registered — ChatGPT presents its own `client_id` |
| Auth0 app changes | None | Register Claude.ai callback URL | Register ChatGPT callback URL (from connector setup UI) |
| Backend changes | None | None | None |

---

## Error Handling

All tools catch API errors and return descriptive strings (never throw), so Claude can reason about failures:

| Status | Message |
|--------|---------|
| 401 | `Authentication failed: invalid or expired token` |
| 400 | `Validation failed: {field}: {error}` |
| 402 | `Plan limit exceeded: {message}. User needs to upgrade.` |
| 403 | `Not authorized to modify this resource.` |
| 404 | `Resource not found: {id}` |
| 409 | `Conflict: {message}` (e.g., trade already closed) |
| 500 | `API error: {message}` |

---

## Agent Workflow — Statement Parsing

This flow is orchestrated by Claude (not the MCP server itself). The tools just need to be wired correctly:

1. User uploads a bank statement image to Claude
2. Claude reads it using multimodal vision
3. Claude calls `list_transaction_categories` → gets categories with IDs
4. Claude extracts transactions from the image, maps descriptions to category IDs
5. Claude shows the user a markdown preview table:
   ```
   | # | Month    | Type    | Amount | Currency | Category       | Note   |
   |---|----------|---------|--------|----------|----------------|--------|
   | 1 | 2024-04  | Expense | 50000  | VND      | Food & Drinks  | Lunch  |
   | 2 | 2024-04  | Income  | 500000 | VND      | Salary         |        |
   ```
6. Claude asks: "Does this look correct? Any changes before I import?"
7. User confirms (or requests edits — Claude adjusts and re-shows)
8. Claude calls `batch_create_transactions` with all confirmed rows
9. Claude reports: "15 of 16 transactions created. 1 failed: ..."

---

## Setup & Build

```bash
cd mcp
npm install
npm run build     # outputs to mcp/dist/
npm run typecheck # zero errors
```

### Distribution via npx (recommended)

Once the package is published to npm (public or scoped private), users can run the server without cloning or building the repo:

```json
{
  "mcpServers": {
    "fintrackpro": {
      "command": "npx",
      "args": ["-y", "@fintrackpro/mcp-server"],
      "env": {
        "FINTRACKPRO_API_TOKEN": "<your-keycloak-token>",
        "FINTRACKPRO_API_URL": "http://localhost:5018"
      }
    }
  }
}
```

During development (before publishing), use `npm link` as a local substitute:

```bash
cd mcp && npm link
# Now `fintrackpro-mcp-server` is available globally
```

Then reference it by name instead of an absolute path.

### Claude Desktop (local dev)

Add to `~/Library/Application Support/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "fintrackpro": {
      "command": "node",
      "args": ["/absolute/path/to/FinTrackPro/mcp/dist/index.js"],
      "env": {
        "FINTRACKPRO_API_TOKEN": "<your-keycloak-token>",
        "FINTRACKPRO_API_URL": "http://localhost:5018"
      }
    }
  }
}
```

### Claude Code (local dev)

Add to `.mcp.json` at the repo root (create if it does not exist):

```json
{
  "mcpServers": {
    "fintrackpro": {
      "command": "node",
      "args": ["mcp/dist/index.js"],
      "env": {
        "FINTRACKPRO_API_TOKEN": "<your-keycloak-token>",
        "FINTRACKPRO_API_URL": "http://localhost:5018"
      }
    }
  }
}
```

> **Note:** `.claude/settings.json` is for Claude Code hooks and other settings — not MCP server config. Claude Code reads MCP servers from `.mcp.json` (project-level) or `~/.claude.json` (user-level).

### Claude.ai (production)

1. Deploy the MCP server at a public HTTPS URL, e.g. `https://mcp.fintrackpro.dev`
2. Start with HTTP transport (exposes both `/mcp` and `/sse` simultaneously):
   ```bash
   MCP_TRANSPORT=http \
   MCP_SERVER_URL=https://mcp.fintrackpro.dev \
   AUTH0_DOMAIN=<tenant>.auth0.com \
   AUTH0_AUDIENCE=https://api.fintrackpro.dev \
   AUTH0_M2M_CLIENT_ID=<m2m-client-id> \
   AUTH0_M2M_CLIENT_SECRET=<m2m-client-secret> \
   FINTRACKPRO_API_URL=https://api.fintrackpro.dev \
   node mcp/dist/index.js
   ```
3. In Claude.ai Settings → Integrations, add: `https://mcp.fintrackpro.dev/mcp`
4. Claude.ai will trigger the OAuth 2.1 PKCE flow automatically on first use

### ChatGPT (production)

1. Same deployed server as Claude.ai — no additional deployment needed
2. In ChatGPT Settings → Apps & Connectors → Add App, enter: `https://mcp.fintrackpro.dev/sse`
3. ChatGPT will display its OAuth callback URL during setup — copy it and add it to the Auth0 `FinTrackPro MCP` app's **Allowed Callback URLs** before completing setup
4. ChatGPT will trigger the OAuth 2.1 PKCE flow when the user connects the app

---

## Verification

1. **Build**: `cd mcp && npm run build` — `dist/` produced with no errors
2. **Type check**: `npm run typecheck` — zero TypeScript errors
3. **Smoke test — stdio** (with API running at port 5018):
   ```bash
   FINTRACKPRO_API_TOKEN=<token> node mcp/dist/index.js
   # Process stays alive (stdin open) — no startup errors
   ```
4. **Smoke test — HTTP** (with API running at port 5018):
   ```bash
   MCP_TRANSPORT=http MCP_PORT=3100 \
   MCP_SERVER_URL=http://localhost:3100 \
   AUTH0_DOMAIN=<tenant>.auth0.com \
   AUTH0_AUDIENCE=https://api.fintrackpro.dev \
   AUTH0_M2M_CLIENT_ID=<m2m-client-id> \
   AUTH0_M2M_CLIENT_SECRET=<m2m-client-secret> \
   FINTRACKPRO_API_URL=http://localhost:5018 \
   node mcp/dist/index.js

   # Protected Resource Metadata (RFC 9728)
   # GET http://localhost:3100/.well-known/oauth-protected-resource → 200
   #   { "resource": "http://localhost:3100", "authorization_servers": ["https://<tenant>.auth0.com"], ... }

   # Streamable HTTP — Claude.ai transport
   # GET http://localhost:3100/mcp (no token) → 401
   #   WWW-Authenticate: Bearer resource_metadata="http://localhost:3100/.well-known/oauth-protected-resource", scope="openid profile"

   # SSE — ChatGPT transport
   # GET http://localhost:3100/sse (no token) → 401 (same WWW-Authenticate header)
   # GET http://localhost:3100/sse (with valid Bearer token) → 200, Content-Type: text/event-stream
   ```
5. **End-to-end** (after adding to Claude Desktop config):
   - "List my recent transactions" → calls `list_transactions`
   - "Create an expense of 50,000 VND for food in April 2024" → Claude calls `list_transaction_categories`, then `create_transaction`
   - Upload a bank statement screenshot → full parsing workflow

---

## Key Design Decisions

- **Triple transport**: stdio for local/CLI use (zero config, single user); Streamable HTTP (`/mcp`) for Claude.ai (MCP 2025-11-25); SSE (`/sse`) for ChatGPT's Apps & Connectors UI (which still uses the legacy SSE transport). All three share the same tool implementations — transport selection is handled entirely in `http-server.ts` and `index.ts`.
- **Auth0 for production**: Matches the existing backend auth provider. Auth0 issues separate tokens per audience — one for the MCP server (`https://mcp.fintrackpro.dev`) and one for the REST API (`https://api.fintrackpro.dev`), keeping token audiences cleanly separated.
- **Token isolation (no passthrough)**: Per the 2025-11-25 spec, the MCP server validates the AI client's Bearer token (audience `https://mcp.fintrackpro.dev`) and then obtains a separate M2M token to call the REST API. Token passthrough is explicitly forbidden by the spec.
- **Protected Resource Metadata (RFC 9728)**: Required by the 2025-11-25 spec. The MCP server exposes `/.well-known/oauth-protected-resource` so AI clients can discover the Auth0 authorization server without any pre-configuration. The 401 response includes a `WWW-Authenticate` header pointing to this document.
- **`resource` parameter (RFC 8707)**: Required in both authorization and token requests. Binds issued tokens to their intended audience, preventing cross-service token reuse.
- **`Promise.allSettled()` for batch ops**: Never throws on partial failure — agent always gets a per-item result so it can report exactly which rows succeeded or failed.
- **No backend changes**: Purely additive. The MCP server is a separate process that calls the existing REST API.
