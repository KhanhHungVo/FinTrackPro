# ── Render auth ───────────────────────────────────────────────────────────────
variable "render_api_key" {
  type        = string
  sensitive   = true
  description = "Render API key — Render dashboard → Account → API Keys"
}

variable "render_owner_id" {
  type        = string
  description = "Render owner ID — Render dashboard → Account → ID (usr-...)"
}

# ── Database ───────────────────────────────────────────────────────────────────
variable "db_connection_string" {
  type        = string
  sensitive   = true
  description = "Azure SQL ADO.NET connection string"
}

# ── Auth0 ──────────────────────────────────────────────────────────────────────
variable "auth0_domain" {
  type        = string
  description = "Auth0 tenant domain, e.g. dev-abc123.us.auth0.com"
}

variable "auth0_m2m_client_id" {
  type        = string
  description = "Auth0 M2M application client ID (fintrackpro-m2m)"
}

variable "auth0_m2m_client_secret" {
  type        = string
  sensitive   = true
  description = "Auth0 M2M application client secret"
}

# ── App ────────────────────────────────────────────────────────────────────────
variable "cors_origins" {
  type        = string
  description = "Allowed CORS origins — set to the frontend Render URL after first deploy"
}

variable "coingecko_api_key" {
  type        = string
  sensitive   = true
  description = "CoinGecko Demo or Pro API key"
}

variable "telegram_bot_token" {
  type        = string
  sensitive   = true
  default     = ""
  description = "Telegram bot token — optional, notifications silently skipped if empty"
}

variable "hangfire_dashboard_password" {
  type        = string
  sensitive   = true
  description = "Password for the Hangfire dashboard Basic Auth (username: hangfire-admin)"
}

# ── Frontend build-time vars (VITE_*) ─────────────────────────────────────────
variable "vite_api_base_url" {
  type        = string
  description = "API Render URL, e.g. https://fintrackpro-api.onrender.com"
}

variable "vite_auth0_domain" {
  type        = string
  description = "Auth0 tenant domain — same value as auth0_domain"
}

variable "vite_auth0_client_id" {
  type        = string
  description = "Auth0 SPA application client ID (fintrackpro-spa)"
}
