namespace FinTrackPro.Infrastructure.Auth;

public class IdentityProviderOptions
{
    public const string SectionName = "IdentityProvider";

    /// <summary>
    /// The active identity provider. Accepted values: "keycloak" (default) | "auth0".
    /// </summary>
    public string Provider { get; init; } = "keycloak";

    /// <summary>
    /// JWT audience claim — must match what both Keycloak and Auth0 emit.
    /// URI convention (RFC 8707): https://api.fintrackpro.dev
    /// </summary>
    public string Audience { get; init; } = "https://api.fintrackpro.dev";

    /// <summary>
    /// Client ID for the M2M service account used to call the active IAM provider's admin API.
    /// Keycloak: the fintrackpro-api confidential client. Auth0: the dedicated M2M application.
    /// </summary>
    public string AdminClientId { get; init; } = string.Empty;

    /// <summary>
    /// Client secret for the admin API service account. Set via gitignored override or env var.
    /// </summary>
    public string AdminClientSecret { get; init; } = string.Empty;

    public static class Providers
    {
        public const string Keycloak = "keycloak";
        public const string Auth0    = "auth0";
    }
}
