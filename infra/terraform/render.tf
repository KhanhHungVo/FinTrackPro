# ── Backend API — Docker Web Service ──────────────────────────────────────────
resource "render_web_service" "api" {
  name   = "fintrackpro-api"
  plan   = "starter"
  region = "oregon"

  runtime_source = {
    docker = {
      repo_url        = "https://github.com/KhanhHungVo/FinTrackPro"
      branch          = "main"
      dockerfile_path = "backend/Dockerfile"
      context         = "backend"
      auto_deploy     = true
    }
  }

  health_check_path = "/health"

  env_vars = {
    ASPNETCORE_ENVIRONMENT = { value = "Production" }
    ASPNETCORE_URLS        = { value = "http://+:8080" }

    IdentityProvider__Provider = { value = "auth0" }
    IdentityProvider__Audience = { value = "https://api.fintrackpro.dev" }

    ConnectionStrings__DefaultConnection = { value = var.db_connection_string }
    IdentityProvider__AdminClientId      = { value = var.auth0_m2m_client_id }
    IdentityProvider__AdminClientSecret  = { value = var.auth0_m2m_client_secret }
    Auth0__Domain                        = { value = var.auth0_domain }
    Cors__Origins                        = { value = var.cors_origins }
    CoinGecko__ApiKey                    = { value = var.coingecko_api_key }
    Telegram__BotToken                   = { value = var.telegram_bot_token }
  }
}

# ── Frontend — Static Site ─────────────────────────────────────────────────────
resource "render_static_site" "frontend" {
  name          = "fintrackpro-ui"
  repo_url      = "https://github.com/KhanhHungVo/FinTrackPro"  # update to your repo URL
  branch        = "main"
  build_command = "npm install && npm run build"
  publish_path  = "dist"
  root_directory = "frontend/fintrackpro-ui"
  auto_deploy   = true

  env_vars = {
    VITE_AUTH_PROVIDER  = { value = "auth0" }
    VITE_AUTH0_AUDIENCE = { value = "https://api.fintrackpro.dev" }
    VITE_API_BASE_URL   = { value = var.vite_api_base_url }
    VITE_AUTH0_DOMAIN   = { value = var.vite_auth0_domain }
    VITE_AUTH0_CLIENT_ID = { value = var.vite_auth0_client_id }
  }

  routes = [
    {
      type        = "rewrite"
      source      = "/*"
      destination = "/index.html"
    }
  ]
}
