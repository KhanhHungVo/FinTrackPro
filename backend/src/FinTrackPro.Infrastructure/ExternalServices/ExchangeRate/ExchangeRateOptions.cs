namespace FinTrackPro.Infrastructure.ExternalServices.ExchangeRate;

public class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRate";
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Approximate rates used when the live API is unreachable.
    /// Overridable via appsettings.json ExchangeRate:FallbackRates.
    /// </summary>
    public Dictionary<string, decimal> FallbackRates { get; set; } = new()
    {
        ["USD"] = 1m,
        ["VND"] = 26000m,
    };
}
