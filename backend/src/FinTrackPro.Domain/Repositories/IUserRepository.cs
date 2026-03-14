using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByKeycloakIdAsync(string keycloakUserId, CancellationToken cancellationToken = default);
    Task<List<AppUser>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(AppUser user);
}
