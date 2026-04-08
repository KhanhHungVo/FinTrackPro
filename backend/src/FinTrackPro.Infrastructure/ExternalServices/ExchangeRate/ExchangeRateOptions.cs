namespace FinTrackPro.Infrastructure.ExternalServices.ExchangeRate;

public sealed class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRate";
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey  { get; init; } = string.Empty;

    /// <summary>
    /// Approximate rates used when the live API is unreachable.
    /// Overridable via appsettings.json ExchangeRate:FallbackRates.
    /// </summary>
    public Dictionary<string, decimal> FallbackRates { get; init; } = new()
    {
        ["USD"] = 1m,
        ["VND"] = 26000m,
    };
}
