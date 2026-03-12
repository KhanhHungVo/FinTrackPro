using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.RemoveWatchedSymbol;

public class RemoveWatchedSymbolCommandHandler : IRequestHandler<RemoveWatchedSymbolCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IWatchedSymbolRepository _watchedSymbolRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public RemoveWatchedSymbolCommandHandler(
        IApplicationDbContext context,
        IWatchedSymbolRepository watchedSymbolRepository,
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _context = context;
        _watchedSymbolRepository = watchedSymbolRepository;
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    public async Task Handle(RemoveWatchedSymbolCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(
            _currentUser.KeycloakUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.KeycloakUserId!);

        var symbol = await _watchedSymbolRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WatchedSymbol), request.Id);

        if (symbol.UserId != user.Id)
            throw new DomainException("You are not authorized to remove this symbol.");

        _watchedSymbolRepository.Remove(symbol);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
