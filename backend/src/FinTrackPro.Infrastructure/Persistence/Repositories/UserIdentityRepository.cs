using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class UserIdentityRepository(ApplicationDbContext context) : IUserIdentityRepository
{
    public Task<UserIdentity?> GetAsync(
        string externalId, string provider, CancellationToken cancellationToken = default) =>
        context.UserIdentities
            .Include(i => i.User)
            .FirstOrDefaultAsync(
                i => i.ExternalUserId == externalId && i.Provider == provider,
                cancellationToken);

    public Task<List<UserIdentity>> GetByProviderAsync(
        string provider, CancellationToken cancellationToken = default) =>
        context.UserIdentities
            .Include(i => i.User)
            .Where(i => i.Provider == provider)
            .ToListAsync(cancellationToken);
}
