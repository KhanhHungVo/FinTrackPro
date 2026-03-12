using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetWatchedSymbols;

public class GetWatchedSymbolsQueryHandler : IRequestHandler<GetWatchedSymbolsQuery, IEnumerable<WatchedSymbolDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IWatchedSymbolRepository _watchedSymbolRepository;
    private readonly ICurrentUserService _currentUser;

    public GetWatchedSymbolsQueryHandler(
        IUserRepository userRepository,
        IWatchedSymbolRepository watchedSymbolRepository,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _watchedSymbolRepository = watchedSymbolRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<WatchedSymbolDto>> Handle(
        GetWatchedSymbolsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(
            _currentUser.KeycloakUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.KeycloakUserId!);

        var symbols = await _watchedSymbolRepository.GetByUserAsync(user.Id, cancellationToken);
        return symbols.Select(s => (WatchedSymbolDto)s);
    }
}
