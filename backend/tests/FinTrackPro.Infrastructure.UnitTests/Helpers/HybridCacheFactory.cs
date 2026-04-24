using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrackPro.Infrastructure.UnitTests.Helpers;

public static class HybridCacheFactory
{
    public static HybridCache Create()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }
}
