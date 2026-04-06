using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.AddWatchedSymbol;

public class AddWatchedSymbolCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IUserRepository userRepository,
    IWatchedSymbolRepository watchedSymbolRepository) : IRequestHandler<AddWatchedSymbolCommand, Guid>
{
    public async Task<Guid> Handle(AddWatchedSymbolCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var exists = await watchedSymbolRepository.ExistsAsync(user.Id, request.Symbol, cancellationToken);
        if (exists)
            throw new ConflictException($"Symbol '{request.Symbol}' is already in your watchlist.");

        var watchedSymbol = WatchedSymbol.Create(user.Id, request.Symbol);
        watchedSymbolRepository.Add(watchedSymbol);
        await context.SaveChangesAsync(cancellationToken);

        return watchedSymbol.Id;
    }
}
