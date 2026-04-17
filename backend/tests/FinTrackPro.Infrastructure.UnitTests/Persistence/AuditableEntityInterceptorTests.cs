using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Infrastructure.Persistence;
using FinTrackPro.Infrastructure.Persistence.Interceptors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;

namespace FinTrackPro.Infrastructure.UnitTests.Persistence;

public class AuditableEntityInterceptorTests
{
    private static readonly DateTime FixedUtc = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private static (ApplicationDbContext db, AuditableEntityInterceptor interceptor) CreateContext()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(FixedUtc);

        var interceptor = new AuditableEntityInterceptor(clock);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return (new ApplicationDbContext(options), interceptor);
    }

    [Fact]
    public async Task SaveChanges_NewAuditableEntity_SetsBothTimestamps()
    {
        var (db, _) = CreateContext();
        var budget = Budget.Create(Guid.NewGuid(), "Food", 500m, "USD", 1.0m, "2026-01");

        db.Budgets.Add(budget);
        await db.SaveChangesAsync();

        budget.CreatedAt.Should().Be(FixedUtc);
        budget.UpdatedAt.Should().Be(FixedUtc);
    }

    [Fact]
    public async Task SaveChanges_UpdatedAuditableEntity_SetsUpdatedAtOnly()
    {
        var (db, _) = CreateContext();
        var budget = Budget.Create(Guid.NewGuid(), "Food", 500m, "USD", 1.0m, "2026-01");
        db.Budgets.Add(budget);
        await db.SaveChangesAsync();

        var createdAt = budget.CreatedAt;

        budget.UpdateLimit(1000m);
        await db.SaveChangesAsync();

        budget.CreatedAt.Should().Be(createdAt);
        budget.UpdatedAt.Should().Be(FixedUtc);
    }

    [Fact]
    public async Task SaveChanges_NewCreatedEntity_SetsCreatedAtOnly()
    {
        var (db, _) = CreateContext();
        var symbol = WatchedSymbol.Create(Guid.NewGuid(), "BTC");

        db.WatchedSymbols.Add(symbol);
        await db.SaveChangesAsync();

        symbol.CreatedAt.Should().Be(FixedUtc);
    }

    [Fact]
    public async Task SaveChanges_NewAuditableEntity_CreatedAtNotChangedOnSubsequentUpdate()
    {
        var (db, _) = CreateContext();
        var budget = Budget.Create(Guid.NewGuid(), "Food", 500m, "USD", 1.0m, "2026-01");
        db.Budgets.Add(budget);
        await db.SaveChangesAsync();

        var originalCreatedAt = budget.CreatedAt;

        budget.UpdateLimit(750m);
        await db.SaveChangesAsync();

        budget.CreatedAt.Should().Be(originalCreatedAt);
    }
}
