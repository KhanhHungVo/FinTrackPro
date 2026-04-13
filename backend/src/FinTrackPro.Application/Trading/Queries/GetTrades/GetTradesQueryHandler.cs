using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetTrades;

public class GetTradesQueryHandler(
    IUserRepository userRepository,
    ITradeRepository tradeRepository,
    ICurrentUser currentUser) : IRequestHandler<GetTradesQuery, PagedResult<TradeDto>>
{
    private const int MaxPageSize = 100;

    public async Task<PagedResult<TradeDto>> Handle(
        GetTradesQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var pageSize = Math.Min(request.PageSize, MaxPageSize);
        var pageQuery = new TradePageQuery(
            request.Page, pageSize, request.Search,
            request.Status, request.Direction,
            request.DateFrom, request.DateTo,
            request.SortBy, request.SortDir);

        var (items, totalCount) = await tradeRepository.GetPagedAsync(user.Id, pageQuery, cancellationToken);

        return new PagedResult<TradeDto>(
            items.Select(t => (TradeDto)t).ToList(),
            request.Page,
            pageSize,
            totalCount);
    }
}
