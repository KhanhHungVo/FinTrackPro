using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetTrades;

public class GetTradesQueryHandler(
    IUserRepository userRepository,
    ITradeRepository tradeRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetTradesQuery, IEnumerable<TradeDto>>
{
    public async Task<IEnumerable<TradeDto>> Handle(
        GetTradesQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(
            currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.ExternalUserId!);

        var trades = await tradeRepository.GetByUserAsync(user.Id, cancellationToken);
        return trades
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (TradeDto)t);
    }
}
