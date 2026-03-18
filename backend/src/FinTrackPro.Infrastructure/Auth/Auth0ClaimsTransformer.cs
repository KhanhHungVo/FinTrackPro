using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace FinTrackPro.Infrastructure.Auth;

/// <summary>
/// Maps roles injected by the Auth0 post-login Action into standard ClaimTypes.Role claims.
/// The Action must add roles as a JSON array string at claim "https://fintrackpro.dev/roles".
/// </summary>
public class Auth0ClaimsTransformer : IClaimsTransformation
{
    private const string RolesClaim = "https://fintrackpro.dev/roles";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity!;
        var rolesJson = identity.FindFirst(RolesClaim)?.Value;
        if (rolesJson is null) return Task.FromResult(principal);

        using var doc = JsonDocument.Parse(rolesJson);
        foreach (var role in doc.RootElement.EnumerateArray())
        {
            var roleName = role.GetString();
            if (roleName is not null && !identity.HasClaim(ClaimTypes.Role, roleName))
                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
        }
        return Task.FromResult(principal);
    }
}
