using FinTrackPro.BackgroundJobs.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrackPro.BackgroundJobs;

public static class DependencyInjection
{
    public static IServiceCollection AddBackgroundJobServices(this IServiceCollection services)
    {
        services.AddScoped<MarketSignalJob>();
        services.AddScoped<BudgetOverrunJob>();
        services.AddScoped<IamUserSyncJob>();
        services.AddScoped<ExchangeRateSyncJob>();
        services.AddScoped<SignalCleanupJob>();

        return services;
    }
}
