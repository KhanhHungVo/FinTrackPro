using FinTrackPro.Application.Admin;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Admin;

public class AdminRevokeSubscriptionHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly AdminRevokeSubscriptionCommandHandler _handler;

    public AdminRevokeSubscriptionHandlerTests()
    {
        _handler = new AdminRevokeSubscriptionCommandHandler(_userRepository, _context);
    }

    [Fact]
    public async Task Handle_ProUser_SetsPlanToFreeAndClearsExpiry()
    {
        var user = AppUser.Create("pro@dev.com", "Pro");
        user.ActivateSubscription("sub_123", DateTime.UtcNow.AddMonths(1));
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new AdminRevokeSubscriptionCommand(user.Id), CancellationToken.None);

        user.Plan.Should().Be(SubscriptionPlan.Free);
        user.SubscriptionExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _userRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((AppUser?)null);

        var act = async () => await _handler.Handle(new AdminRevokeSubscriptionCommand(missingId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
