using FinTrackPro.Application.Common.Behaviors;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Common.Behaviors;

public class EnsureUserBehaviorTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IApplicationDbContext _db = Substitute.For<IApplicationDbContext>();
    private readonly EnsureUserBehavior<TestRequest, TestResponse> _behavior;

    public EnsureUserBehaviorTests()
    {
        _behavior = new EnsureUserBehavior<TestRequest, TestResponse>(_currentUser, _userRepository, _db);
        _currentUser.ExternalUserId.Returns("kc-admin");
        _currentUser.Email.Returns("Admin@FinTrackPro.dev");
        _currentUser.DisplayName.Returns("Admin");
        _currentUser.ProviderName.Returns("keycloak");
    }

    [Fact]
    public async Task Handle_ConcurrentInsert_ReusesUserReturnedByRepository()
    {
        var existingUser = AppUser.Create("kc-admin", "admin@fintrackpro.dev", "Admin", "keycloak");

        _userRepository.GetByExternalIdAsync("kc-admin", Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);
        _userRepository.GetByEmailAsync("Admin@FinTrackPro.dev", Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);
        _userRepository.EnsureCreatedAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var result = await _behavior.Handle(
            new TestRequest(),
            _ => Task.FromResult(new TestResponse()),
            CancellationToken.None);

        result.Should().NotBeNull();
        _userRepository.DidNotReceive().Add(Arg.Any<AppUser>());
        // SyncIdentity on an already-matching user returns false → no extra save
        await _db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserMissingByExternalId_ReusesExistingUserByEmail()
    {
        var existingUser = AppUser.Create("legacy-admin", "admin@fintrackpro.dev", "Legacy Admin", "auth0");

        _userRepository.GetByExternalIdAsync("kc-admin", Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);
        _userRepository.GetByEmailAsync("Admin@FinTrackPro.dev", Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var result = await _behavior.Handle(
            new TestRequest(),
            _ => Task.FromResult(new TestResponse()),
            CancellationToken.None);

        result.Should().NotBeNull();
        existingUser.ExternalUserId.Should().Be("kc-admin");
        existingUser.Email.Should().Be("admin@fintrackpro.dev");
        existingUser.DisplayName.Should().Be("Admin");
        existingUser.Provider.Should().Be("keycloak");
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _userRepository.DidNotReceive().Add(Arg.Any<AppUser>());
    }

    private sealed record TestRequest;

    private sealed record TestResponse;
}
