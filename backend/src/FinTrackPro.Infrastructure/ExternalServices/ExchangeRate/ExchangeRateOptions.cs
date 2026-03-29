namespace FinTrackPro.Infrastructure.ExternalServices.ExchangeRate;

public class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRate";
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
