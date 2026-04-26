using FinTrackPro.Application.Admin;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Admin;

public class AdminActivateSubscriptionHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly AdminActivateSubscriptionCommandHandler _handler;

    public AdminActivateSubscriptionHandlerTests()
    {
        _handler = new AdminActivateSubscriptionCommandHandler(_userRepository, _context);
    }

    [Fact]
    public async Task Handle_FreeUser_Monthly_SetsPlanProAndCorrectExpiry()
    {
        var user = AppUser.Create("free@dev.com", "Free");
        var userId = user.Id;
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        var before = DateTime.UtcNow;

        await _handler.Handle(new AdminActivateSubscriptionCommand(userId, BillingPeriod.Monthly), CancellationToken.None);

        user.Plan.Should().Be(SubscriptionPlan.Pro);
        user.SubscriptionExpiresAt.Should().BeCloseTo(before.AddMonths(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ActiveProUser_Monthly_ExtendsFromExistingExpiry()
    {
        var user = AppUser.Create("pro@dev.com", "Pro");
        var existingExpiry = DateTime.UtcNow.AddMonths(2);
        user.ActivateSubscription("sub_existing", existingExpiry);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new AdminActivateSubscriptionCommand(user.Id, BillingPeriod.Monthly), CancellationToken.None);

        user.SubscriptionExpiresAt.Should().BeCloseTo(existingExpiry.AddMonths(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ExpiredProUser_Monthly_ExtendsFromNow()
    {
        var user = AppUser.Create("expired@dev.com", "Expired");
        user.ActivateSubscription("sub_expired", DateTime.UtcNow.AddDays(-1));
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var before = DateTime.UtcNow;

        await _handler.Handle(new AdminActivateSubscriptionCommand(user.Id, BillingPeriod.Monthly), CancellationToken.None);

        user.SubscriptionExpiresAt.Should().BeCloseTo(before.AddMonths(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_Yearly_SetsExpiryOneYearOut()
    {
        var user = AppUser.Create("free2@dev.com", "Free2");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var before = DateTime.UtcNow;

        await _handler.Handle(new AdminActivateSubscriptionCommand(user.Id, BillingPeriod.Yearly), CancellationToken.None);

        user.SubscriptionExpiresAt.Should().BeCloseTo(before.AddYears(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _userRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((AppUser?)null);

        var act = async () => await _handler.Handle(new AdminActivateSubscriptionCommand(missingId, BillingPeriod.Monthly), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
