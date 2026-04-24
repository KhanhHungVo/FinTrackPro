using FinTrackPro.Application.Common.Models;
using MediatR;

namespace FinTrackPro.Application.Market.Queries.GetMarketCapCoins;

public record GetMarketCapCoinsQuery : IRequest<IEnumerable<MarketCapCoinDto>>;
