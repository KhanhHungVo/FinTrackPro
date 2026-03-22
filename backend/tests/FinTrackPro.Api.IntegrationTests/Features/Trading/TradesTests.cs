using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tests.Common;
using Tests.Common.Builders;

namespace FinTrackPro.Api.IntegrationTests.Features.Trading;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestCollection))]
public class TradesTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public TradesTests(DatabaseFixture fixture)
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
    public async Task CreateTrade_ValidRequest_Returns201()
    {
        var request = TradeRequestBuilder.Build();

        var response = await _client.PostAsJsonAsync("/api/trades", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTrades_ReturnsCreatedTrades()
    {
        await _client.PostAsJsonAsync("/api/trades", TradeRequestBuilder.Build());

        var response = await _client.GetAsync("/api/trades");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<object>>();
        items.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task DeleteTrade_OwnedTrade_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/trades", TradeRequestBuilder.Build());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var deleteResponse = await _client.DeleteAsync($"/api/trades/{id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTrade_NonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/trades/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTrade_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/api/trades", TradeRequestBuilder.Build());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTrade_ZeroEntryPrice_Returns400()
    {
        var request = TradeRequestBuilder.Build(entryPrice: 0m);

        var response = await _client.PostAsJsonAsync("/api/trades", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTrade_EmptySymbol_Returns400()
    {
        var request = TradeRequestBuilder.Build(symbol: "");

        var response = await _client.PostAsJsonAsync("/api/trades", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
