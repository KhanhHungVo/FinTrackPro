using Hangfire.Dashboard;
using System.Text;

namespace FinTrackPro.API.Infrastructure;

public class HangfireBasicAuthFilter(IConfiguration configuration) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var expectedUsername = configuration["Hangfire:Username"];
        var expectedPassword = configuration["Hangfire:Password"];

        if (string.IsNullOrEmpty(expectedUsername) || string.IsNullOrEmpty(expectedPassword))
            return false;

        var authHeader = httpContext.Request.Headers.Authorization.ToString();

        if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var encoded = authHeader["Basic ".Length..].Trim();
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var separatorIndex = decoded.IndexOf(':');

                if (separatorIndex > 0)
                {
                    var username = decoded[..separatorIndex];
                    var password = decoded[(separatorIndex + 1)..];

                    if (username == expectedUsername && password == expectedPassword)
                        return true;
                }
            }
            catch (FormatException)
            {
                // malformed Base64 — fall through to challenge
            }
        }

        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire Dashboard\"";
        return false;
    }
}
