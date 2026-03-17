using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tests.Common;

namespace FinTrackPro.Api.IntegrationTests.Features.Trading;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestCollection))]
public class WatchedSymbolsTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public WatchedSymbolsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();

        var token = AuthTokenFactory.GenerateToken("test-keycloak-id", "User");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task InitializeAsync() => await _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddWatchedSymbol_ValidSymbol_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/watchedsymbols", new { symbol = "BTCUSDT" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWatchedSymbols_ReturnsAddedSymbols()
    {
        await _client.PostAsJsonAsync("/api/watchedsymbols", new { symbol = "ETHUSDT" });

        var response = await _client.GetAsync("/api/watchedsymbols");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<object>>();
        items.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task RemoveWatchedSymbol_ExistingId_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/watchedsymbols", new { symbol = "SOLUSDT" });
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var deleteResponse = await _client.DeleteAsync($"/api/watchedsymbols/{id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AddWatchedSymbol_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/api/watchedsymbols", new { symbol = "BTCUSDT" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
