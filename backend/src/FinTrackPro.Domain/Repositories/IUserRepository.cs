using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByExternalIdAsync(string externalId, string provider, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByPaymentCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<List<AppUser>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(AppUser user);
}
