using Bogus;
using FinTrackPro.Domain.Enums;

namespace Tests.Common.Builders;

public static class TradeRequestBuilder
{
    private static readonly Faker _faker = new();

    private static readonly string[] Symbols = ["BTCUSDT", "ETHUSDT", "SOLUSDT", "BNBUSDT"];

    public static object Build(
        string? symbol = null,
        TradeDirection? direction = null,
        decimal? entryPrice = null,
        decimal? exitPrice = null,
        string? currency = null,
        TradeStatus? status = null)
    {
        var resolvedStatus = status ?? TradeStatus.Closed;
        var entry = entryPrice ?? _faker.Finance.Amount(100, 50000);
        var exit = exitPrice ?? (resolvedStatus == TradeStatus.Closed ? _faker.Finance.Amount(100, 50000) : (decimal?)null);

        return new
        {
            symbol = symbol ?? _faker.PickRandom(Symbols),
            direction = direction ?? _faker.PickRandom<TradeDirection>(),
            status = resolvedStatus,
            entryPrice = entry,
            exitPrice = exit,
            currentPrice = resolvedStatus == TradeStatus.Open ? _faker.Finance.Amount(100, 50000) : (decimal?)null,
            positionSize = _faker.Finance.Amount(0.01m, 10m),
            fees = _faker.Finance.Amount(0.1m, 50m),
            notes = _faker.Lorem.Sentence(),
            currency = currency ?? "USD"
        };
    }

    public static object BuildOpen(
        string? symbol = null,
        decimal? entryPrice = null,
        decimal? currentPrice = null)
    {
        var entry = entryPrice ?? _faker.Finance.Amount(100, 50000);

        return new
        {
            symbol = symbol ?? _faker.PickRandom(Symbols),
            direction = _faker.PickRandom<TradeDirection>(),
            status = TradeStatus.Open,
            entryPrice = entry,
            exitPrice = (decimal?)null,
            currentPrice = currentPrice ?? _faker.Finance.Amount(100, 50000),
            positionSize = _faker.Finance.Amount(0.01m, 10m),
            fees = 0m,
            notes = (string?)null,
            currency = "USD"
        };
    }
}
