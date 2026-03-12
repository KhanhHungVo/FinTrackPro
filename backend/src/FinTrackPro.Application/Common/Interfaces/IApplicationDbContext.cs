using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Budget> Budgets { get; }
    DbSet<Trade> Trades { get; }
    DbSet<WatchedSymbol> WatchedSymbols { get; }
    DbSet<Signal> Signals { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
