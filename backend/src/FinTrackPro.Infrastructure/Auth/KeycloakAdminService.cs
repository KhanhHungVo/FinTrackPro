using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinTrackPro.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.Infrastructure.Auth;

/// <summary>
/// Calls the Keycloak Admin REST API using a client-credentials token
/// obtained from the fintrackpro-api service account.
/// Configuration required:
///   Keycloak:Authority               — e.g. http://localhost:8080/realms/fintrackpro
///   IdentityProvider:AdminClientId   — e.g. fintrackpro-api
///   IdentityProvider:AdminClientSecret — client secret from Keycloak Credentials tab
/// </summary>
public class KeycloakAdminService : IIamProviderService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakAdminService> _logger;

    public KeycloakAdminService(
        HttpClient http,
        IConfiguration configuration,
        ILogger<KeycloakAdminService> logger)
    {
        _http = http;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<IamUserInfo>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        if (token is null) return [];

        var authority = _configuration["Keycloak:Authority"]!; // e.g. http://localhost:8080/realms/fintrackpro
        var realmBase = authority[..authority.LastIndexOf("/realms/", StringComparison.Ordinal)]; // http://localhost:8080
        var realm = authority[(authority.LastIndexOf('/') + 1)..]; // fintrackpro

        var result = new List<IamUserInfo>();
        const int pageSize = 100;
        var first = 0;

        while (true)
        {
            var url = $"{realmBase}/admin/realms/{realm}/users?first={first}&max={pageSize}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new("Bearer", token);

            var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Keycloak Admin API returned {StatusCode} for users list", response.StatusCode);
                break;
            }

            var page = await response.Content.ReadFromJsonAsync<List<KeycloakUserRepresentation>>(cancellationToken: cancellationToken);
            if (page is null || page.Count == 0) break;

            result.AddRange(page.Select(u => new IamUserInfo(u.Id, u.Enabled)));

            if (page.Count < pageSize) break;
            first += pageSize;
        }

        return result;
    }

    private async Task<string?> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        var authority = _configuration["Keycloak:Authority"]!;
        var clientId = _configuration["IdentityProvider:AdminClientId"];
        var clientSecret = _configuration["IdentityProvider:AdminClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogWarning("IdentityProvider:AdminClientId or IdentityProvider:AdminClientSecret not configured — skipping user sync");
            return null;
        }

        var tokenUrl = $"{authority}/protocol/openid-connect/token";
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };

        var response = await _http.PostAsync(tokenUrl, new FormUrlEncodedContent(form), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to obtain Keycloak admin token: {StatusCode}", response.StatusCode);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        return result?.AccessToken;
    }

    private sealed record KeycloakUserRepresentation(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("enabled")] bool Enabled);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);
}
