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

    public Task<AppUser?> GetByKeycloakIdAsync(string keycloakUserId, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, cancellationToken);

    public void Add(AppUser user) => _context.Users.Add(user);
}
