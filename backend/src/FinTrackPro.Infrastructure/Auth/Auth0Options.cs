namespace FinTrackPro.Infrastructure.Auth;

public sealed class Auth0Options
{
    public const string SectionName = "Auth0";

    /// <summary>
    /// Auth0 tenant domain — e.g. your-tenant.auth0.com
    /// </summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>
    /// OIDC authority derived from Domain. Auth0 always uses HTTPS.
    /// </summary>
    public string Authority => $"https://{Domain}/";
}
