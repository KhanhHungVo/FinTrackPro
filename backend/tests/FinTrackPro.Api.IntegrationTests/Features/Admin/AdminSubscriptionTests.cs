using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Tests.Common;

namespace FinTrackPro.Api.IntegrationTests.Features.Admin;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestCollection))]
public class AdminSubscriptionTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _userClient;

    public AdminSubscriptionTests(DatabaseFixture fixture)
    {
        _fixture = fixture;

        _adminClient = fixture.Factory.CreateClient();
        var adminToken = AuthTokenFactory.GenerateToken("admin-keycloak-id", "User", "Admin");
        _adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        _userClient = fixture.Factory.CreateClient();
        var userToken = AuthTokenFactory.GenerateToken("regular-user-keycloak-id", "User");
        _userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userToken);
    }

    public async Task InitializeAsync() => await _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> EnsureUserProvisionedAsync(HttpClient client)
    {
        // Hit any authed endpoint to trigger AppUser provisioning via UserContextMiddleware
        await client.GetAsync("/api/watchedsymbols");

        var response = await _adminClient.GetAsync("/api/admin/users?pageSize=100");
        var paged = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = paged.GetProperty("items").EnumerateArray().ToList();

        // Find the user whose JWT sub maps to the provisioned AppUser
        // We use email which AuthTokenFactory sets as {userId}@test.fintrackpro.dev
        var match = items.FirstOrDefault(u =>
            u.GetProperty("email").GetString()?.Contains("regular-user") == true);

        return Guid.Parse(match.GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_Returns200WithPaginatedList()
    {
        var response = await _adminClient.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("items", out _).Should().BeTrue();
        body.TryGetProperty("totalCount", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetUsers_AsUser_Returns403()
    {
        var response = await _userClient.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateSubscription_Monthly_AsAdmin_Returns200AndPlanIsPro()
    {
        var userId = await EnsureUserProvisionedAsync(_userClient);

        var response = await _adminClient.PostAsJsonAsync(
            $"/api/admin/users/{userId}/subscription",
            new { period = "Monthly" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("plan").GetString().Should().Be("Pro");
    }

    [Fact]
    public async Task ActivateSubscription_AsUser_Returns403()
    {
        var userId = Guid.NewGuid();
        var response = await _userClient.PostAsJsonAsync(
            $"/api/admin/users/{userId}/subscription",
            new { period = "Monthly" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeSubscription_AsAdmin_Returns204AndPlanIsFree()
    {
        var userId = await EnsureUserProvisionedAsync(_userClient);

        // First activate
        await _adminClient.PostAsJsonAsync(
            $"/api/admin/users/{userId}/subscription",
            new { period = "Monthly" });

        // Then revoke
        var revokeResponse = await _adminClient.DeleteAsync(
            $"/api/admin/users/{userId}/subscription");

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify via GET users
        var listResponse = await _adminClient.GetAsync($"/api/admin/users?email=regular-user");
        var paged = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var user = paged.GetProperty("items").EnumerateArray()
            .FirstOrDefault(u => Guid.Parse(u.GetProperty("id").GetString()!) == userId);
        user.GetProperty("plan").GetString().Should().Be("Free");
    }
}
