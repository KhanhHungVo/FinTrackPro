using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Subscription.Queries.GetSubscriptionStatus;

public class GetSubscriptionStatusQueryHandler(
    ICurrentUser currentUser,
    IUserRepository userRepository) : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusDto>
{
    public async Task<SubscriptionStatusDto> Handle(
        GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        return (SubscriptionStatusDto)user;
    }
}
