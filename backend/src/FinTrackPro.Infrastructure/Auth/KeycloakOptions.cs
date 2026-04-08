namespace FinTrackPro.Infrastructure.Auth;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Validates the <c>iss</c> claim in tokens — always the public-facing Keycloak URL.
    /// e.g. http://localhost:8080/realms/fintrackpro
    /// </summary>
    public string Authority { get; init; } = string.Empty;

    /// <summary>
    /// Where the API fetches signing keys. Differs from Authority in Docker
    /// (container hostname) vs hybrid dev (same as Authority).
    /// Falls back to Authority when not set.
    /// </summary>
    public string MetadataAddress { get; init; } = string.Empty;

    /// <summary>Returns MetadataAddress if set, otherwise Authority.</summary>
    public string ResolvedMetadataAddress => string.IsNullOrWhiteSpace(MetadataAddress) ? Authority : MetadataAddress;
}
