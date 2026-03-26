using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.CreateTrade;

public class CreateTradeCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IUserRepository userRepository,
    IBinanceService binanceService) : IRequestHandler<CreateTradeCommand, Guid>
{
    public async Task<Guid> Handle(CreateTradeCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var isValid = await binanceService.IsValidSymbolAsync(request.Symbol, cancellationToken);
        if (!isValid)
            throw new DomainException($"Symbol '{request.Symbol}' is not a valid Binance trading pair.");

        var trade = Trade.Create(
            user.Id, request.Symbol, request.Direction,
            request.EntryPrice, request.ExitPrice,
            request.PositionSize, request.Fees, request.Notes);

        context.Trades.Add(trade);
        await context.SaveChangesAsync(cancellationToken);

        return trade.Id;
    }
}
