namespace FinTrackPro.Infrastructure.ExternalServices;

internal sealed class BinanceOptions
{
    public const string SectionName = "Binance";

    public string BaseUrl { get; init; } = "https://api.binance.com";
}
