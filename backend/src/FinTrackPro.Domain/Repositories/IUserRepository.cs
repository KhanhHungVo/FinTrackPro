using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByExternalIdAsync(string externalUserId, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<AppUser>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(AppUser user);

    /// <summary>
    /// Inserts <paramref name="user"/> if no row with the same ExternalUserId or Email exists.
    /// Returns the canonical user (newly inserted, or the existing one that won a concurrent-insert race).
    /// </summary>
    Task<AppUser> EnsureCreatedAsync(AppUser user, CancellationToken cancellationToken = default);
}
