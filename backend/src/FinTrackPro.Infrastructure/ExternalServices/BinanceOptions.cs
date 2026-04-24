namespace FinTrackPro.Infrastructure.ExternalServices;

public sealed class BinanceOptions
{
    public const string SectionName          = "Binance";
    public string BaseUrl                     { get; init; } = "https://api.binance.com";
    public int    ExchangeInfoCacheTtlSeconds { get; init; } = 86_400; // 24 h
}
