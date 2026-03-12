using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Signals.Queries.GetSignals;

public class GetSignalsQueryHandler : IRequestHandler<GetSignalsQuery, IEnumerable<SignalDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ISignalRepository _signalRepository;
    private readonly ICurrentUserService _currentUser;

    public GetSignalsQueryHandler(
        IUserRepository userRepository,
        ISignalRepository signalRepository,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _signalRepository = signalRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<SignalDto>> Handle(
        GetSignalsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(
            _currentUser.KeycloakUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.KeycloakUserId!);

        var signals = await _signalRepository.GetLatestByUserAsync(user.Id, request.Count, cancellationToken);
        return signals.Select(s => (SignalDto)s);
    }
}
