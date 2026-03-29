using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Exceptions;

namespace FinTrackPro.Application.Common.Extensions;

public static class ExchangeRateServiceExtensions
{
    public static async Task<decimal> GetRateForCurrencyAsync(
        this IExchangeRateService service,
        string currency,
        CancellationToken ct = default)
    {
        var code = currency.ToUpperInvariant();
        if (code == "USD") return 1m;

        var rates = await service.GetRateToUsdAsync(ct);
        return rates.TryGetValue(code, out var rate)
            ? rate
            : throw new DomainException($"Exchange rate for currency '{code}' is not available.");
    }
}
