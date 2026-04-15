using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<WatchedSymbol> WatchedSymbols => Set<WatchedSymbol>();
    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<TransactionCategory> TransactionCategories => Set<TransactionCategory>();

    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
        System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken)
        => Database.BeginTransactionAsync(isolationLevel, cancellationToken);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Ignore<BaseEvent>();
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
