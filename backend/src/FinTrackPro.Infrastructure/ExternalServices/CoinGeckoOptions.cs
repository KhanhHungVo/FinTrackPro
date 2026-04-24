namespace FinTrackPro.Infrastructure.ExternalServices;

public sealed class CoinGeckoOptions
{
    public const string SectionName          = "CoinGecko";
    public const int    DefaultCacheTtlSeconds = 60;

    public string BaseUrl                  { get; init; } = "https://api.coingecko.com";
    public string ApiKey                   { get; init; } = string.Empty;
    public int    TrendingCacheTtlSeconds  { get; init; } = DefaultCacheTtlSeconds;
    public int    MarketCapCacheTtlSeconds { get; init; } = DefaultCacheTtlSeconds;
}
