using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetWatchedSymbols;

public class GetWatchedSymbolsQueryHandler(
    IUserRepository userRepository,
    IWatchedSymbolRepository watchedSymbolRepository,
    ICurrentUser currentUser) : IRequestHandler<GetWatchedSymbolsQuery, IEnumerable<WatchedSymbolDto>>
{
    public async Task<IEnumerable<WatchedSymbolDto>> Handle(
        GetWatchedSymbolsQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var symbols = await watchedSymbolRepository.GetByUserAsync(user.Id, cancellationToken);
        return symbols.Select(s => (WatchedSymbolDto)s);
    }
}
