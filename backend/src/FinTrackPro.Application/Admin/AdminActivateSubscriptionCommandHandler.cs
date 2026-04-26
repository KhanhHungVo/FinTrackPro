using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Subscription.Queries.GetSubscriptionStatus;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Admin;

public class AdminActivateSubscriptionCommandHandler(
    IUserRepository userRepository,
    IApplicationDbContext context)
    : IRequestHandler<AdminActivateSubscriptionCommand, SubscriptionStatusDto>
{
    public async Task<SubscriptionStatusDto> Handle(
        AdminActivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), request.UserId);

        user.RenewSubscription(request.Period);
        await context.SaveChangesAsync(cancellationToken);

        return (SubscriptionStatusDto)user;
    }
}
