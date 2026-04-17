using FinTrackPro.Application.Common.Interfaces;

namespace FinTrackPro.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
