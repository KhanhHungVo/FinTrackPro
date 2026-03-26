using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.RemoveWatchedSymbol;

public class RemoveWatchedSymbolCommandHandler(
    IApplicationDbContext context,
    IWatchedSymbolRepository watchedSymbolRepository,
    ICurrentUser currentUser,
    IUserRepository userRepository) : IRequestHandler<RemoveWatchedSymbolCommand>
{
    public async Task Handle(RemoveWatchedSymbolCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var symbol = await watchedSymbolRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WatchedSymbol), request.Id);

        if (symbol.UserId != user.Id)
            throw new AuthorizationException("You are not authorized to remove this symbol.");

        watchedSymbolRepository.Remove(symbol);
        await context.SaveChangesAsync(cancellationToken);
    }
}
