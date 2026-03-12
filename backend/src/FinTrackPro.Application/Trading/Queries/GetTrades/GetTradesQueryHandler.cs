using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetTrades;

public class GetTradesQueryHandler : IRequestHandler<GetTradesQuery, IEnumerable<TradeDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITradeRepository _tradeRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTradesQueryHandler(
        IUserRepository userRepository,
        ITradeRepository tradeRepository,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _tradeRepository = tradeRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<TradeDto>> Handle(
        GetTradesQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(
            _currentUser.KeycloakUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), _currentUser.KeycloakUserId!);

        var trades = await _tradeRepository.GetByUserAsync(user.Id, cancellationToken);
        return trades
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (TradeDto)t);
    }
}
