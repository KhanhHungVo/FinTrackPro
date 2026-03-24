using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class UserRepository(ApplicationDbContext context) : IUserRepository
{
    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<AppUser?> GetByExternalIdAsync(string externalUserId, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId, cancellationToken);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(
            u => u.Email == email.Trim().ToLowerInvariant(),
            cancellationToken);

    public Task<List<AppUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
        context.Users.ToListAsync(cancellationToken);

    public void Add(AppUser user) => context.Users.Add(user);

    public async Task<AppUser> EnsureCreatedAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        context.Users.Add(user);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException)
        {
            context.Entry(user).State = EntityState.Detached;

            var existing = await context.Users.FirstOrDefaultAsync(
                u => u.ExternalUserId == user.ExternalUserId || u.Email == user.Email,
                cancellationToken);

            if (existing is null) throw;
            return existing;
        }
    }
}
