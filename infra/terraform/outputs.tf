output "api_url" {
  description = "Deployed API URL — use as vite_api_base_url and cors_origins"
  value       = render_web_service.api.url
}

output "frontend_url" {
  description = "Deployed frontend URL — add to Auth0 Allowed Callback/Logout/Web Origins"
  value       = render_static_site.frontend.url
}

output "db_internal_url" {
  description = "PostgreSQL internal URL — accessible only within Render's network"
  value       = render_postgres.db.connection_info.internal_connection_string
  sensitive   = true
}

output "db_external_url" {
  description = "PostgreSQL external URL — use for local migrations via dotnet user-secrets"
  value       = render_postgres.db.connection_info.external_connection_string
  sensitive   = true
}
