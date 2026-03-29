using FinTrackPro.Application.Common.Interfaces;
using MediatR;

namespace FinTrackPro.Application.Market.Queries.GetExchangeRates;

public class GetExchangeRatesQueryHandler(IExchangeRateService exchangeRateService)
    : IRequestHandler<GetExchangeRatesQuery, Dictionary<string, decimal>>
{
    public async Task<Dictionary<string, decimal>> Handle(
        GetExchangeRatesQuery request, CancellationToken cancellationToken)
    {
        var requestedCodes = request.Currencies.Select(c => c.ToUpperInvariant()).ToHashSet();

        // Fetch all rates once
        var allRates = await exchangeRateService.GetRateToUsdAsync(cancellationToken);

        // Filter only requested currencies
        var filteredRates = allRates
                            .Where(kvp => requestedCodes.Contains(kvp.Key))
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return filteredRates;
    }
}
