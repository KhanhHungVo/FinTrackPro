namespace FinTrackPro.Infrastructure.ExternalServices;

internal sealed class FearGreedOptions
{
    public const string SectionName = "FearGreed";

    public string BaseUrl { get; init; } = "https://api.alternative.me";
}
