namespace FinTrackPro.Infrastructure.Http;

internal sealed class HttpResilienceOptions
{
    public const string SectionName = "HttpResilience";

    /// <summary>Maximum number of retry attempts on transient failures (5xx, network errors).</summary>
    public int RetryCount { get; init; } = 3;

    /// <summary>Base delay in milliseconds for exponential back-off between retries.</summary>
    public int RetryBaseDelayMs { get; init; } = 500;

    /// <summary>Total request timeout in seconds, covering all retries.</summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>Percentage of failures (0–100) within the sampling window that opens the circuit.</summary>
    public int CircuitBreakerFailurePercent { get; init; } = 50;

    /// <summary>How long (seconds) the circuit stays open before allowing a probe request.</summary>
    public int CircuitBreakerBreakDurationSeconds { get; init; } = 30;

    /// <summary>Sliding window (seconds) over which failure ratio is measured.</summary>
    public int CircuitBreakerSamplingDurationSeconds { get; init; } = 60;

    /// <summary>Minimum number of requests in the sampling window before the circuit can open.</summary>
    public int CircuitBreakerMinimumThroughput { get; init; } = 5;
}
