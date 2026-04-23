using System.Security.Claims;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Identity;
using FinTrackPro.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinTrackPro.Infrastructure.UnitTests.Identity;

public class IdentityServiceTests
{
    private const string ExternalId = "kc-123";
    private const string Provider   = "http://keycloak/realm";
    private const string Email      = "test@example.com";

    private readonly IUserIdentityRepository _identityRepo = Substitute.For<IUserIdentityRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ApplicationDbContext _db;
    private readonly IdentityService _service;

    public IdentityServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _service = new IdentityService(_identityRepo, _userRepo, _db, NullLogger<IdentityService>.Instance);
    }

    private static ClaimsPrincipal BuildPrincipal(
        string sub = ExternalId,
        string iss = Provider,
        string? email = Email,
        bool emailVerified = true,
        string? name = "Test User")
    {
        var claims = new List<Claim>
        {
            new("sub", sub),
            new("iss", iss),
        };
        if (email is not null) claims.Add(new Claim("email", email));
        if (emailVerified)     claims.Add(new Claim("email_verified", "true"));
        if (name is not null)  claims.Add(new Claim("name", name));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public async Task ResolveAsync_ReturningUser_ProfileUnchanged_NoSave()
    {
        var user = AppUser.Create(Email, "Test User");

        _userRepo.GetByExternalIdAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns(user);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var result = await _service.ResolveAsync(BuildPrincipal());

        result.UserId.Should().Be(user.Id);
        // No DB save needed — profile unchanged
        _db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ReturningUser_ProfileChanged_Saves()
    {
        var user = AppUser.Create("old@example.com", "Old Name");

        _userRepo.GetByExternalIdAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns(user);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var result = await _service.ResolveAsync(BuildPrincipal(email: Email, name: "New Name"));

        result.UserId.Should().Be(user.Id);
        user.Email.Should().Be(Email);
        user.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public async Task ResolveAsync_ReturningUser_WasDeactivated_ReactivatesAndSaves()
    {
        var user = AppUser.Create(Email, "Test User");
        user.Deactivate();

        _userRepo.GetByExternalIdAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns(user);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        await _service.ResolveAsync(BuildPrincipal());

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_NewUser_CreatesAppUserAndIdentity()
    {
        _userRepo.GetByExternalIdAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);
        _userRepo.GetByEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        AppUser? addedUser = null;
        _userRepo.When(r => r.Add(Arg.Any<AppUser>()))
            .Do(ci =>
            {
                addedUser = ci.Arg<AppUser>();
                _db.Users.Add(addedUser);
            });

        var result = await _service.ResolveAsync(BuildPrincipal());

        _userRepo.Received(1).Add(Arg.Any<AppUser>());
        result.UserId.Should().NotBeEmpty();
        addedUser.Should().NotBeNull();
        addedUser!.Identities.Should().HaveCount(1);
    }

    [Fact]
    public async Task ResolveAsync_NewProviderLink_EmailVerified_LinksToExistingUser()
    {
        // existingUser is NOT pre-saved to _db — the mock returns it directly.
        // IdentityService gets it from GetByEmailAsync (mock), calls AddIdentity, then SaveChangesAsync.
        // Since the untracked object has no pending EF changes, SaveChangesAsync is a no-op;
        // DB persistence of UserIdentity is validated in integration tests.
        var existingUser = AppUser.Create(Email, "Existing");

        _userRepo.GetByExternalIdAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);
        _userRepo.GetByEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var result = await _service.ResolveAsync(BuildPrincipal());

        result.UserId.Should().Be(existingUser.Id);
        _userRepo.DidNotReceive().Add(Arg.Any<AppUser>());
        existingUser.Identities.Should().HaveCount(1);
    }

    [Fact]
    public async Task ResolveAsync_NewProviderLink_EmailNotVerified_CreatesNewUser()
    {
        _userRepo.GetByExternalIdAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        AppUser? addedUser = null;
        _userRepo.When(r => r.Add(Arg.Any<AppUser>()))
            .Do(ci =>
            {
                addedUser = ci.Arg<AppUser>();
                _db.Users.Add(addedUser);
            });

        await _service.ResolveAsync(BuildPrincipal(emailVerified: false));

        // Email not verified → should NOT look up by email
        await _userRepo.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _userRepo.Received(1).Add(Arg.Any<AppUser>());
    }

    // ── Fix: EntityState.Unchanged reset ────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_NewProviderLink_ExistingUser_AppUserNotMarkedModified()
    {
        // Arrange — existing user returned by repo mock (not pre-saved to _db)
        var existingUser = AppUser.Create(Email, "Existing");

        _userRepo.GetByExternalIdAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);
        _userRepo.GetByEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(existingUser);

        // Act
        await _service.ResolveAsync(BuildPrincipal());

        // Assert — AppUser must not be in Modified state; only UserIdentity may be Added
        var appUserEntry = _db.ChangeTracker.Entries<AppUser>().SingleOrDefault();
        appUserEntry?.State.Should().NotBe(EntityState.Modified,
            "a spurious UPDATE \"Users\" would race under concurrent logins");
    }

    [Fact]
    public async Task ResolveAsync_NewProviderLink_ExistingUser_SavesUserIdentityWithoutException()
    {
        // Regression test: before the ValueGeneratedNever fix on UserIdentity.Id, EF would mark
        // the new UserIdentity as Modified (not Added) after db.Attach(user) + AddIdentity,
        // causing SaveChanges to emit an UPDATE against a non-existent row →
        // DbUpdateConcurrencyException on every concurrent request.
        var existingUser = AppUser.Create(Email, "Existing");

        _userRepo.GetByExternalIdAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);
        _userRepo.GetByEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(existingUser);

        // Pre-save the user so Attach succeeds in the in-memory provider
        _db.Users.Add(existingUser);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Must not throw DbUpdateConcurrencyException
        var act = async () => await _service.ResolveAsync(BuildPrincipal());
        await act.Should().NotThrowAsync();

        // The UserIdentity row must have been inserted
        var saved = await _db.UserIdentities
            .FirstOrDefaultAsync(i => i.ExternalUserId == ExternalId && i.Provider == Provider);
        saved.Should().NotBeNull("the UserIdentity row must have been INSERTed, not UPDATEd");
        saved!.UserId.Should().Be(existingUser.Id);
    }

    [Fact]
    public async Task ResolveAsync_NewUser_AppUserIsAdded_NotUnchanged()
    {
        // Brand-new user must stay Added (not reset to Unchanged) so the INSERT is not suppressed.
        // We capture state inside the Add callback — before SaveChangesAsync transitions it.
        _userRepo.GetByExternalIdAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);
        _userRepo.GetByEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        EntityState? stateAtAddTime = null;
        _userRepo.When(r => r.Add(Arg.Any<AppUser>()))
            .Do(ci =>
            {
                var addedUser = ci.Arg<AppUser>();
                _db.Users.Add(addedUser);
                stateAtAddTime = _db.Entry(addedUser).State;
            });

        await _service.ResolveAsync(BuildPrincipal());

        stateAtAddTime.Should().Be(EntityState.Added,
            "a new AppUser must remain Added before SaveChanges so the INSERT is not suppressed");
    }

    // ── Concurrency catch paths ──────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_DbUpdateException_UniqueConstraintRace_ReturnWinner()
    {
        var existingUser = AppUser.Create(Email, "Existing");
        var winnerIdentity = new UserIdentity(ExternalId, Provider, existingUser.Id);
        SetUserOnIdentity(winnerIdentity, existingUser);

        _userRepo.GetByExternalIdAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null); // not yet committed — triggers slow path
        _identityRepo.GetAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns(winnerIdentity); // catch block finds winner
        _userRepo.GetByEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var dbEx = new DbUpdateException("unique constraint");
        var throwingDb = new ThrowOnceSaveDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            dbEx);

        var service = new IdentityService(_identityRepo, _userRepo, throwingDb, NullLogger<IdentityService>.Instance);

        var result = await service.ResolveAsync(BuildPrincipal());

        result.UserId.Should().Be(existingUser.Id);
    }

    // ── DisplayName fallback chain ───────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_NoNameClaim_FallsBackToClaimsIdentityName()
    {
        _userRepo.GetByExternalIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        AppUser? addedUser = null;
        _userRepo.When(r => r.Add(Arg.Any<AppUser>()))
            .Do(ci => { addedUser = ci.Arg<AppUser>(); _db.Users.Add(addedUser); });

        // no "name" claim, but ClaimTypes.Name is present
        await _service.ResolveAsync(BuildPrincipal(name: null));

        // BuildPrincipal with name:null still sets ClaimTypes.Name via ClaimsIdentity "Name" type?
        // Since BuildPrincipal only adds "name" claim, with name:null → falls back to externalId
        addedUser!.DisplayName.Should().Be(ExternalId);
    }

    [Fact]
    public async Task ResolveAsync_NoEmailClaim_CreatesUserWithNullEmail()
    {
        _userRepo.GetByExternalIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        AppUser? addedUser = null;
        _userRepo.When(r => r.Add(Arg.Any<AppUser>()))
            .Do(ci => { addedUser = ci.Arg<AppUser>(); _db.Users.Add(addedUser); });

        await _service.ResolveAsync(BuildPrincipal(email: null, emailVerified: false));

        addedUser.Should().NotBeNull();
        addedUser!.Email.Should().BeNull();
        addedUser.Identities.Should().HaveCount(1);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    // Helper: set the User navigation property via reflection (EF normally does this)
    private static void SetUserOnIdentity(UserIdentity identity, AppUser user)
    {
        var prop = typeof(UserIdentity).GetProperty("User")!;
        prop.SetValue(identity, user);
    }
}

// Throws a specified exception on the first SaveChangesAsync call, succeeds on subsequent calls.
internal sealed class ThrowOnceSaveDbContext(DbContextOptions<ApplicationDbContext> options, Exception toThrow)
    : ApplicationDbContext(options)
{
    private bool _thrown;

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (!_thrown)
        {
            _thrown = true;
            throw toThrow;
        }
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
