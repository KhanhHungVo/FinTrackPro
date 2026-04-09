using FinTrackPro.Application.Common.Extensions;
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
    ITradeRepository tradeRepository,
    ISubscriptionLimitService subscriptionLimitService,
    IExchangeRateService exchangeRateService) : IRequestHandler<CreateTradeCommand, Guid>
{
    public async Task<Guid> Handle(CreateTradeCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        await subscriptionLimitService.EnforceTradeLimitAsync(user, tradeRepository, cancellationToken);

        var rateToUsd = await exchangeRateService.GetRateForCurrencyAsync(request.Currency, cancellationToken);

        var trade = Trade.Create(
            user.Id, request.Symbol.ToUpperInvariant(), request.Direction, request.Status,
            request.EntryPrice, request.ExitPrice, request.CurrentPrice,
            request.PositionSize, request.Fees,
            request.Currency, rateToUsd, request.Notes);

        context.Trades.Add(trade);
        await context.SaveChangesAsync(cancellationToken);

        return trade.Id;
    }
}
