using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class UserRepository(ApplicationDbContext context) : IUserRepository
{
    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.Users
            .Include(u => u.Identities)
            .FirstOrDefaultAsync(
                u => u.Email == email.Trim().ToLowerInvariant(),
                cancellationToken);

    public Task<AppUser?> GetByPaymentCustomerIdAsync(string customerId, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.PaymentCustomerId == customerId, cancellationToken);

    public Task<List<AppUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
        context.Users.ToListAsync(cancellationToken);

    public void Add(AppUser user) => context.Users.Add(user);
}
