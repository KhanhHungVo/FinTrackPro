using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetTrades;

public class GetTradesQueryHandler(
    IUserRepository userRepository,
    ITradeRepository tradeRepository,
    ICurrentUser currentUser) : IRequestHandler<GetTradesQuery, IEnumerable<TradeDto>>
{
    public async Task<IEnumerable<TradeDto>> Handle(
        GetTradesQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var trades = await tradeRepository.GetByUserAsync(user.Id, cancellationToken);
        return trades
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (TradeDto)t);
    }
}
