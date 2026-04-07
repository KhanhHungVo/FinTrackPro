namespace FinTrackPro.Infrastructure.ExternalServices;

internal sealed class CoinGeckoOptions
{
    public const string SectionName = "CoinGecko";

    public string BaseUrl { get; init; } = "https://api.coingecko.com";
    public string ApiKey  { get; init; } = string.Empty;
}
