using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Signals.Queries.GetSignals;

public class GetSignalsQueryHandler(
    IUserRepository userRepository,
    ISignalRepository signalRepository,
    ICurrentUser currentUser,
    ISubscriptionLimitService subscriptionLimitService) : IRequestHandler<GetSignalsQuery, IEnumerable<SignalDto>>
{
    public async Task<IEnumerable<SignalDto>> Handle(
        GetSignalsQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        await subscriptionLimitService.EnforceWatchlistReadAccessAsync(user, cancellationToken);

        var signals = await signalRepository.GetLatestByUserAsync(user.Id, request.Count, cancellationToken);
        return signals.Select(s => (SignalDto)s);
    }
}
