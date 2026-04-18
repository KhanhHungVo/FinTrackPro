#!/usr/bin/env bash
# rotate-render-db.sh
#
# Rotates the Render free-tier PostgreSQL database:
#   1. Discovers the current DB (name prefix: fintrackpro-db)
#   2. Creates a new free DB (fintrackpro-db-YYYY-MM)
#   3. Waits for it to become available
#   4. pg_dump old → pg_restore new
#   5. Updates ConnectionStrings__DefaultConnection on the API service
#   6. Polls the API health endpoint until healthy
#   7. Prints old DB ID for manual deletion after you verify data
#
# Required env vars:
#   RENDER_API_KEY    — Render personal API key
#   RENDER_OWNER_ID   — Render owner/team ID
#   RENDER_SERVICE_ID — Render web service ID for fintrackpro-api (srv-...)
#   API_HEALTH_URL    — Health endpoint to poll (e.g. https://fintrackpro-api.onrender.com/health)
#
# Usage: bash scripts/rotate-render-db.sh

set -euo pipefail

RENDER_API="https://api.render.com/v1"
DB_NAME_PREFIX="fintrackpro-db"
DB_REGION="oregon"
DB_VERSION="18"
DB_PLAN="free"
PROVISION_TIMEOUT=300   # seconds to wait for new DB to become available
PROVISION_INTERVAL=10   # polling interval (seconds)
HEALTH_TIMEOUT=180      # seconds to wait for API to recover after env var update
HEALTH_INTERVAL=15      # polling interval (seconds)
ENV_VAR_KEY="ConnectionStrings__DefaultConnection"

# ── helpers ────────────────────────────────────────────────────────────────────

log()  { echo "[$(date -u '+%H:%M:%S')] $*"; }
err()  { echo "[$(date -u '+%H:%M:%S')] ERROR: $*" >&2; exit 1; }

check_deps() {
  local missing=()
  for cmd in curl jq pg_dump pg_restore; do
    command -v "$cmd" &>/dev/null || missing+=("$cmd")
  done
  [[ ${#missing[@]} -eq 0 ]] || err "Missing required tools: ${missing[*]}"
}

render_get() {
  local path="$1"
  curl -sf \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    -H "Accept: application/json" \
    "${RENDER_API}${path}"
}

render_post() {
  local path="$1" body="$2"
  curl -sf -X POST \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    -H "Content-Type: application/json" \
    -H "Accept: application/json" \
    -d "$body" \
    "${RENDER_API}${path}"
}

render_put() {
  local path="$1" body="$2"
  curl -sf -X PUT \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    -H "Content-Type: application/json" \
    -H "Accept: application/json" \
    -d "$body" \
    "${RENDER_API}${path}"
}

render_delete() {
  local path="$1"
  curl -sf -X DELETE \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    "${RENDER_API}${path}"
}

# ── validate inputs ─────────────────────────────────────────────────────────────

check_deps

: "${RENDER_API_KEY:?RENDER_API_KEY is required}"
: "${RENDER_OWNER_ID:?RENDER_OWNER_ID is required}"
: "${RENDER_SERVICE_ID:?RENDER_SERVICE_ID is required}"
: "${API_HEALTH_URL:?API_HEALTH_URL is required}"

# ── step 1: discover old DB ────────────────────────────────────────────────────

log "Step 1 — Discovering current database..."

DB_LIST=$(render_get "/postgres?ownerId=${RENDER_OWNER_ID}&limit=20")
OLD_DB=$(echo "$DB_LIST" | jq -r \
  --arg prefix "$DB_NAME_PREFIX" \
  '[.[] | select(.postgres.name | startswith($prefix))] | sort_by(.postgres.createdAt) | last | .postgres')

[[ "$OLD_DB" != "null" && -n "$OLD_DB" ]] || err "No database with name prefix '$DB_NAME_PREFIX' found."

OLD_DB_ID=$(echo "$OLD_DB" | jq -r '.id')
OLD_DB_NAME=$(echo "$OLD_DB" | jq -r '.name')
OLD_EXTERNAL_URL=$(echo "$OLD_DB" | jq -r '.connectionInfo.externalConnectionString')

log "  Found: $OLD_DB_NAME (id=$OLD_DB_ID)"

# ── step 2: create new DB ──────────────────────────────────────────────────────

NEW_DB_NAME="${DB_NAME_PREFIX}-$(date -u '+%Y-%m')"
log "Step 2 — Creating new database: $NEW_DB_NAME..."

CREATE_BODY=$(jq -n \
  --arg name "$NEW_DB_NAME" \
  --arg owner "$RENDER_OWNER_ID" \
  --arg plan "$DB_PLAN" \
  --arg region "$DB_REGION" \
  --arg version "$DB_VERSION" \
  '{name: $name, ownerId: $owner, plan: $plan, region: $region, version: $version}')

NEW_DB_RESP=$(render_post "/postgres" "$CREATE_BODY")
NEW_DB_ID=$(echo "$NEW_DB_RESP" | jq -r '.id // .postgres.id')

[[ -n "$NEW_DB_ID" && "$NEW_DB_ID" != "null" ]] || err "Failed to create new database. Response: $NEW_DB_RESP"

log "  Created: $NEW_DB_NAME (id=$NEW_DB_ID)"

# ── step 3: wait for new DB to be available ─────────────────────────────────────

log "Step 3 — Waiting for new database to become available (timeout: ${PROVISION_TIMEOUT}s)..."

elapsed=0
while true; do
  DB_INFO=$(render_get "/postgres/${NEW_DB_ID}")
  STATUS=$(echo "$DB_INFO" | jq -r '.status // .postgres.status')

  if [[ "$STATUS" == "available" ]]; then
    NEW_INTERNAL_URL=$(echo "$DB_INFO" | jq -r \
      '.connectionInfo.internalConnectionString // .postgres.connectionInfo.internalConnectionString')
    NEW_EXTERNAL_URL=$(echo "$DB_INFO" | jq -r \
      '.connectionInfo.externalConnectionString // .postgres.connectionInfo.externalConnectionString')
    log "  New DB is available."
    break
  fi

  if (( elapsed >= PROVISION_TIMEOUT )); then
    err "New DB did not become available within ${PROVISION_TIMEOUT}s (last status: $STATUS). " \
        "New DB id=$NEW_DB_ID is still running — delete it manually if you want to retry."
  fi

  log "  Status: $STATUS — waiting ${PROVISION_INTERVAL}s... (${elapsed}s elapsed)"
  sleep "$PROVISION_INTERVAL"
  (( elapsed += PROVISION_INTERVAL ))
done

# ── step 4: dump + restore ─────────────────────────────────────────────────────

log "Step 4 — Migrating data (pg_dump | pg_restore)..."

# pg_dump produces a custom-format archive; pg_restore deserializes it.
# --no-owner and --no-acl ensure roles from the old instance are not applied to the new one.
pg_dump --format=custom --no-acl --no-owner "$OLD_EXTERNAL_URL" \
  | pg_restore --no-owner --no-acl --exit-on-error -d "$NEW_EXTERNAL_URL"

log "  Data migration complete."

# ── step 5: update API service env var ────────────────────────────────────────

log "Step 5 — Updating API service env var ($ENV_VAR_KEY)..."

# Render PUT /env-vars requires the full list of env vars.
CURRENT_ENV_VARS=$(render_get "/services/${RENDER_SERVICE_ID}/env-vars")

UPDATED_ENV_VARS=$(echo "$CURRENT_ENV_VARS" | jq \
  --arg key "$ENV_VAR_KEY" \
  --arg val "$NEW_INTERNAL_URL" \
  '[.[] | if .envVar.key == $key then .envVar.value = $val else . end | .envVar]')

render_put "/services/${RENDER_SERVICE_ID}/env-vars" "$UPDATED_ENV_VARS" >/dev/null

log "  Env var updated. Service will restart automatically."

# ── step 6: verify API health ─────────────────────────────────────────────────

log "Step 6 — Polling API health (timeout: ${HEALTH_TIMEOUT}s)..."
log "  Endpoint: $API_HEALTH_URL"

elapsed=0
while true; do
  HTTP_STATUS=$(curl -so /dev/null -w "%{http_code}" --max-time 10 "$API_HEALTH_URL" || true)

  if [[ "$HTTP_STATUS" == "200" ]]; then
    log "  API is healthy (HTTP 200)."
    break
  fi

  if (( elapsed >= HEALTH_TIMEOUT )); then
    echo ""
    echo "================================================================="
    echo "  HEALTH CHECK FAILED after ${HEALTH_TIMEOUT}s (last HTTP: $HTTP_STATUS)"
    echo "  Both databases are still intact."
    echo "  To roll back: update $ENV_VAR_KEY on the API service in the"
    echo "  Render dashboard to point back to the old DB."
    echo "    Old DB id : $OLD_DB_ID"
    echo "    New DB id : $NEW_DB_ID"
    echo "================================================================="
    exit 1
  fi

  log "  HTTP $HTTP_STATUS — waiting ${HEALTH_INTERVAL}s... (${elapsed}s elapsed)"
  sleep "$HEALTH_INTERVAL"
  (( elapsed += HEALTH_INTERVAL ))
done

# ── step 7: print summary ─────────────────────────────────────────────────────

echo ""
echo "================================================================="
echo "  DB ROTATION COMPLETE"
echo "================================================================="
echo "  Old DB   : $OLD_DB_NAME (id=$OLD_DB_ID)"
echo "  New DB   : $NEW_DB_NAME (id=$NEW_DB_ID)"
echo "  API      : healthy"
echo ""
echo "  The OLD database is still running."
echo "  After you verify data in the app, delete it:"
echo ""
echo "    curl -X DELETE \\"
echo "      -H \"Authorization: Bearer \$RENDER_API_KEY\" \\"
echo "      \"https://api.render.com/v1/postgres/${OLD_DB_ID}\""
echo ""
echo "  Or delete it from the Render dashboard:"
echo "    https://dashboard.render.com/d/${OLD_DB_ID}"
echo "================================================================="
