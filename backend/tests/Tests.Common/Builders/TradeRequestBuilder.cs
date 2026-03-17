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
        decimal? exitPrice = null)
    {
        var entry = entryPrice ?? _faker.Finance.Amount(100, 50000);
        var exit = exitPrice ?? _faker.Finance.Amount(100, 50000);

        return new
        {
            symbol = symbol ?? _faker.PickRandom(Symbols),
            direction = direction ?? _faker.PickRandom<TradeDirection>(),
            entryPrice = entry,
            exitPrice = exit,
            positionSize = _faker.Finance.Amount(0.01m, 10m),
            fees = _faker.Finance.Amount(0.1m, 50m),
            notes = _faker.Lorem.Sentence(),
        };
    }
}
