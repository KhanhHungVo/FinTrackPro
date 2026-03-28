#!/usr/bin/env bash
# scripts/api-e2e-local.sh
#
# Runs the Newman API E2E suite against a locally running stack.
# Mirrors the pattern of scripts/e2e-local.sh (Playwright).
#
# Usage:
#   bash scripts/api-e2e-local.sh
#   bash scripts/api-e2e-local.sh --folder "Trades*"
#   bash scripts/api-e2e-local.sh --verbose
#
# Override defaults:
#   KEYCLOAK_URL=http://localhost:8080 \
#   API_BASE_URL=http://localhost:5018 \
#   E2E_USERNAME=admin@fintrackpro.dev \
#   E2E_PASSWORD=Admin1234! \
#   E2E_USERNAME2=user2@fintrackpro.dev \
#   E2E_PASSWORD2=User2Pass! \
#   bash scripts/api-e2e-local.sh
#
# Prerequisites:
#   - Docker running (postgres + keycloak containers up)
#   - API running on http://localhost:5018
#   - Newman installed globally:
#       npm install -g newman
#
# The fintrackpro-e2e Keycloak client and both test users are provisioned
# automatically from infra/docker/keycloak-realm.json on first `docker compose up`.
# If they are missing, restart Keycloak: docker compose restart keycloak

set -euo pipefail

KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8080}"
API_BASE_URL="${API_BASE_URL:-http://localhost:5018}"
E2E_USERNAME="${E2E_USERNAME:-admin@fintrackpro.dev}"
E2E_PASSWORD="${E2E_PASSWORD:-Admin1234!}"
E2E_USERNAME2="${E2E_USERNAME2:-user2@fintrackpro.dev}"
E2E_PASSWORD2="${E2E_PASSWORD2:-User2Pass!}"

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
COLLECTION="$REPO_ROOT/docs/postman/FinTrackPro.postman_collection.json"
ENVIRONMENT="$REPO_ROOT/docs/postman/FinTrackPro.postman_environment.json"
RESULTS_DIR="$REPO_ROOT/test-results"

mkdir -p "$RESULTS_DIR"

echo "==> Running Newman API E2E tests"
echo "    API:      $API_BASE_URL"
echo "    Keycloak: $KEYCLOAK_URL"
echo "    User1:    $E2E_USERNAME"
echo "    User2:    $E2E_USERNAME2"
echo ""

newman run "$COLLECTION" \
  -e "$ENVIRONMENT" \
  --env-var "baseUrl=$API_BASE_URL" \
  --env-var "keycloakUrl=$KEYCLOAK_URL" \
  --env-var "testUsername=$E2E_USERNAME" \
  --env-var "testPassword=$E2E_PASSWORD" \
  --env-var "testUsername2=$E2E_USERNAME2" \
  --env-var "testPassword2=$E2E_PASSWORD2" \
  --bail \
  -r cli,junit \
  --reporter-junit-export "$RESULTS_DIR/newman.xml" \
  "$@"
