using FinTrackPro.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.BackgroundJobs.Jobs;

public class SignalCleanupJob(
    ISignalRepository signalRepository,
    ILogger<SignalCleanupJob> logger)
{
    private const int RetentionDays = 90;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
            var deleted = await signalRepository.DeleteOldDismissedAsync(cutoff, cancellationToken);
            logger.LogInformation("SignalCleanupJob deleted {Count} dismissed signal(s) older than {Days} days", deleted, RetentionDays);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SignalCleanupJob encountered an unexpected error");
        }
    }
}
