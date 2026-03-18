using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.CreateTrade;

public class CreateTradeCommandHandler : IRequestHandler<CreateTradeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IBinanceService _binanceService;

    public CreateTradeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IUserRepository userRepository,
        IBinanceService binanceService)
    {
        _context = context;
        _currentUser = currentUser;
        _userRepository = userRepository;
        _binanceService = binanceService;
    }

    public async Task<Guid> Handle(CreateTradeCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(
            _currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.ExternalUserId!);

        var isValid = await _binanceService.IsValidSymbolAsync(request.Symbol, cancellationToken);
        if (!isValid)
            throw new DomainException($"Symbol '{request.Symbol}' is not a valid Binance trading pair.");

        var trade = Trade.Create(
            user.Id, request.Symbol, request.Direction,
            request.EntryPrice, request.ExitPrice,
            request.PositionSize, request.Fees, request.Notes);

        _context.Trades.Add(trade);
        await _context.SaveChangesAsync(cancellationToken);

        return trade.Id;
    }
}
