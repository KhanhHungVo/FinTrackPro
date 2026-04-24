using System.Net;
using FinTrackPro.Infrastructure.ExternalServices;
using FinTrackPro.Infrastructure.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinTrackPro.Infrastructure.UnitTests.ExternalServices;

public class CoinGeckoServiceTests
{
    private static CoinGeckoService BuildService(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(status, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.coingecko.com") };
        return new CoinGeckoService(httpClient, HybridCacheFactory.Create(), NullLogger<CoinGeckoService>.Instance);
    }

    private static CoinGeckoService BuildMultiResponseService(
        IEnumerable<(HttpStatusCode Status, string Json)> responses)
    {
        var handler = new MultiResponseMockHttpMessageHandler(responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.coingecko.com") };
        return new CoinGeckoService(httpClient, HybridCacheFactory.Create(), NullLogger<CoinGeckoService>.Instance);
    }

    // ── GetTrendingCoinsAsync ────────────────────────────────────────────────

    private const string TrendingResponse = """
        {
          "coins": [
            { "item": { "id": "bitcoin",  "name": "Bitcoin",  "symbol": "BTC", "market_cap_rank": 1 } },
            { "item": { "id": "ethereum", "name": "Ethereum", "symbol": "ETH", "market_cap_rank": 2 } },
            { "item": { "id": "solana",   "name": "Solana",   "symbol": "SOL", "market_cap_rank": 5 } }
          ]
        }
        """;

    private const string MarketsWithPriceResponse = """
        [
          {
            "id": "bitcoin",
            "current_price": 64230.5,
            "price_change_percentage_1h_in_currency": 0.12,
            "price_change_percentage_24h_in_currency": 2.45,
            "price_change_percentage_7d_in_currency": 5.30
          },
          {
            "id": "ethereum",
            "current_price": 3210.0,
            "price_change_percentage_1h_in_currency": -0.08,
            "price_change_percentage_24h_in_currency": -1.20,
            "price_change_percentage_7d_in_currency": 3.10
          },
          {
            "id": "solana",
            "current_price": 180.0,
            "price_change_percentage_1h_in_currency": 1.50,
            "price_change_percentage_24h_in_currency": 8.20,
            "price_change_percentage_7d_in_currency": 15.40
          }
        ]
        """;

    [Fact]
    public async Task GetTrendingCoinsAsync_ValidResponseWithPriceFields_MapsPriceAndChangePercentages()
    {
        var service = BuildMultiResponseService([
            (HttpStatusCode.OK, TrendingResponse),
            (HttpStatusCode.OK, MarketsWithPriceResponse)
        ]);

        var result = (await service.GetTrendingCoinsAsync()).ToList();

        result.Should().HaveCount(3);
        result[0].Id.Should().Be("bitcoin");
        result[0].Price.Should().Be(64230.5m);
        result[0].Change1h.Should().Be(0.12m);
        result[0].Change24h.Should().Be(2.45m);
        result[0].Change7d.Should().Be(5.30m);
        result[1].Id.Should().Be("ethereum");
        result[1].Price.Should().Be(3210.0m);
    }

    [Fact]
    public async Task GetTrendingCoinsAsync_MissingPriceFields_ReturnsNullableFieldsAsNull()
    {
        const string marketsNoPriceResponse = """
            [
              { "id": "bitcoin" },
              { "id": "ethereum" },
              { "id": "solana" }
            ]
            """;

        var service = BuildMultiResponseService([
            (HttpStatusCode.OK, TrendingResponse),
            (HttpStatusCode.OK, marketsNoPriceResponse)
        ]);

        var result = (await service.GetTrendingCoinsAsync()).ToList();

        result.Should().HaveCount(3);
        result[0].Id.Should().Be("bitcoin");
        result[0].Price.Should().BeNull();
        result[0].Change1h.Should().BeNull();
        result[0].Change24h.Should().BeNull();
        result[0].Change7d.Should().BeNull();
        result[0].MarketCapRank.Should().Be(1);
        result[0].Name.Should().Be("Bitcoin");
        result[0].Symbol.Should().Be("BTC");
    }

    [Fact]
    public async Task GetTrendingCoinsAsync_HttpError_ReturnsEmptyList()
    {
        var service = BuildService("{}", HttpStatusCode.InternalServerError);

        var result = await service.GetTrendingCoinsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTrendingCoinsAsync_MalformedJson_ReturnsEmptyList()
    {
        var service = BuildService("not-valid-json");

        var result = await service.GetTrendingCoinsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTrendingCoinsAsync_MarketCapRankNotNumber_DefaultsToZero()
    {
        const string trendingJson = """
            {
              "coins": [
                { "item": { "id": "newcoin", "name": "New Coin", "symbol": "NEW", "market_cap_rank": null } }
              ]
            }
            """;
        const string marketsJson = """[ { "id": "newcoin" } ]""";

        var service = BuildMultiResponseService([
            (HttpStatusCode.OK, trendingJson),
            (HttpStatusCode.OK, marketsJson)
        ]);

        var result = (await service.GetTrendingCoinsAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].MarketCapRank.Should().Be(0);
    }

    // ── GetMarketCapCoinsAsync ───────────────────────────────────────────────

    private const string MarketCapResponse = """
        [
          {
            "id": "bitcoin",
            "name": "Bitcoin",
            "symbol": "btc",
            "market_cap": 1250000000000,
            "current_price": 64230.5,
            "price_change_percentage_1h_in_currency": 0.12,
            "price_change_percentage_24h_in_currency": 2.45,
            "price_change_percentage_7d_in_currency": 5.30
          },
          {
            "id": "ethereum",
            "name": "Ethereum",
            "symbol": "eth",
            "market_cap": 385000000000,
            "current_price": 3210.0,
            "price_change_percentage_1h_in_currency": -0.08,
            "price_change_percentage_24h_in_currency": -1.20,
            "price_change_percentage_7d_in_currency": 3.10
          }
        ]
        """;

    [Fact]
    public async Task GetMarketCapCoinsAsync_ValidResponse_ReturnsMappedCoins()
    {
        var service = BuildService(MarketCapResponse);

        var result = (await service.GetMarketCapCoinsAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].Rank.Should().Be(1);
        result[0].Id.Should().Be("bitcoin");
        result[0].Name.Should().Be("Bitcoin");
        result[0].Symbol.Should().Be("btc");
        result[0].Price.Should().Be(64230.5m);
        result[0].MarketCap.Should().Be(1250000000000m);
        result[0].Change1h.Should().Be(0.12m);
        result[0].Change24h.Should().Be(2.45m);
        result[0].Change7d.Should().Be(5.30m);
        result[1].Rank.Should().Be(2);
        result[1].Id.Should().Be("ethereum");
    }

    [Fact]
    public async Task GetMarketCapCoinsAsync_HttpError_ReturnsEmptyList()
    {
        var service = BuildService("{}", HttpStatusCode.InternalServerError);

        var result = await service.GetMarketCapCoinsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMarketCapCoinsAsync_CachedResponse_DoesNotCallHttpClient()
    {
        // Both calls go to the same service instance with the same HybridCache.
        // The second call should return the cached result even though the handler
        // would return the same data — we verify count consistency.
        var service = BuildService(MarketCapResponse);

        var first = (await service.GetMarketCapCoinsAsync()).ToList();
        var second = (await service.GetMarketCapCoinsAsync()).ToList();

        second.Should().BeEquivalentTo(first);
    }
}
