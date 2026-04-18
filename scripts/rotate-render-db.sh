#!/usr/bin/env bash
# rotate-render-db.sh
#
# Rotates the Render free-tier PostgreSQL database:
#   1. Discovers the current DB (name prefix: fintrackpro-db)
#   2. pg_dump old DB → temp file
#   3. Deletes old DB via Render API
#   4. Creates a new free DB (fintrackpro-db-YYYY-MM)
#   5. Waits for it to become available
#   6. pg_restore from temp file
#   7. Updates ConnectionStrings__DefaultConnection on the API service
#   8. Polls the API health endpoint until healthy
#
# NOTE: There is a brief window between step 3 and step 7 where the API has
#       no valid database connection. This is unavoidable on Render's free
#       tier, which allows only one active PostgreSQL at a time.
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
DUMP_FILE="$(mktemp /tmp/fintrackpro-db-dump.XXXXXX)"

# ── helpers ────────────────────────────────────────────────────────────────────

log()  { echo "[$(date -u '+%H:%M:%S')] $*"; }
err()  { echo "[$(date -u '+%H:%M:%S')] ERROR: $*" >&2; exit 1; }

cleanup() {
  [[ -f "$DUMP_FILE" ]] && rm -f "$DUMP_FILE" && log "Temp dump file removed."
}
trap cleanup EXIT

check_deps() {
  local missing=()
  for cmd in curl jq pg_dump pg_restore; do
    command -v "$cmd" &>/dev/null || missing+=("$cmd")
  done
  [[ ${#missing[@]} -eq 0 ]] || err "Missing required tools: ${missing[*]}"
}

render_get() {
  local path="$1"
  local resp http_code
  resp=$(curl -s -w "\n%{http_code}" \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    -H "Accept: application/json" \
    "${RENDER_API}${path}")
  http_code=$(echo "$resp" | tail -n1)
  body=$(echo "$resp" | head -n -1)
  if [[ "$http_code" -lt 200 || "$http_code" -ge 300 ]]; then
    err "GET ${path} failed (HTTP $http_code): $body"
  fi
  echo "$body"
}

render_post() {
  local path="$1" data="$2"
  local resp http_code body
  resp=$(curl -s -w "\n%{http_code}" -X POST \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    -H "Content-Type: application/json" \
    -H "Accept: application/json" \
    -d "$data" \
    "${RENDER_API}${path}")
  http_code=$(echo "$resp" | tail -n1)
  body=$(echo "$resp" | head -n -1)
  if [[ "$http_code" -lt 200 || "$http_code" -ge 300 ]]; then
    err "POST ${path} failed (HTTP $http_code): $body"
  fi
  echo "$body"
}

render_put() {
  local path="$1" data="$2"
  local resp http_code body
  resp=$(curl -s -w "\n%{http_code}" -X PUT \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    -H "Content-Type: application/json" \
    -H "Accept: application/json" \
    -d "$data" \
    "${RENDER_API}${path}")
  http_code=$(echo "$resp" | tail -n1)
  body=$(echo "$resp" | head -n -1)
  if [[ "$http_code" -lt 200 || "$http_code" -ge 300 ]]; then
    err "PUT ${path} failed (HTTP $http_code): $body"
  fi
  echo "$body"
}

render_delete() {
  local path="$1"
  local resp http_code body
  resp=$(curl -s -w "\n%{http_code}" -X DELETE \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    "${RENDER_API}${path}")
  http_code=$(echo "$resp" | tail -n1)
  body=$(echo "$resp" | head -n -1)
  if [[ "$http_code" -lt 200 || "$http_code" -ge 300 ]]; then
    err "DELETE ${path} failed (HTTP $http_code): $body"
  fi
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

# ── step 2: dump old DB ────────────────────────────────────────────────────────

log "Step 2 — Dumping old database to temp file..."
log "  Dump file: $DUMP_FILE"

pg_dump --format=custom --no-acl --no-owner "$OLD_EXTERNAL_URL" > "$DUMP_FILE"

log "  Dump complete ($(du -sh "$DUMP_FILE" | cut -f1))."

# ── step 3: delete old DB ──────────────────────────────────────────────────────

log "Step 3 — Deleting old database ($OLD_DB_NAME)..."

render_delete "/postgres/${OLD_DB_ID}"

log "  Old database deleted."

# ── step 4: create new DB ──────────────────────────────────────────────────────

NEW_DB_NAME="$DB_NAME_PREFIX"
log "Step 4 — Creating new database: $NEW_DB_NAME..."

CREATE_BODY=$(jq -n \
  --arg name "$NEW_DB_NAME" \
  --arg owner "$RENDER_OWNER_ID" \
  --arg plan "$DB_PLAN" \
  --arg region "$DB_REGION" \
  --arg version "$DB_VERSION" \
  '{name: $name, ownerId: $owner, plan: $plan, region: $region, version: $version}')

NEW_DB_RESP=$(render_post "/postgres" "$CREATE_BODY")
NEW_DB_ID=$(echo "$NEW_DB_RESP" | jq -r '.id // .postgres.id')

[[ -n "$NEW_DB_ID" && "$NEW_DB_ID" != "null" ]] || err "Failed to parse new database ID. Response: $NEW_DB_RESP"

log "  Created: $NEW_DB_NAME (id=$NEW_DB_ID)"

# ── step 5: wait for new DB to be available ─────────────────────────────────────

log "Step 5 — Waiting for new database to become available (timeout: ${PROVISION_TIMEOUT}s)..."

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
        "New DB id=$NEW_DB_ID is still provisioning — check the Render dashboard."
  fi

  log "  Status: $STATUS — waiting ${PROVISION_INTERVAL}s... (${elapsed}s elapsed)"
  sleep "$PROVISION_INTERVAL"
  (( elapsed += PROVISION_INTERVAL ))
done

# ── step 6: restore into new DB ───────────────────────────────────────────────

log "Step 6 — Restoring data into new database..."

pg_restore --no-owner --no-acl --exit-on-error -d "$NEW_EXTERNAL_URL" "$DUMP_FILE"

log "  Data restore complete."

# ── step 7: update API service env var ────────────────────────────────────────

log "Step 7 — Updating API service env var ($ENV_VAR_KEY)..."

# Render PUT /env-vars requires the full list of env vars.
CURRENT_ENV_VARS=$(render_get "/services/${RENDER_SERVICE_ID}/env-vars")

UPDATED_ENV_VARS=$(echo "$CURRENT_ENV_VARS" | jq \
  --arg key "$ENV_VAR_KEY" \
  --arg val "$NEW_INTERNAL_URL" \
  '[.[] | if .envVar.key == $key then .envVar.value = $val else . end | .envVar]')

render_put "/services/${RENDER_SERVICE_ID}/env-vars" "$UPDATED_ENV_VARS" >/dev/null

log "  Env var updated. Service will restart automatically."

# ── step 8: verify API health ─────────────────────────────────────────────────

log "Step 8 — Polling API health (timeout: ${HEALTH_TIMEOUT}s)..."
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
    echo "  New database is intact but the API has not recovered."
    echo "  Check the Render dashboard for the service restart status."
    echo "    New DB id : $NEW_DB_ID"
    echo "    New DB name : $NEW_DB_NAME"
    echo "================================================================="
    exit 1
  fi

  log "  HTTP $HTTP_STATUS — waiting ${HEALTH_INTERVAL}s... (${elapsed}s elapsed)"
  sleep "$HEALTH_INTERVAL"
  (( elapsed += HEALTH_INTERVAL ))
done

# ── summary ───────────────────────────────────────────────────────────────────

echo ""
echo "================================================================="
echo "  DB ROTATION COMPLETE"
echo "================================================================="
echo "  Old DB   : $OLD_DB_NAME (deleted)"
echo "  New DB   : $NEW_DB_NAME (id=$NEW_DB_ID)"
echo "  API      : healthy"
echo "================================================================="
