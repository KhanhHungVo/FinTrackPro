using System.Security.Claims;

namespace FinTrackPro.Infrastructure.Identity;

public static class ClaimsExtensions
{
    public static string GetExternalId(this ClaimsPrincipal principal)
        => principal.FindFirstValue("sub")
           ?? throw new InvalidOperationException("JWT is missing 'sub' claim.");

    public static string GetProvider(this ClaimsPrincipal principal)
        => principal.FindFirstValue("iss")
           ?? throw new InvalidOperationException("JWT is missing 'iss' claim.");

    public static string? GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email)
        ?? principal.FindFirstValue("email");

    public static bool IsEmailVerified(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("email_verified");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
