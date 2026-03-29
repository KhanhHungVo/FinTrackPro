using MediatR;

namespace FinTrackPro.Application.Market.Queries.GetExchangeRates;

public record GetExchangeRatesQuery(string[] Currencies) : IRequest<Dictionary<string, decimal>>;
