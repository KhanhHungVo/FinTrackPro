using FinTrackPro.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.BackgroundJobs.Jobs;

/// <summary>
/// Hangfire recurring job — pre-warms the IMemoryCache with exchange rates every 8 h.
/// Failure is intentionally swallowed; the API falls back to the HTTP client on cache miss.
/// </summary>
public class ExchangeRateSyncJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ExchangeRateSyncJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var exchangeRateService = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
            await exchangeRateService.GetRateToUsdAsync(ct);
            logger.LogInformation("Exchange rates refreshed");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to refresh exchange rates");
        }
    }
}
