using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tests.Common;
using Tests.Common.Builders;

namespace FinTrackPro.Api.IntegrationTests.Features.Auth;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestCollection))]
public class AuthorizationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public AuthorizationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync() => await _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("GET", "/api/transactions")]
    [InlineData("GET", "/api/trades")]
    [InlineData("GET", "/api/watchedsymbols")]
    public async Task ProtectedEndpoints_NoToken_Return401(string method, string url)
    {
        var client = _fixture.Factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostTransaction_ValidUserToken_Returns201()
    {
        var client = _fixture.Factory.CreateClient();
        var token = AuthTokenFactory.GenerateToken("test-keycloak-id", "User");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostTransaction_ExpiredToken_Returns401()
    {
        var client = _fixture.Factory.CreateClient();
        // Generate a token with negative expiry (already expired)
        var expiredToken = GenerateExpiredToken("test-keycloak-id", "User");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string GenerateExpiredToken(string keycloakUserId, params string[] roles)
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var key = new System.Text.StringBuilder();
        // Use AuthTokenFactory but trick it with system clock — simplest: just pass wrong audience
        // Actually: re-implement with past expiry
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(AuthTokenFactory.TestSigningKey));

        var header = Base64UrlEncode(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64UrlEncode(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = keycloakUserId,
            iss = AuthTokenFactory.TestIssuer,
            aud = AuthTokenFactory.TestAudience,
            exp = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(),
        }));

        var signingInput = $"{header}.{payload}";
        var signature = Base64UrlEncode(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signingInput)));
        return $"{signingInput}.{signature}";
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
