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

    // ---------------------------------------------------------------
    // CREATE — closed trade
    // ---------------------------------------------------------------

    [Fact]
    public async Task CreateTrade_ValidClosedRequest_Returns201()
    {
        var request = TradeRequestBuilder.Build();

        var response = await _client.PostAsJsonAsync("/api/trades", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTrade_OpenWithoutExitPrice_Returns201()
    {
        var request = TradeRequestBuilder.BuildOpen();

        var response = await _client.PostAsJsonAsync("/api/trades", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTrade_ClosedWithoutExitPrice_Returns400()
    {
        var request = new
        {
            symbol = "BTCUSDT",
            direction = "Long",
            status = "Closed",
            entryPrice = 30000m,
            exitPrice = (decimal?)null,
            currentPrice = (decimal?)null,
            positionSize = 0.1m,
            fees = 5m,
            currency = "USD",
            notes = (string?)null,
        };

        var response = await _client.PostAsJsonAsync("/api/trades", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    [Fact]
    public async Task CreateTrade_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/api/trades", TradeRequestBuilder.Build());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------
    // GET
    // ---------------------------------------------------------------

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
    public async Task GetTrades_ResponseIncludesStatusField()
    {
        await _client.PostAsJsonAsync("/api/trades", TradeRequestBuilder.Build());

        var response = await _client.GetAsync("/api/trades");
        var body = await response.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();

        body.Should().NotBeEmpty();
        body![0].TryGetProperty("status", out _).Should().BeTrue();
    }

    // ---------------------------------------------------------------
    // DELETE
    // ---------------------------------------------------------------

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

    // ---------------------------------------------------------------
    // UPDATE
    // ---------------------------------------------------------------

    [Fact]
    public async Task UpdateTrade_ValidRequest_Returns200WithUpdatedValues()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/trades", TradeRequestBuilder.Build(symbol: "BTCUSDT"));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateRequest = new
        {
            symbol = "ETHUSDT",
            direction = "Short",
            status = "Closed",
            entryPrice = 2000m,
            exitPrice = 2500m,
            currentPrice = (decimal?)null,
            positionSize = 1m,
            fees = 10m,
            notes = "Updated\nMultiline note",
            currency = "USD",
        };

        var response = await _client.PutAsJsonAsync($"/api/trades/{id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("symbol").GetString().Should().Be("ETHUSDT");
        body.GetProperty("notes").GetString().Should().Be("Updated\nMultiline note");
        body.GetProperty("status").GetString().Should().Be("Closed");
    }

    [Fact]
    public async Task UpdateTrade_NonExistentId_Returns404()
    {
        var updateRequest = TradeRequestBuilder.Build();

        var response = await _client.PutAsJsonAsync($"/api/trades/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTrade_ZeroExitPrice_Returns400()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/trades", TradeRequestBuilder.Build());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateRequest = TradeRequestBuilder.Build(exitPrice: 0m);

        var response = await _client.PutAsJsonAsync($"/api/trades/{id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTrade_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.PutAsJsonAsync($"/api/trades/{Guid.NewGuid()}", TradeRequestBuilder.Build());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------
    // CLOSE POSITION
    // ---------------------------------------------------------------

    [Fact]
    public async Task ClosePosition_ValidRequest_Returns200WithStatusClosed()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/trades", TradeRequestBuilder.BuildOpen());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var closeRequest = new { exitPrice = 35000m, fees = 5m };

        var response = await _client.PatchAsJsonAsync($"/api/trades/{id}/close", closeRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Closed");
        body.GetProperty("exitPrice").GetDecimal().Should().Be(35000m);
        body.TryGetProperty("currentPrice", out var cp).Should().BeTrue();
        cp.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task ClosePosition_AlreadyClosed_Returns409()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/trades", TradeRequestBuilder.Build());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var closeRequest = new { exitPrice = 35000m, fees = 5m };

        var response = await _client.PatchAsJsonAsync($"/api/trades/{id}/close", closeRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ClosePosition_MissingExitPrice_Returns400()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/trades", TradeRequestBuilder.BuildOpen());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var closeRequest = new { exitPrice = 0m, fees = 0m };

        var response = await _client.PatchAsJsonAsync($"/api/trades/{id}/close", closeRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ClosePosition_NonExistentId_Returns404()
    {
        var closeRequest = new { exitPrice = 35000m, fees = 0m };

        var response = await _client.PatchAsJsonAsync($"/api/trades/{Guid.NewGuid()}/close", closeRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
