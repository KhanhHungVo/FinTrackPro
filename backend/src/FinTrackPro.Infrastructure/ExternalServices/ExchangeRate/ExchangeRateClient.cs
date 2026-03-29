using System.Net.Http.Json;
using System.Text.Json;
using FinTrackPro.Infrastructure.ExternalServices.ExchangeRate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ExchangeRateClient : IExchangeRateClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<ExchangeRateClient> _logger;

    public ExchangeRateClient(
        HttpClient httpClient,
        IOptions<ExchangeRateOptions> options,
        ILogger<ExchangeRateClient> logger)
    {
        _httpClient = httpClient;
        _apiKey = options.Value.ApiKey;
        _logger = logger;
    }

    public async Task<Dictionary<string, decimal>> GetLatestRatesAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("ExchangeRate ApiKey is not configured — skipping fetch");
            return new Dictionary<string, decimal>();
        }

        var response = await _httpClient.GetAsync($"{_apiKey}/latest/USD", ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Exchange API failed with {StatusCode}", response.StatusCode);
            return new Dictionary<string, decimal>();
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        if (!json.TryGetProperty("conversion_rates", out var ratesElement))
        {
            _logger.LogWarning("Invalid response format");
            return new Dictionary<string, decimal>();
        }

        return ratesElement
            .EnumerateObject()
            .ToDictionary(
                x => x.Name.ToUpperInvariant(),
                x => x.Value.GetDecimal());
    }
}
