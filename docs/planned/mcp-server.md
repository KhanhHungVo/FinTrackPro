# MCP Server for FinTrackPro

## Overview

A TypeScript MCP (Model Context Protocol) server that lets an AI agent (Claude) interact with the FinTrackPro API — creating and updating transactions and trades. The primary use case is **statement parsing**: the user uploads a bank statement image, Claude reads it, maps categories, presents a markdown preview table, asks for confirmation, then bulk-creates the records.

---

## Approach

Build a TypeScript MCP server in a new `mcp/` directory at the repo root. It uses `@modelcontextprotocol/sdk`. Transport and auth depend on the deployment environment:

| Environment | Transport | Auth |
|-------------|-----------|------|
| Local dev (Claude Desktop / Claude Code) | **stdio** | Pre-obtained JWT via `FINTRACKPRO_API_TOKEN` env var |
| Production (Claude.ai / ChatGPT) | **Streamable HTTP** | **OAuth 2.1 Authorization Code + PKCE** via Auth0 |

The server is a thin adapter: it translates MCP tool calls into authenticated HTTP requests against the FinTrackPro REST API and formats responses for Claude.

---

## Directory Structure

```
mcp/
├── src/
│   ├── index.ts              # Entry point — detects transport (stdio vs http), starts server
│   ├── server.ts             # MCP server setup, registers all tools
│   ├── http-server.ts        # Express + Streamable HTTP transport, OAuth 2.1 middleware (prod only)
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

Used with **Claude.ai** and **ChatGPT**. Multi-user — each user authenticates with their own Auth0 account. Follows the [MCP Authorization spec (2025-03-26)](https://spec.modelcontextprotocol.io/specification/2025-03-26/basic/authorization/) using OAuth 2.1 Authorization Code + PKCE.

**Flow:**

```
User opens Claude.ai / ChatGPT
       │
       ▼
AI client GETs MCP OAuth discovery document
GET https://mcp.fintrackpro.dev/.well-known/oauth-authorization-server
→ MCP server proxies Auth0's OIDC metadata
       │
       ▼
AI client redirects user's browser to Auth0 login
https://<tenant>.auth0.com/authorize
  ?client_id=<fintrackpro-mcp-client-id>
  &response_type=code
  &redirect_uri=https://claude.ai/mcp/callback   (or ChatGPT equivalent)
  &code_challenge=<PKCE S256 challenge>
  &code_challenge_method=S256
  &scope=openid profile
  &audience=https://api.fintrackpro.dev
       │
       ▼
User logs in with their FinTrackPro Auth0 credentials
       │
       ▼
Auth0 issues authorization code → redirects to AI client callback URL
       │
       ▼
AI client POSTs code + PKCE verifier to Auth0 token endpoint
POST https://<tenant>.auth0.com/oauth/token
       │
       ▼
Auth0 returns access_token (JWT, aud=https://api.fintrackpro.dev) + refresh_token
       │
       ▼
AI client calls MCP server with Bearer token on every request
Authorization: Bearer <access_token>
       │
       ▼
MCP server validates JWT via Auth0 JWKS (cached)
GET https://<tenant>.auth0.com/.well-known/jwks.json
Checks: signature, iss, aud, exp, alg=RS256
       │
       ▼
MCP tool executes → api-client.ts forwards same Bearer token to REST API
       │
       ▼
FinTrackPro .NET API validates same JWT (already configured for Auth0)
Data is scoped to the authenticated user automatically
```

**Auth0 Application setup (one-time):**

Create a new Auth0 Application:

| Field | Value |
|-------|-------|
| Type | Single Page Application (public client — PKCE, no client secret) |
| Name | `FinTrackPro MCP` |
| Allowed Callback URLs | Claude.ai callback URL, ChatGPT callback URL |
| Allowed Web Origins | `https://claude.ai`, `https://chatgpt.com` |
| Authorized APIs | `https://api.fintrackpro.dev` (existing API audience) |

**Token validation (`auth.ts`):**

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
      audience: process.env.AUTH0_AUDIENCE,   // "https://api.fintrackpro.dev"
      algorithms: ['RS256'],
    }, (err, decoded) => {
      if (err) reject(err);
      else resolve(decoded as jwt.JwtPayload);
    });
  });
}
```

**OAuth discovery endpoint (`http-server.ts`):**

```ts
// Proxy Auth0's OIDC metadata as the MCP OAuth discovery document
app.get('/.well-known/oauth-authorization-server', async (_req, res) => {
  const discovery = await fetch(
    `https://${process.env.AUTH0_DOMAIN}/.well-known/openid-configuration`
  ).then(r => r.json());
  res.json(discovery);
});
```

**Token forwarding — each tool call is user-scoped:**

```ts
// http-server.ts — extract and validate token before routing to MCP
app.use('/mcp', async (req, res, next) => {
  const token = req.headers.authorization?.replace('Bearer ', '');
  if (!token) return res.status(401).json({ error: 'Missing Bearer token' });
  try {
    req.user = await validateToken(token);
    req.token = token;
    next();
  } catch {
    res.status(401).json({ error: 'Invalid or expired token' });
  }
});

// api-client.ts — forward the user's own token to the REST API
export function createApiClient(token: string) {
  return axios.create({
    baseURL: process.env.FINTRACKPRO_API_URL,
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

**Environment variables (HTTP mode):**

| Variable | Required | Description |
|----------|----------|-------------|
| `MCP_TRANSPORT` | Yes | Set to `http` to enable HTTP mode |
| `MCP_PORT` | No | HTTP server port (default `3100`) |
| `AUTH0_DOMAIN` | Yes | Auth0 tenant domain, e.g. `<tenant>.auth0.com` |
| `AUTH0_AUDIENCE` | Yes | Auth0 API audience, e.g. `https://api.fintrackpro.dev` |
| `AUTH0_MCP_CLIENT_ID` | Yes | Auth0 Application client ID for the MCP server |
| `FINTRACKPRO_API_URL` | Yes | FinTrackPro REST API base URL |

**`sub` claim format:** Auth0 uses `auth0|<id>` (or `google-oauth2|<id>` for social logins). The .NET backend stores this in `AppUser.ExternalUserId` with `AppUser.Provider = "auth0"` — no backend changes needed.

---

## Environment comparison

| | Local dev | Production |
|---|---|---|
| Transport | stdio | Streamable HTTP |
| Auth provider | Keycloak | Auth0 |
| Token source | Env var (`FINTRACKPRO_API_TOKEN`) | OAuth 2.1 PKCE flow |
| Multi-user | No | Yes |
| Token validation | None (trusted env var) | JWKS signature + iss + aud + exp |
| Backend changes | None | None |

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

### Claude.ai / ChatGPT (production)

1. Deploy the MCP server at a public HTTPS URL, e.g. `https://mcp.fintrackpro.dev`
2. Start with HTTP transport:
   ```bash
   MCP_TRANSPORT=http \
   AUTH0_DOMAIN=<tenant>.auth0.com \
   AUTH0_AUDIENCE=https://api.fintrackpro.dev \
   AUTH0_MCP_CLIENT_ID=<client-id> \
   FINTRACKPRO_API_URL=https://api.fintrackpro.dev \
   node mcp/dist/index.js
   ```
3. Register the MCP server URL in Claude.ai / ChatGPT connector settings
4. The OAuth 2.1 PKCE flow is handled automatically by the AI client

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
   AUTH0_DOMAIN=<tenant>.auth0.com \
   AUTH0_AUDIENCE=https://api.fintrackpro.dev \
   AUTH0_MCP_CLIENT_ID=<client-id> \
   FINTRACKPRO_API_URL=http://localhost:5018 \
   node mcp/dist/index.js
   # GET http://localhost:3100/.well-known/oauth-authorization-server → 200
   ```
5. **End-to-end** (after adding to Claude Desktop config):
   - "List my recent transactions" → calls `list_transactions`
   - "Create an expense of 50,000 VND for food in April 2024" → Claude calls `list_transaction_categories`, then `create_transaction`
   - Upload a bank statement screenshot → full parsing workflow

---

## Key Design Decisions

- **Dual transport**: stdio for local/CLI use (zero config, single user); Streamable HTTP for hosted AI clients (multi-user, OAuth 2.1).
- **Auth0 for production**: Matches the existing backend auth provider. The same JWT issued by Auth0 is accepted by both the MCP server (JWKS validation) and the .NET API (already configured) — no separate token exchange needed.
- **Token forwarding**: The MCP server never issues tokens. It validates the Bearer token from the AI client and forwards it as-is to the REST API, so all authorization stays in Auth0 and the .NET backend.
- **`Promise.allSettled()` for batch ops**: Never throws on partial failure — agent always gets a per-item result so it can report exactly which rows succeeded or failed.
- **No backend changes**: Purely additive. The MCP server is a separate process that calls the existing REST API.
