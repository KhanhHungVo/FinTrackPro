output "api_url" {
  description = "Deployed API URL — use as vite_api_base_url and cors_origins"
  value       = render_web_service.api.url
}

output "frontend_url" {
  description = "Deployed frontend URL — add to Auth0 Allowed Callback/Logout/Web Origins"
  value       = render_static_site.frontend.url
}
