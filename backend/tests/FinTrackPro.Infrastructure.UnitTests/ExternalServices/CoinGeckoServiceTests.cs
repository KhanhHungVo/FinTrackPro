using System.Net;
using FinTrackPro.Infrastructure.ExternalServices;
using FinTrackPro.Infrastructure.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinTrackPro.Infrastructure.UnitTests.ExternalServices;

public class CoinGeckoServiceTests
{
    private static CoinGeckoService BuildService(
        string json,
        HttpStatusCode status = HttpStatusCode.OK,
        IMemoryCache? cache = null)
    {
        var handler = new MockHttpMessageHandler(status, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.coingecko.com") };
        cache ??= new MemoryCache(new MemoryCacheOptions());
        return new CoinGeckoService(httpClient, cache, NullLogger<CoinGeckoService>.Instance);
    }

    private const string ValidResponse = """
        {
          "coins": [
            { "item": { "id": "bitcoin",  "name": "Bitcoin",  "symbol": "BTC", "market_cap_rank": 1 } },
            { "item": { "id": "ethereum", "name": "Ethereum", "symbol": "ETH", "market_cap_rank": 2 } },
            { "item": { "id": "solana",   "name": "Solana",   "symbol": "SOL", "market_cap_rank": 5 } }
          ]
        }
        """;

    [Fact]
    public async Task GetTrendingCoinsAsync_ValidResponse_ReturnsMappedCoins()
    {
        var service = BuildService(ValidResponse);

        var result = (await service.GetTrendingCoinsAsync()).ToList();

        result.Should().HaveCount(3);
        result[0].Id.Should().Be("bitcoin");
        result[0].Name.Should().Be("Bitcoin");
        result[0].Symbol.Should().Be("BTC");
        result[0].MarketCapRank.Should().Be(1);
        result[1].Id.Should().Be("ethereum");
        result[2].Id.Should().Be("solana");
    }

    [Fact]
    public async Task GetTrendingCoinsAsync_CachedResponse_DoesNotCallHttpClient()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = BuildService(ValidResponse, cache: cache);

        // First call populates the cache
        var first = (await service.GetTrendingCoinsAsync()).ToList();

        // Second service instance shares the same cache but would fail on network
        var errorService = BuildService("invalid-json", HttpStatusCode.InternalServerError, cache);
        var second = (await errorService.GetTrendingCoinsAsync()).ToList();

        second.Should().BeEquivalentTo(first);
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
        const string json = """
            {
              "coins": [
                { "item": { "id": "newcoin", "name": "New Coin", "symbol": "NEW", "market_cap_rank": null } }
              ]
            }
            """;
        var service = BuildService(json);

        var result = (await service.GetTrendingCoinsAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].MarketCapRank.Should().Be(0);
    }
}
