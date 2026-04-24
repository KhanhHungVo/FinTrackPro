using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using FinTrackPro.Application.Trading.Queries.GetWatchlistAnalysis;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Trading;

public class GetWatchlistAnalysisQueryHandlerTests
{
    private readonly IWatchedSymbolRepository _watchedSymbolRepository = Substitute.For<IWatchedSymbolRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IBinanceService _binanceService = Substitute.For<IBinanceService>();
    private readonly HybridCache _cache;
    private readonly GetWatchlistAnalysisQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public GetWatchlistAnalysisQueryHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var sp = services.BuildServiceProvider();
        _cache = sp.GetRequiredService<HybridCache>();

        _handler = new GetWatchlistAnalysisQueryHandler(
            _watchedSymbolRepository,
            _currentUser,
            _binanceService,
            _cache,
            Substitute.For<ILogger<GetWatchlistAnalysisQueryHandler>>());

        _currentUser.UserId.Returns(TestUser.Id);

        // Default: all symbols are valid on Binance
        _binanceService.GetValidSymbolsAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "BTCUSDT", "ETHUSDT", "SOLUSDT" });
    }

    private static IEnumerable<KlineDto> MakeKlines(int count, decimal close = 100m)
        => Enumerable.Range(0, count)
            .Select(i => new KlineDto(
                DateTime.UtcNow.AddDays(-count + i),
                close, close, close, close, 1000m));

    private static IEnumerable<KlineDto> MakeIncreasingKlines(int count)
        => Enumerable.Range(0, count)
            .Select(i => new KlineDto(
                DateTime.UtcNow.AddDays(-count + i),
                i + 1m, i + 1m, i + 1m, i + 1m, 1000m));

    [Fact]
    public async Task Handle_ThreeSymbols_AllDataAvailable_ReturnsThreeFullyPopulatedRows()
    {
        var symbols = new[]
        {
            WatchedSymbol.Create(TestUser.Id, "BTCUSDT"),
            WatchedSymbol.Create(TestUser.Id, "ETHUSDT"),
            WatchedSymbol.Create(TestUser.Id, "SOLUSDT"),
        };
        _watchedSymbolRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(symbols);

        foreach (var s in new[] { "BTCUSDT", "ETHUSDT", "SOLUSDT" })
        {
            _binanceService.Get24HrTickerAsync(s, Arg.Any<CancellationToken>())
                .Returns(new TickerDto(s, 100m, 2m, 50000m, 999m));
            _binanceService.GetKlinesAsync(s, "1d", 100, Arg.Any<CancellationToken>())
                .Returns(MakeIncreasingKlines(100));
            _binanceService.GetKlinesAsync(s, "1w", 100, Arg.Any<CancellationToken>())
                .Returns(MakeIncreasingKlines(100));
        }

        var result = (await _handler.Handle(new GetWatchlistAnalysisQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(3);
        result.Should().AllSatisfy(r =>
        {
            r.Price.Should().NotBeNull();
            r.Change24h.Should().NotBeNull();
            r.RsiDaily.Should().NotBeNull();
            r.RsiWeekly.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Handle_OneSymbolBinanceReturnsNullTicker_ThatRowHasNullPriceAndChange_OthersUnaffected()
    {
        var symbols = new[]
        {
            WatchedSymbol.Create(TestUser.Id, "BTCUSDT"),
            WatchedSymbol.Create(TestUser.Id, "ETHUSDT"),
            WatchedSymbol.Create(TestUser.Id, "SOLUSDT"),
        };
        _watchedSymbolRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(symbols);

        _binanceService.Get24HrTickerAsync("BTCUSDT", Arg.Any<CancellationToken>())
            .Returns(new TickerDto("BTCUSDT", 64000m, 2m, 50000m, 999m));
        _binanceService.Get24HrTickerAsync("ETHUSDT", Arg.Any<CancellationToken>())
            .Returns((TickerDto?)null);
        _binanceService.Get24HrTickerAsync("SOLUSDT", Arg.Any<CancellationToken>())
            .Returns(new TickerDto("SOLUSDT", 180m, 5m, 1000m, 999m));

        foreach (var s in new[] { "BTCUSDT", "ETHUSDT", "SOLUSDT" })
        {
            _binanceService.GetKlinesAsync(s, "1d", 100, Arg.Any<CancellationToken>())
                .Returns(MakeIncreasingKlines(100));
            _binanceService.GetKlinesAsync(s, "1w", 100, Arg.Any<CancellationToken>())
                .Returns(MakeIncreasingKlines(100));
        }

        var result = (await _handler.Handle(new GetWatchlistAnalysisQuery(), CancellationToken.None))
            .ToDictionary(r => r.Symbol);

        result["ETHUSDT"].Price.Should().BeNull();
        result["ETHUSDT"].Change24h.Should().BeNull();
        result["BTCUSDT"].Price.Should().NotBeNull();
        result["SOLUSDT"].Price.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_OneSymbolKlinesInsufficientForRsi_ThatRowHasNullRsi_OthersUnaffected()
    {
        var symbols = new[]
        {
            WatchedSymbol.Create(TestUser.Id, "BTCUSDT"),
            WatchedSymbol.Create(TestUser.Id, "ETHUSDT"),
            WatchedSymbol.Create(TestUser.Id, "SOLUSDT"),
        };
        _watchedSymbolRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(symbols);

        foreach (var s in new[] { "BTCUSDT", "ETHUSDT", "SOLUSDT" })
            _binanceService.Get24HrTickerAsync(s, Arg.Any<CancellationToken>())
                .Returns(new TickerDto(s, 100m, 1m, 50000m, 999m));

        // SOLUSDT has too few klines
        _binanceService.GetKlinesAsync("SOLUSDT", "1d", 100, Arg.Any<CancellationToken>())
            .Returns(MakeKlines(5));
        _binanceService.GetKlinesAsync("SOLUSDT", "1w", 100, Arg.Any<CancellationToken>())
            .Returns(MakeKlines(5));

        foreach (var s in new[] { "BTCUSDT", "ETHUSDT" })
        {
            _binanceService.GetKlinesAsync(s, "1d", 100, Arg.Any<CancellationToken>())
                .Returns(MakeIncreasingKlines(100));
            _binanceService.GetKlinesAsync(s, "1w", 100, Arg.Any<CancellationToken>())
                .Returns(MakeIncreasingKlines(100));
        }

        var result = (await _handler.Handle(new GetWatchlistAnalysisQuery(), CancellationToken.None))
            .ToDictionary(r => r.Symbol);

        result["SOLUSDT"].RsiDaily.Should().BeNull();
        result["SOLUSDT"].RsiWeekly.Should().BeNull();
        result["BTCUSDT"].RsiDaily.Should().NotBeNull();
        result["ETHUSDT"].RsiDaily.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_EmptyWatchlist_ReturnsEmptyWithoutException()
    {
        _watchedSymbolRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<WatchedSymbol>());

        var result = await _handler.Handle(new GetWatchlistAnalysisQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ResultsOrderedBySymbolAscending()
    {
        var symbols = new[]
        {
            WatchedSymbol.Create(TestUser.Id, "SOLUSDT"),
            WatchedSymbol.Create(TestUser.Id, "ETHUSDT"),
            WatchedSymbol.Create(TestUser.Id, "BTCUSDT"),
        };
        _watchedSymbolRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(symbols);

        foreach (var s in new[] { "BTCUSDT", "ETHUSDT", "SOLUSDT" })
        {
            _binanceService.Get24HrTickerAsync(s, Arg.Any<CancellationToken>())
                .Returns(new TickerDto(s, 100m, 1m, 50000m, 999m));
            _binanceService.GetKlinesAsync(s, "1d", 100, Arg.Any<CancellationToken>())
                .Returns(MakeKlines(20));
            _binanceService.GetKlinesAsync(s, "1w", 100, Arg.Any<CancellationToken>())
                .Returns(MakeKlines(20));
        }

        var result = (await _handler.Handle(new GetWatchlistAnalysisQuery(), CancellationToken.None))
            .Select(r => r.Symbol)
            .ToList();

        result.Should().BeInAscendingOrder();
        result[0].Should().Be("BTCUSDT");
        result[1].Should().Be("ETHUSDT");
        result[2].Should().Be("SOLUSDT");
    }

    [Fact]
    public async Task Handle_SymbolNotOnBinance_IsExcludedFromResults()
    {
        var symbols = new[]
        {
            WatchedSymbol.Create(TestUser.Id, "BTCUSDT"),
            WatchedSymbol.Create(TestUser.Id, "FAKECOIN"),
        };
        _watchedSymbolRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(symbols);

        // Only BTCUSDT is a valid Binance symbol
        _binanceService.GetValidSymbolsAsync(Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "BTCUSDT" });

        _binanceService.Get24HrTickerAsync("BTCUSDT", Arg.Any<CancellationToken>())
            .Returns(new TickerDto("BTCUSDT", 64000m, 2m, 50000m, 999m));
        _binanceService.GetKlinesAsync("BTCUSDT", "1d", 100, Arg.Any<CancellationToken>())
            .Returns(MakeIncreasingKlines(100));
        _binanceService.GetKlinesAsync("BTCUSDT", "1w", 100, Arg.Any<CancellationToken>())
            .Returns(MakeIncreasingKlines(100));

        var result = (await _handler.Handle(new GetWatchlistAnalysisQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result[0].Symbol.Should().Be("BTCUSDT");
    }
}
