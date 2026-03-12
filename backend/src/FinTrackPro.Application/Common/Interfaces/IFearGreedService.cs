using FinTrackPro.Application.Common.Models;

namespace FinTrackPro.Application.Common.Interfaces;

public interface IFearGreedService
{
    Task<FearGreedDto?> GetLatestAsync(CancellationToken cancellationToken = default);
}
