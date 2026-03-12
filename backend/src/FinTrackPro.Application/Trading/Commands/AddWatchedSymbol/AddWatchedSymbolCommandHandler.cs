using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.AddWatchedSymbol;

public class AddWatchedSymbolCommandHandler : IRequestHandler<AddWatchedSymbolCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IWatchedSymbolRepository _watchedSymbolRepository;
    private readonly IBinanceService _binanceService;

    public AddWatchedSymbolCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IUserRepository userRepository,
        IWatchedSymbolRepository watchedSymbolRepository,
        IBinanceService binanceService)
    {
        _context = context;
        _currentUser = currentUser;
        _userRepository = userRepository;
        _watchedSymbolRepository = watchedSymbolRepository;
        _binanceService = binanceService;
    }

    public async Task<Guid> Handle(AddWatchedSymbolCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(
            _currentUser.KeycloakUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.KeycloakUserId!);

        var isValid = await _binanceService.IsValidSymbolAsync(request.Symbol, cancellationToken);
        if (!isValid)
            throw new DomainException($"Symbol '{request.Symbol}' is not a valid Binance trading pair.");

        var exists = await _watchedSymbolRepository.ExistsAsync(user.Id, request.Symbol, cancellationToken);
        if (exists)
            throw new DomainException($"Symbol '{request.Symbol}' is already in your watchlist.");

        var watchedSymbol = WatchedSymbol.Create(user.Id, request.Symbol);
        _watchedSymbolRepository.Add(watchedSymbol);
        await _context.SaveChangesAsync(cancellationToken);

        return watchedSymbol.Id;
    }
}
