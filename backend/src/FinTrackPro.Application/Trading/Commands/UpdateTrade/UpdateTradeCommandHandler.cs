using FinTrackPro.Application.Common.Extensions;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.Queries.GetTrades;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.UpdateTrade;

public class UpdateTradeCommandHandler(
    IApplicationDbContext context,
    ITradeRepository tradeRepository,
    ICurrentUser currentUser,
    IUserRepository userRepository,
    IExchangeRateService exchangeRateService) : IRequestHandler<UpdateTradeCommand, TradeDto>
{
    public async Task<TradeDto> Handle(UpdateTradeCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var trade = await tradeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Trade), request.Id);

        if (trade.UserId != user.Id)
            throw new AuthorizationException("You are not authorized to edit this trade.");

        var rateToUsd = await exchangeRateService.GetRateForCurrencyAsync(request.Currency, cancellationToken);

        trade.Update(
            request.Symbol,
            request.Direction,
            request.Status,
            request.EntryPrice,
            request.ExitPrice,
            request.CurrentPrice,
            request.PositionSize,
            request.Fees,
            request.Currency,
            rateToUsd,
            request.Notes);

        await context.SaveChangesAsync(cancellationToken);

        return (TradeDto)trade;
    }
}
