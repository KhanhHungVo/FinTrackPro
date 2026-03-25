using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FinTrackPro.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.Infrastructure.Auth;

/// <summary>
/// Calls the Auth0 Management API v2 using a client-credentials token
/// to list all tenant users for the nightly sync job.
/// Configuration required:
///   Auth0:Domain                        — e.g. your-tenant.auth0.com
///   IdentityProvider:AdminClientId      — M2M application client ID
///   IdentityProvider:AdminClientSecret  — M2M application client secret (env var or gitignored override)
/// Rate limit note: Auth0 free tier allows 1,000 Management API requests/day.
/// This service paginates with 100 users/page and adds a 200 ms delay between pages.
/// </summary>
public class Auth0ManagementService(
    HttpClient http,
    IConfiguration configuration,
    ILogger<Auth0ManagementService> logger) : IIamProviderService
{
    public async Task<List<IamUserInfo>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetManagementTokenAsync(cancellationToken);
        if (token is null) return [];

        var domain = configuration["Auth0:Domain"]!;
        var result = new List<IamUserInfo>();
        const int pageSize = 100;
        var page = 0;

        while (true)
        {
            var url = $"https://{domain}/api/v2/users?per_page={pageSize}&page={page}&include_totals=false";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new("Bearer", token);

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                logger.LogWarning("Auth0 Management API rate limit hit — aborting user sync to preserve daily quota");
                break;
            }
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Auth0 Management API returned {StatusCode} for users list", response.StatusCode);
                break;
            }

            var users = await response.Content.ReadFromJsonAsync<List<Auth0UserRepresentation>>(cancellationToken: cancellationToken);
            if (users is null || users.Count == 0) break;

            result.AddRange(users.Select(u => new IamUserInfo(u.UserId, !u.Blocked)));

            if (users.Count < pageSize) break;
            page++;

            // Respect rate limits: 200 ms pause between pages
            await Task.Delay(200, cancellationToken);
        }

        return result;
    }

    private async Task<string?> GetManagementTokenAsync(CancellationToken cancellationToken)
    {
        var domain = configuration["Auth0:Domain"];
        var clientId = configuration["IdentityProvider:AdminClientId"];
        var clientSecret = configuration["IdentityProvider:AdminClientSecret"];

        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            logger.LogWarning("IdentityProvider:AdminClientId or IdentityProvider:AdminClientSecret not configured — skipping user sync");
            return null;
        }

        var tokenUrl = $"https://{domain}/oauth/token";
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["audience"] = $"https://{domain}/api/v2/"
        };

        using var response = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(form), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to obtain Auth0 management token: {StatusCode}", response.StatusCode);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        return result?.AccessToken;
    }

    private sealed record Auth0UserRepresentation(
        [property: JsonPropertyName("user_id")] string UserId,
        [property: JsonPropertyName("blocked")] bool Blocked);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);
}
