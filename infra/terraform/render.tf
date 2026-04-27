# ── Project ────────────────────────────────────────────────────────────────────
resource "render_project" "fintrackpro" {
  name = "fintrackpro"
  environments = {
    production = {
      name             = "Production"
      protected_status = "protected"
    }
  }
}

# ── PostgreSQL Database ────────────────────────────────────────────────────────
# Provisioned once. Never modified or destroyed by subsequent terraform applies.
resource "render_postgres" "db" {
  name           = "fintrackpro-db"
  plan           = "free"
  region         = "singapore"
  version        = "18"
  environment_id = render_project.fintrackpro.environments["production"].id

  lifecycle {
    ignore_changes  = all  # created once, never updated
    prevent_destroy = true # blocks accidental terraform destroy
  }
}

# ── Backend API — Docker Web Service ──────────────────────────────────────────
resource "render_web_service" "api" {
  name           = "fintrackpro-api"
  plan           = "free"
  region         = "singapore"
  environment_id = render_project.fintrackpro.environments["production"].id

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

    DatabaseProvider__Provider           = { value = "postgresql" }
    ConnectionStrings__DefaultConnection = { value = var.db_connection_string }

    IdentityProvider__Provider = { value = "auth0" }
    IdentityProvider__Audience = { value = "https://api.fintrackpro.dev" }

    IdentityProvider__AdminClientId     = { value = var.auth0_m2m_client_id }
    IdentityProvider__AdminClientSecret = { value = var.auth0_m2m_client_secret }
    Auth0__Domain                       = { value = var.auth0_domain }
    Cors__Origins                       = { value = var.cors_origins }
    CoinGecko__ApiKey                   = { value = var.coingecko_api_key }
    ExchangeRate__ApiKey                = { value = var.exchangerate_api_key }
    Telegram__BotToken                  = { value = var.telegram_bot_token }
    Hangfire__Password                  = { value = var.hangfire_dashboard_password }

    PaymentGateway__Provider   = { value = "stripe" }
    PaymentGateway__PriceId    = { value = var.stripe_price_id }
    Stripe__SecretKey          = { value = var.stripe_secret_key }
    Stripe__WebhookSecret      = { value = var.stripe_webhook_secret }
  }
}

# ── Frontend — Static Site ─────────────────────────────────────────────────────
resource "render_static_site" "frontend" {
  name           = "fintrackpro-ui"
  repo_url       = "https://github.com/KhanhHungVo/FinTrackPro"
  branch         = "main"
  build_command  = "npm install && npm run build"
  publish_path   = "dist"
  root_directory = "frontend/fintrackpro-ui"
  auto_deploy    = true
  environment_id = render_project.fintrackpro.environments["production"].id

  env_vars = {
    VITE_AUTH_PROVIDER   = { value = "auth0" }
    VITE_AUTH0_AUDIENCE  = { value = "https://api.fintrackpro.dev" }
    VITE_API_BASE_URL    = { value = var.vite_api_base_url }
    VITE_AUTH0_DOMAIN    = { value = var.vite_auth0_domain }
    VITE_AUTH0_CLIENT_ID = { value = var.vite_auth0_client_id }

    VITE_ADMIN_TELEGRAM       = { value = var.vite_admin_telegram }
    VITE_ADMIN_EMAIL          = { value = var.vite_admin_email }
    VITE_BANK_NAME            = { value = var.vite_bank_name }
    VITE_BANK_ACCOUNT_NUMBER  = { value = var.vite_bank_account_number }
    VITE_BANK_ACCOUNT_NAME    = { value = var.vite_bank_account_name }
    VITE_BANK_TRANSFER_AMOUNT = { value = var.vite_bank_transfer_amount }
    VITE_BANK_QR_URL          = { value = var.vite_bank_qr_url }
  }

  routes = [
    {
      type        = "rewrite"
      source      = "/*"
      destination = "/index.html"
    }
  ]
}
