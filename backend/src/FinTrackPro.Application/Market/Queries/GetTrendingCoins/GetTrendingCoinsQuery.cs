using FinTrackPro.Application.Common.Models;
using MediatR;

namespace FinTrackPro.Application.Market.Queries.GetTrendingCoins;

public record GetTrendingCoinsQuery : IRequest<IEnumerable<TrendingCoinDto>>;
