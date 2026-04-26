#!/usr/bin/env bash
# rotate-render-db.sh
#
# Rotates the Render free-tier PostgreSQL database:
#   1. Discovers the current DB (name prefix: fintrackpro-db)
#   2. Reads credentials directly from the Render API
#   3. pg_dump using external connection string → temp file
#   4. Validates the dump (pg_restore --list) — aborts if invalid
#   5. Deletes old DB via Render API
#   6. Creates a new free DB (fintrackpro-db) in the same project
#   7. Waits for it to become available
#   8. pg_restore from validated dump
#   9. Updates ConnectionStrings__DefaultConnection on the API service + triggers clear-cache deploy
#  10. Polls the API health endpoint until healthy
#
# NOTE: There is a brief window between step 5 and step 9 where the API has
#       no valid database connection. This is unavoidable on Render's free
#       tier, which allows only one active PostgreSQL at a time.
#       The dump is fully validated before the old DB is deleted, so data
#       is safe as long as the restore in step 8 succeeds.
#
# Required env vars:
#   RENDER_API_KEY          — Render personal API key
#   RENDER_OWNER_ID         — Render owner/team ID (tea-...)
#   RENDER_SERVICE_ID       — Render web service ID for fintrackpro-api (srv-...)
#   RENDER_PROJECT_ID       — Render project ID (prj-...)
#   RENDER_ENVIRONMENT_ID   — Render environment ID within the project (evm-...)
#   API_HEALTH_URL          — Health endpoint to poll (e.g. https://fintrackpro-api.onrender.com/health)
#
# Usage: bash scripts/rotate-render-db.sh

set -euo pipefail

RENDER_API="https://api.render.com/v1"
DB_NAME_PREFIX="fintrackpro-db"
DB_REGION="singapore"
DB_VERSION="18"
DB_PLAN="free"
PROVISION_TIMEOUT=300   # seconds to wait for new DB to become available
PROVISION_INTERVAL=10   # polling interval (seconds)
HEALTH_TIMEOUT=300      # seconds to wait for API to recover after env var update
HEALTH_INTERVAL=20      # polling interval (seconds)
ENV_VAR_KEY="ConnectionStrings__DefaultConnection"
DUMP_FILE="$(mktemp /tmp/fintrackpro-db-dump.XXXXXX)"

SCRIPT_START=$(date -u '+%H:%M:%S')

# ── helpers ────────────────────────────────────────────────────────────────────

log()    { echo "[$(date -u '+%H:%M:%S')] $*" >&2; }
debug()  { echo "[$(date -u '+%H:%M:%S')] [DEBUG] $*" >&2; }
warn()   { echo "[$(date -u '+%H:%M:%S')] [WARN]  $*" >&2; }
err()    { echo "[$(date -u '+%H:%M:%S')] [ERROR] $*" >&2; exit 1; }

check_deps() {
  log "Checking required tools: curl jq pg_dump pg_restore..."
  local missing=()
  for cmd in curl jq pg_dump pg_restore; do
    command -v "$cmd" &>/dev/null || missing+=("$cmd")
  done
  [[ ${#missing[@]} -eq 0 ]] || err "Missing required tools: ${missing[*]}"
  log "  All tools present."
}

_RENDER_BODY_FILE="$(mktemp /tmp/render_resp.XXXXXX)"

cleanup() {
  rm -f "$_RENDER_BODY_FILE"
  if [[ -f "${DUMP_FILE:-}" ]]; then
    rm -f "$DUMP_FILE"
    log "Temp dump file removed."
  fi
}
trap cleanup EXIT

render_get() {
  local path="$1"
  debug "GET ${path}"
  local http_code
  http_code=$(curl -s -o "$_RENDER_BODY_FILE" -w "%{http_code}" \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    -H "Accept: application/json" \
    "${RENDER_API}${path}")
  debug "  → HTTP $http_code"
  if [[ "$http_code" -lt 200 || "$http_code" -ge 300 ]]; then
    err "GET ${path} failed (HTTP $http_code): $(cat "$_RENDER_BODY_FILE")"
  fi
  cat "$_RENDER_BODY_FILE"
}

render_post() {
  local path="$1" data="$2"
  debug "POST ${path}"
  local http_code
  http_code=$(curl -s -o "$_RENDER_BODY_FILE" -w "%{http_code}" -X POST \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    -H "Content-Type: application/json" \
    -H "Accept: application/json" \
    -d "$data" \
    "${RENDER_API}${path}")
  debug "  → HTTP $http_code"
  if [[ "$http_code" -lt 200 || "$http_code" -ge 300 ]]; then
    err "POST ${path} failed (HTTP $http_code): $(cat "$_RENDER_BODY_FILE")"
  fi
  cat "$_RENDER_BODY_FILE"
}

render_put() {
  local path="$1" data="$2"
  debug "PUT ${path}"
  local http_code
  http_code=$(curl -s -o "$_RENDER_BODY_FILE" -w "%{http_code}" -X PUT \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    -H "Content-Type: application/json" \
    -H "Accept: application/json" \
    -d "$data" \
    "${RENDER_API}${path}")
  debug "  → HTTP $http_code"
  if [[ "$http_code" -lt 200 || "$http_code" -ge 300 ]]; then
    err "PUT ${path} failed (HTTP $http_code): $(cat "$_RENDER_BODY_FILE")"
  fi
  cat "$_RENDER_BODY_FILE"
}

render_delete() {
  local path="$1"
  debug "DELETE ${path}"
  local http_code
  http_code=$(curl -s -o "$_RENDER_BODY_FILE" -w "%{http_code}" -X DELETE \
    -H "Authorization: Bearer $RENDER_API_KEY" \
    "${RENDER_API}${path}")
  debug "  → HTTP $http_code"
  if [[ "$http_code" -lt 200 || "$http_code" -ge 300 ]]; then
    err "DELETE ${path} failed (HTTP $http_code): $(cat "$_RENDER_BODY_FILE")"
  fi
}

# ── validate inputs ─────────────────────────────────────────────────────────────

echo ""
echo "================================================================="
echo "  FINTRACKPRO DB ROTATION — started at $SCRIPT_START UTC"
echo "================================================================="
echo ""

check_deps

log "Validating required environment variables..."
: "${RENDER_API_KEY:?RENDER_API_KEY is required}"
: "${RENDER_OWNER_ID:?RENDER_OWNER_ID is required}"
: "${RENDER_SERVICE_ID:?RENDER_SERVICE_ID is required}"
: "${RENDER_PROJECT_ID:?RENDER_PROJECT_ID is required}"
: "${RENDER_ENVIRONMENT_ID:?RENDER_ENVIRONMENT_ID is required}"
: "${API_HEALTH_URL:?API_HEALTH_URL is required}"
log "  RENDER_API_KEY        = set"
log "  RENDER_OWNER_ID       = set"
log "  RENDER_SERVICE_ID     = set"
log "  RENDER_PROJECT_ID     = $RENDER_PROJECT_ID"
log "  RENDER_ENVIRONMENT_ID = $RENDER_ENVIRONMENT_ID"
log "  API_HEALTH_URL        = $API_HEALTH_URL"
log "  DUMP_FILE             = $DUMP_FILE"
log "  All env vars present."

# ── step 1: discover old DB ────────────────────────────────────────────────────

echo ""
log "Step 1 — Discovering current database..."

DB_LIST=$(render_get "/postgres?ownerId=${RENDER_OWNER_ID}&limit=20")
DB_COUNT=$(echo "$DB_LIST" | jq 'length')
debug "  Total postgres instances returned: $DB_COUNT"

OLD_DB=$(echo "$DB_LIST" | jq -r \
  --arg prefix "$DB_NAME_PREFIX" \
  '[.[] | select(.postgres.name | startswith($prefix))] | sort_by(.postgres.createdAt) | last | .postgres')

[[ "$OLD_DB" != "null" && -n "$OLD_DB" ]] \
  || err "No database with name prefix '$DB_NAME_PREFIX' found. Instances found: $DB_COUNT"

OLD_DB_ID=$(echo "$OLD_DB" | jq -r '.id')
OLD_DB_NAME=$(echo "$OLD_DB" | jq -r '.name')
OLD_DB_STATUS=$(echo "$OLD_DB" | jq -r '.status')

log "  Found: $OLD_DB_NAME  id: $OLD_DB_ID  status: $OLD_DB_STATUS"

# ── step 1b: fetch full DB details (IP rules + region + credentials) ──────────

echo ""
log "Step 1b — Fetching full details for old database..."

OLD_DB_INFO=$(render_get "/postgres/${OLD_DB_ID}")
OLD_DB_REGION=$(echo "$OLD_DB_INFO" | jq -r '.region')
debug "  region: $OLD_DB_REGION"

OLD_IP_RULES=$(echo "$OLD_DB_INFO" | jq '.ipAllowList // []')
RULE_COUNT=$(echo "$OLD_IP_RULES" | jq 'length')
if [[ "$RULE_COUNT" -gt 0 ]]; then
  log "  Fetched $RULE_COUNT IP rule(s)."
else
  OLD_IP_RULES='[{"cidrBlock":"0.0.0.0/0","description":"Allow all"}]'
  warn "  No IP rules on old DB — using fallback: 0.0.0.0/0 Allow all."
fi

# ── step 1c: fetch connection credentials via dedicated endpoint ──────────────

echo ""
log "Step 1c — Fetching connection credentials..."

OLD_CONN_INFO=$(render_get "/postgres/${OLD_DB_ID}/connection-info")
debug "  connection-info keys: $(echo "$OLD_CONN_INFO" | jq -c 'keys? // "none"')"

# ── step 2: build external connection string for pg_dump ──────────────────────

echo ""
log "Step 2 — Extracting credentials and building external connection string..."
log "  DB: $OLD_DB_NAME  region: $OLD_DB_REGION"

# Use externalConnectionString directly — it contains the correct hostname from Render
OLD_EXTERNAL_URL=$(echo "$OLD_CONN_INFO" | jq -r '.externalConnectionString // empty')

[[ -n "$OLD_EXTERNAL_URL" ]] \
  || err "Could not extract externalConnectionString. connection-info keys: $(echo "$OLD_CONN_INFO" | jq -c 'keys? // "none"')"

debug "  externalConnectionString: obtained"
log "  External connection string built."

# ── step 3: dump old DB ────────────────────────────────────────────────────────

echo ""
log "Step 3 — Dumping old database to temp file..."
log "  Source: $OLD_DB_NAME  region: $OLD_DB_REGION"
log "  Target: $DUMP_FILE"

DUMP_START=$(date +%s)
pg_dump --format=custom --no-acl --no-owner "$OLD_EXTERNAL_URL" > "$DUMP_FILE"
DUMP_END=$(date +%s)

DUMP_SIZE=$(du -sh "$DUMP_FILE" | cut -f1)
DUMP_BYTES=$(wc -c < "$DUMP_FILE")
log "  Dump complete in $(( DUMP_END - DUMP_START ))s — size: $DUMP_SIZE ($DUMP_BYTES bytes)."

# ── step 4: validate the dump ─────────────────────────────────────────────────

echo ""
log "Step 4 — Validating dump file..."

[[ -s "$DUMP_FILE" ]] \
  || err "Dump file is empty (0 bytes) — aborting before deleting old DB. No data was lost."

OBJECT_LIST=$(pg_restore --list "$DUMP_FILE" 2>/dev/null || true)
OBJECT_COUNT=$(echo "$OBJECT_LIST" | grep -c '^[0-9]' || true)
debug "  First 10 objects in dump:"
echo "$OBJECT_LIST" | grep '^[0-9]' | head -10 | while read -r line; do debug "    $line"; done

[[ "$OBJECT_COUNT" -gt 0 ]] \
  || err "Dump file failed validation (no restorable objects) — aborting before deleting old DB. No data was lost."

log "  Dump valid — $OBJECT_COUNT restorable objects found."

# ── step 5: delete old DB ──────────────────────────────────────────────────────

echo ""
log "Step 5 — Deleting old database ($OLD_DB_NAME / $OLD_DB_ID)..."

render_delete "/postgres/${OLD_DB_ID}"

log "  Old database deleted successfully."

# ── step 6: create new DB ──────────────────────────────────────────────────────

echo ""
NEW_DB_NAME="$DB_NAME_PREFIX"
log "Step 6 — Creating new database: $NEW_DB_NAME..."
log "  plan: $DB_PLAN  region: $DB_REGION  version: $DB_VERSION  project: $RENDER_PROJECT_ID"

CREATE_BODY=$(jq -n \
  --arg name "$NEW_DB_NAME" \
  --arg owner "$RENDER_OWNER_ID" \
  --arg plan "$DB_PLAN" \
  --arg region "$DB_REGION" \
  --arg version "$DB_VERSION" \
  --arg project "$RENDER_PROJECT_ID" \
  --arg env "$RENDER_ENVIRONMENT_ID" \
  '{name: $name, ownerId: $owner, plan: $plan, region: $region, version: $version, projectId: $project, environmentId: $env}')

NEW_DB_RESP=$(render_post "/postgres" "$CREATE_BODY")
NEW_DB_ID=$(echo "$NEW_DB_RESP" | jq -r '.id // .postgres.id')
debug "  create response keys: $(echo "$NEW_DB_RESP" | jq -c 'keys? // "none"')"
debug "  environmentId in response: $(echo "$NEW_DB_RESP" | jq -r '.environmentId // "absent"')"

[[ -n "$NEW_DB_ID" && "$NEW_DB_ID" != "null" ]] \
  || err "Failed to parse new database ID from create response."

log "  Created: $NEW_DB_NAME  id: $NEW_DB_ID"

# ── step 7: wait for new DB to be available ────────────────────────────────────

echo ""
log "Step 7 — Waiting for new database to become available (timeout: ${PROVISION_TIMEOUT}s)..."

elapsed=0
while true; do
  DB_INFO=$(render_get "/postgres/${NEW_DB_ID}")
  STATUS=$(echo "$DB_INFO" | jq -r '.status // .postgres.status')

  if [[ "$STATUS" == "available" ]]; then
    # Fetch credentials via dedicated endpoint (GET /postgres/{id} does not expose password)
    NEW_CONN_INFO=$(render_get "/postgres/${NEW_DB_ID}/connection-info")
    debug "  connection-info keys: $(echo "$NEW_CONN_INFO" | jq -c 'keys? // "none"')"

    # Use connection strings directly from the API — they contain correct hostnames
    NEW_EXTERNAL_URL=$(echo "$NEW_CONN_INFO" | jq -r '.externalConnectionString // empty')
    INT_URL=$(echo "$NEW_CONN_INFO"          | jq -r '.internalConnectionString // empty')

    [[ -n "$NEW_EXTERNAL_URL" ]] \
      || err "Could not extract externalConnectionString for new DB. connection-info keys: $(echo "$NEW_CONN_INFO" | jq -c 'keys? // "none"')"
    [[ -n "$INT_URL" ]] \
      || err "Could not extract internalConnectionString for new DB. connection-info keys: $(echo "$NEW_CONN_INFO" | jq -c 'keys? // "none"')"

    # Render always returns internalConnectionString as a postgresql:// URI.
    # Convert it to Npgsql key=value format required by .NET / Npgsql.
    # Example URI: postgresql://user:pass@dpg-xxx.oregon-postgres.render.com/dbname
    DB_USER=$(echo "$INT_URL"     | sed 's|postgresql://\([^:]*\):.*|\1|')
    DB_PASS=$(echo "$INT_URL"     | sed 's|postgresql://[^:]*:\([^@]*\)@.*|\1|')
    DB_HOST=$(echo "$INT_URL"     | sed 's|postgresql://[^@]*@\([^/:]*\).*|\1|')
    DB_DATABASE=$(echo "$INT_URL" | sed 's|.*/||')
    NEW_INTERNAL_CONN_STR="Host=${DB_HOST};Port=5432;Database=${DB_DATABASE};Username=${DB_USER};Password=${DB_PASS};SSL Mode=Require;Trust Server Certificate=true"

    debug "  externalConnectionString: obtained"
    debug "  internalConnStr (Npgsql): Host=${DB_HOST};Port=5432;Database=${DB_DATABASE};Username=${DB_USER};Password=***"

    log "  New DB is available. name: $NEW_DB_NAME  region: $DB_REGION  id: $NEW_DB_ID  (${elapsed}s elapsed)"
    break
  fi

  if (( elapsed >= PROVISION_TIMEOUT )); then
    err "New DB did not become available within ${PROVISION_TIMEOUT}s (last status: $STATUS). Check Render dashboard for id=$NEW_DB_ID."
  fi

  log "  Status: $STATUS — waiting ${PROVISION_INTERVAL}s... (${elapsed}s elapsed)"
  sleep "$PROVISION_INTERVAL"
  (( elapsed += PROVISION_INTERVAL ))
done

# ── step 7b: apply IP allow-list to new DB ────────────────────────────────────

echo ""
log "Step 7b — Applying IP allow-list ($RULE_COUNT rule(s)) to new database..."

IP_PATCH_BODY=$(jq -n --argjson rules "$OLD_IP_RULES" '{ipAllowList: $rules}')
IP_PATCH_CODE=$(curl -s -o "$_RENDER_BODY_FILE" -w "%{http_code}" -X PATCH \
  -H "Authorization: Bearer $RENDER_API_KEY" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d "$IP_PATCH_BODY" \
  "${RENDER_API}/postgres/${NEW_DB_ID}")
debug "  PATCH /postgres/${NEW_DB_ID} → HTTP $IP_PATCH_CODE"

if [[ "$IP_PATCH_CODE" -ge 200 && "$IP_PATCH_CODE" -lt 300 ]]; then
  log "  IP allow-list applied successfully."
else
  warn "  Could not apply IP rules (HTTP $IP_PATCH_CODE). Apply manually in the Render dashboard: Networking → PostgreSQL Inbound Flows."
fi

# ── step 8: restore into new DB ───────────────────────────────────────────────

echo ""
log "Step 8 — Restoring data into new database..."
log "  Source: $DUMP_FILE ($DUMP_SIZE)"
log "  Target: $NEW_DB_NAME  region: $DB_REGION  id: $NEW_DB_ID"

# Extract host from external URL for TCP readiness check
EXT_HOST=$(echo "$NEW_EXTERNAL_URL" | sed 's|postgresql://[^@]*@\([^/:]*\).*|\1|')
debug "  pg_restore target host: $EXT_HOST"

# Wait for PostgreSQL to be ready to accept connections.
# Render marks status 'available' and opens the TCP port before the SSL
# handshake is functional — pg_isready speaks the PG protocol and confirms
# the server is truly accepting connections.
log "  Waiting for PostgreSQL to accept connections..."
PG_READY_TIMEOUT=120
PG_READY_ELAPSED=0
until pg_isready -h "$EXT_HOST" -p 5432 -U postgres -t 5 &>/dev/null; do
  if (( PG_READY_ELAPSED >= PG_READY_TIMEOUT )); then
    err "PostgreSQL at $EXT_HOST:5432 not ready after ${PG_READY_TIMEOUT}s."
  fi
  log "  Not ready yet — retrying in 5s... (${PG_READY_ELAPSED}s elapsed)"
  sleep 5
  (( PG_READY_ELAPSED += 5 ))
done
log "  PostgreSQL ready after ${PG_READY_ELAPSED}s."

RESTORE_START=$(date +%s)
pg_restore --no-owner --no-acl --exit-on-error -d "$NEW_EXTERNAL_URL" "$DUMP_FILE"
RESTORE_END=$(date +%s)

log "  Data restore complete in $(( RESTORE_END - RESTORE_START ))s."

# ── step 9: update API service env var ────────────────────────────────────────

echo ""
log "Step 9 — Updating API service env var ($ENV_VAR_KEY)..."

CURRENT_ENV_VARS=$(render_get "/services/${RENDER_SERVICE_ID}/env-vars")
CURRENT_VAR_COUNT=$(echo "$CURRENT_ENV_VARS" | jq 'length')
debug "  Env var count on service: $CURRENT_VAR_COUNT"

KEY_EXISTS=$(echo "$CURRENT_ENV_VARS" | jq --arg key "$ENV_VAR_KEY" '[.[] | select(.envVar.key == $key)] | length')
[[ "$KEY_EXISTS" -gt 0 ]] \
  || err "$ENV_VAR_KEY not found in service env vars — cannot update. Check RENDER_SERVICE_ID."
debug "  Key '$ENV_VAR_KEY' found — updating."

UPDATED_ENV_VARS=$(echo "$CURRENT_ENV_VARS" | jq \
  --arg key "$ENV_VAR_KEY" \
  --arg val "$NEW_INTERNAL_CONN_STR" \
  '[.[] | if .envVar.key == $key then .envVar.value = $val else . end | .envVar]')

render_put "/services/${RENDER_SERVICE_ID}/env-vars" "$UPDATED_ENV_VARS" >/dev/null

log "  Env var updated."

# Updating env vars alone only restarts with cached build; trigger a
# clear-cache deploy so the new connection string is picked up cleanly.
log "  Triggering clear-cache deploy..."
render_post "/services/${RENDER_SERVICE_ID}/deploys" '{"clearCache":"clear"}' >/dev/null
log "  Clear-cache deploy triggered. Waiting for service to come back up..."

# ── step 10: verify API health ────────────────────────────────────────────────

echo ""
log "Step 10 — Polling API health (timeout: ${HEALTH_TIMEOUT}s)..."
log "  Endpoint: $API_HEALTH_URL"

elapsed=0
while true; do
  HTTP_STATUS=$(curl -so /dev/null -w "%{http_code}" --max-time 10 "$API_HEALTH_URL" || true)
  debug "  Health poll at ${elapsed}s → HTTP $HTTP_STATUS"

  if [[ "$HTTP_STATUS" == "200" ]]; then
    log "  API is healthy (HTTP 200) after ${elapsed}s."
    break
  fi

  if (( elapsed >= HEALTH_TIMEOUT )); then
    echo ""
    echo "================================================================="
    echo "  HEALTH CHECK FAILED after ${HEALTH_TIMEOUT}s (last HTTP: $HTTP_STATUS)"
    echo "  New database is intact but the API has not recovered."
    echo "  Check the Render dashboard for the service restart status."
    echo "    New DB name : $NEW_DB_NAME"
    echo "    New DB id   : $NEW_DB_ID"
    echo "    Region      : $DB_REGION"
    echo "================================================================="
    exit 1
  fi

  log "  HTTP $HTTP_STATUS — waiting ${HEALTH_INTERVAL}s... (${elapsed}s elapsed)"
  sleep "$HEALTH_INTERVAL"
  (( elapsed += HEALTH_INTERVAL ))
done

# ── summary ───────────────────────────────────────────────────────────────────

SCRIPT_END=$(date -u '+%H:%M:%S')
echo ""
echo "================================================================="
echo "  DB ROTATION COMPLETE"
echo "================================================================="
echo "  Started  : $SCRIPT_START UTC"
echo "  Finished : $SCRIPT_END UTC"
echo "  Old DB   : $OLD_DB_NAME / $OLD_DB_ID (deleted)"
echo "  New DB   : $NEW_DB_NAME / $NEW_DB_ID  region: $DB_REGION"
echo "  Project  : $RENDER_PROJECT_ID"
echo "  API      : healthy"
echo "================================================================="
