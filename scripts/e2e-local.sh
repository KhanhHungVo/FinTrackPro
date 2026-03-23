#!/usr/bin/env bash
# scripts/e2e-local.sh
#
# Mints a short-lived E2E token from the local Keycloak instance and runs the
# full Playwright suite.  Run this from the repo root:
#
#   bash scripts/e2e-local.sh
#
# Prerequisites:
#   - Docker running (sqlserver + keycloak containers up)
#   - API running on http://localhost:5018
#   - curl and sed available (Git Bash / WSL / Linux — no jq or Node required)
#
# The fintrackpro-e2e Keycloak client must exist in the realm.
# It is included in infra/docker/keycloak-realm.json and auto-imported on first
# `docker compose up`. If it is missing, restart Keycloak:
#   docker compose restart keycloak
#
# The minted token is passed as E2E_TOKEN to Playwright. Playwright's auth.setup.ts
# injects it into localStorage alongside the e2e_bypass flag so the app skips the
# Keycloak login redirect and goes straight to degraded mode using the cached JWT.
# The backend still validates the JWT on every request independently.

set -euo pipefail

KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8080}"
REALM="${KEYCLOAK_REALM:-fintrackpro}"
CLIENT_ID="${E2E_CLIENT_ID:-fintrackpro-e2e}"
USERNAME="${E2E_USERNAME:-admin@fintrackpro.dev}"
PASSWORD="${E2E_PASSWORD:-Admin1234!}"

FRONTEND_DIR="$(cd "$(dirname "$0")/../frontend/fintrackpro-ui" && pwd)"

echo "==> Minting E2E token from $KEYCLOAK_URL/realms/$REALM ..."

TOKEN_RESPONSE=$(curl -sf -X POST \
  "$KEYCLOAK_URL/realms/$REALM/protocol/openid-connect/token" \
  -d "grant_type=password" \
  -d "client_id=$CLIENT_ID" \
  -d "username=$USERNAME" \
  -d "password=$PASSWORD")

E2E_TOKEN=$(echo "$TOKEN_RESPONSE" | sed 's/.*"access_token":"\([^"]*\)".*/\1/')

if [[ -z "$E2E_TOKEN" ]]; then
  echo "ERROR: Token mint failed. Keycloak response:" >&2
  echo "$TOKEN_RESPONSE" >&2
  echo "" >&2
  echo "Check that:" >&2
  echo "  - Keycloak is running on $KEYCLOAK_URL" >&2
  echo "  - The fintrackpro-e2e client exists (docker compose restart keycloak to re-import realm)" >&2
  exit 1
fi

echo "==> Token minted (${#E2E_TOKEN} chars). Running Playwright tests ..."

export E2E_TOKEN
cd "$FRONTEND_DIR"
npx playwright test "$@"
