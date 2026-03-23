# scripts/e2e-local.ps1
#
# Mints a short-lived E2E token from the local Keycloak instance and runs the
# full Playwright suite.  Run this from the repo root:
#
#   .\scripts\e2e-local.ps1
#
# Prerequisites:
#   - Docker running (sqlserver + keycloak containers up)
#   - API running on http://localhost:5018
#   - Frontend dev server running on http://localhost:5173
#
# Pass extra Playwright flags as arguments:
#   .\scripts\e2e-local.ps1 --ui
#   .\scripts\e2e-local.ps1 tests/e2e/budgets.spec.ts

param(
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$PlaywrightArgs
)

$ErrorActionPreference = 'Stop'

$KeycloakUrl  = if ($env:KEYCLOAK_URL)    { $env:KEYCLOAK_URL }    else { 'http://localhost:8080' }
$Realm        = if ($env:KEYCLOAK_REALM)  { $env:KEYCLOAK_REALM }  else { 'fintrackpro' }
$ClientId     = if ($env:E2E_CLIENT_ID)   { $env:E2E_CLIENT_ID }   else { 'fintrackpro-e2e' }
$Username     = if ($env:E2E_USERNAME)    { $env:E2E_USERNAME }    else { 'admin@fintrackpro.dev' }
$Password     = if ($env:E2E_PASSWORD)    { $env:E2E_PASSWORD }    else { 'Admin1234!' }

$FrontendDir  = Join-Path $PSScriptRoot '..\frontend\fintrackpro-ui'

Write-Host "==> Minting E2E token from $KeycloakUrl/realms/$Realm ..."

$body = "grant_type=password&client_id=$ClientId&username=$Username&password=$Password"
try {
    $response = Invoke-RestMethod `
        -Method Post `
        -Uri "$KeycloakUrl/realms/$Realm/protocol/openid-connect/token" `
        -ContentType 'application/x-www-form-urlencoded' `
        -Body $body
} catch {
    Write-Error "Token mint failed: $_"
    Write-Host "Check that:"
    Write-Host "  - Keycloak is running on $KeycloakUrl"
    Write-Host "  - The fintrackpro-e2e client exists (docker compose restart keycloak to re-import realm)"
    exit 1
}

$token = $response.access_token
if (-not $token) {
    Write-Error "Token mint failed: access_token was empty in response"
    exit 1
}

Write-Host "==> Token minted ($($token.Length) chars). Running Playwright tests ..."

$env:E2E_TOKEN = $token

Push-Location $FrontendDir
try {
    npx playwright test @PlaywrightArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
    Pop-Location
}
