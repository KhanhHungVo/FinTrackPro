using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Signals.Queries.GetSignals;

public class GetSignalsQueryHandler(
    IUserRepository userRepository,
    ISignalRepository signalRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetSignalsQuery, IEnumerable<SignalDto>>
{
    public async Task<IEnumerable<SignalDto>> Handle(
        GetSignalsQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(
            currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.ExternalUserId!);

        var signals = await signalRepository.GetLatestByUserAsync(user.Id, request.Count, cancellationToken);
        return signals.Select(s => (SignalDto)s);
    }
}
