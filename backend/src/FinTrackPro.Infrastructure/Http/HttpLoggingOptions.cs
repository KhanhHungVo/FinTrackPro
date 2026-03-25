namespace FinTrackPro.Infrastructure.Http;

internal sealed class HttpLoggingOptions
{
    public const string SectionName = "HttpLogging";

    /// <summary>
    /// When true (default), sensitive headers and body fields are redacted before logging.
    /// Set to false in local development to see raw payloads.
    /// </summary>
    public bool MaskSensitiveData { get; init; } = true;
}
