using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Tests.Common;

/// <summary>
/// Generates local JWT tokens accepted by the test API (symmetric key, test issuer/audience).
/// The CustomWebApplicationFactory overrides Keycloak JWT validation to trust these tokens.
/// </summary>
public static class AuthTokenFactory
{
    public const string TestSigningKey = "fintrackpro-test-signing-key-32chars!!";
    public const string TestIssuer = "fintrackpro-test";
    public const string TestAudience = "fintrackpro-api-test";

    public static string GenerateToken(string keycloakUserId, params string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));

        var claims = new List<Claim>
        {
            new("sub", keycloakUserId),
            new(ClaimTypes.NameIdentifier, keycloakUserId),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
