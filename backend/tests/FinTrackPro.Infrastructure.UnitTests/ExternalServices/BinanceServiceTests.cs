using System.Net;
using FinTrackPro.Infrastructure.ExternalServices;
using FinTrackPro.Infrastructure.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinTrackPro.Infrastructure.UnitTests.ExternalServices;

public class BinanceServiceTests
{
    private static BinanceService BuildService(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(status, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.binance.com") };
        return new BinanceService(httpClient, HybridCacheFactory.Create(), NullLogger<BinanceService>.Instance);
    }

    // ── GetKlinesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetKlinesAsync_ValidPayload_ParsesSuccessfully()
    {
        const string json = """
            [
              [1000000000000, "100.50", "105.00", "99.00", "103.00", "5000.00"],
              [1000000060000, "103.00", "108.00", "102.00", "107.00", "6000.00"]
            ]
            """;
        var service = BuildService(json);

        var result = (await service.GetKlinesAsync("BTCUSDT", "1d", 2)).ToList();

        result.Should().HaveCount(2);
        result[0].Open.Should().Be(100.50m);
        result[0].High.Should().Be(105.00m);
        result[0].Low.Should().Be(99.00m);
        result[0].Close.Should().Be(103.00m);
        result[0].Volume.Should().Be(5000.00m);
        result[1].Open.Should().Be(103.00m);
    }

    [Fact]
    public async Task GetKlinesAsync_MalformedNumericFields_SkipsRecord()
    {
        const string json = """
            [
              [1000000000000, "100.50", "105.00", "99.00", "103.00", "5000.00"],
              [1000000060000, "abc",    "108.00", "102.00", "107.00", "6000.00"]
            ]
            """;
        var service = BuildService(json);

        var result = (await service.GetKlinesAsync("BTCUSDT", "1d", 2)).ToList();

        result.Should().HaveCount(1);
        result[0].Open.Should().Be(100.50m);
    }

    [Fact]
    public async Task GetKlinesAsync_NullResponse_ReturnsEmpty()
    {
        var service = BuildService("null");

        var result = await service.GetKlinesAsync("BTCUSDT", "1d", 2);

        result.Should().BeEmpty();
    }

    // ── Get24HrTickerAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task Get24HrTickerAsync_ValidPayload_ParsesSuccessfully()
    {
        const string json = """{"symbol":"BTCUSDT","volume":"12345.67","quoteVolume":"99999999.00"}""";
        var service = BuildService(json);

        var result = await service.Get24HrTickerAsync("BTCUSDT");

        result.Should().NotBeNull();
        result!.Symbol.Should().Be("BTCUSDT");
        result.Volume.Should().Be(12345.67m);
        result.QuoteVolume.Should().Be(99999999.00m);
    }

    [Fact]
    public async Task Get24HrTickerAsync_ValidPayloadWithPriceFields_MapsLastPriceAndPriceChangePercent()
    {
        const string json = """
            {
              "symbol": "BTCUSDT",
              "lastPrice": "64230.50",
              "priceChangePercent": "2.45",
              "volume": "12345.67",
              "quoteVolume": "99999999.00"
            }
            """;
        var service = BuildService(json);

        var result = await service.Get24HrTickerAsync("BTCUSDT");

        result.Should().NotBeNull();
        result!.LastPrice.Should().Be(64230.50m);
        result.PriceChangePercent.Should().Be(2.45m);
    }

    [Fact]
    public async Task Get24HrTickerAsync_MissingPriceFields_ReturnsNullableFieldsAsNull()
    {
        const string json = """{"symbol":"BTCUSDT","volume":"12345.67","quoteVolume":"99999999.00"}""";
        var service = BuildService(json);

        var result = await service.Get24HrTickerAsync("BTCUSDT");

        result.Should().NotBeNull();
        result!.LastPrice.Should().BeNull();
        result.PriceChangePercent.Should().BeNull();
        result.Symbol.Should().Be("BTCUSDT");
        result.Volume.Should().Be(12345.67m);
        result.QuoteVolume.Should().Be(99999999.00m);
    }

    [Fact]
    public async Task Get24HrTickerAsync_MalformedVolumeField_ReturnsNull()
    {
        const string json = """{"symbol":"BTCUSDT","volume":"abc","quoteVolume":"99999999.00"}""";
        var service = BuildService(json);

        var result = await service.Get24HrTickerAsync("BTCUSDT");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Get24HrTickerAsync_NullResponse_ReturnsNull()
    {
        var service = BuildService("null");

        var result = await service.Get24HrTickerAsync("BTCUSDT");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Get24HrTickerAsync_WhenCanceled_RethrowsOperationCanceledException()
    {
        var service = BuildService("{}");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await service.Get24HrTickerAsync("BTCUSDT", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
