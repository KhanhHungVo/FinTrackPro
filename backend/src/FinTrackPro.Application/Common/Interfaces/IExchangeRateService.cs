namespace FinTrackPro.Application.Common.Interfaces;

public interface IExchangeRateService
{
    /// <summary>
    /// Gets the exchange rates of all supported currencies to USD.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A dictionary of currency codes and their exchange rates.</returns>
    Task<Dictionary<string, decimal>> GetRateToUsdAsync(CancellationToken ct = default);
}
