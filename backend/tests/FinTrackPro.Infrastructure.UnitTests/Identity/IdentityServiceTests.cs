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
        var identity = new UserIdentity(ExternalId, Provider, user.Id);
        SetUserOnIdentity(identity, user);

        _identityRepo.GetAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns(identity);

        var result = await _service.ResolveAsync(BuildPrincipal());

        result.UserId.Should().Be(user.Id);
        // No DB save needed — profile unchanged
        _db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ReturningUser_ProfileChanged_Saves()
    {
        var user = AppUser.Create("old@example.com", "Old Name");
        var identity = new UserIdentity(ExternalId, Provider, user.Id);
        SetUserOnIdentity(identity, user);

        _identityRepo.GetAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns(identity);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(CancellationToken.None);

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
        var identity = new UserIdentity(ExternalId, Provider, user.Id);
        SetUserOnIdentity(identity, user);

        _identityRepo.GetAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns(identity);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(CancellationToken.None);

        await _service.ResolveAsync(BuildPrincipal());

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_NewUser_CreatesAppUserAndIdentity()
    {
        _identityRepo.GetAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns((UserIdentity?)null);
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

        _identityRepo.GetAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns((UserIdentity?)null);
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
        _identityRepo.GetAsync(ExternalId, Provider, Arg.Any<CancellationToken>())
            .Returns((UserIdentity?)null);

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

    // Helper: set the User navigation property via reflection (EF normally does this)
    private static void SetUserOnIdentity(UserIdentity identity, AppUser user)
    {
        var prop = typeof(UserIdentity).GetProperty("User")!;
        prop.SetValue(identity, user);
    }
}
