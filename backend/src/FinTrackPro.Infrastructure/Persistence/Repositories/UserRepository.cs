using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    public UserRepository(ApplicationDbContext context) => _context = context;

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<AppUser?> GetByExternalIdAsync(string externalUserId, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId, cancellationToken);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(
            u => u.Email == email.Trim().ToLowerInvariant(),
            cancellationToken);

    public Task<List<AppUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.Users.ToListAsync(cancellationToken);

    public void Add(AppUser user) => _context.Users.Add(user);

    public async Task<AppUser> EnsureCreatedAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException)
        {
            _context.Entry(user).State = EntityState.Detached;

            var existing = await _context.Users.FirstOrDefaultAsync(
                u => u.ExternalUserId == user.ExternalUserId || u.Email == user.Email,
                cancellationToken);

            if (existing is null) throw;
            return existing;
        }
    }
}
