using System.Net;
using FinTrackPro.Infrastructure.ExternalServices.ExchangeRate;
using FinTrackPro.Infrastructure.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FinTrackPro.Infrastructure.UnitTests.ExternalServices;

public class ExchangeRateClientTests
{
    private static ExchangeRateClient BuildClient(
        string json,
        HttpStatusCode status = HttpStatusCode.OK,
        string apiKey = "test-key")
    {
        var handler = new MockHttpMessageHandler(status, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://v6.exchangerate-api.com/v6/") };
        var options = Options.Create(new ExchangeRateOptions { ApiKey = apiKey });
        return new ExchangeRateClient(httpClient, options, NullLogger<ExchangeRateClient>.Instance);
    }

    private const string ValidResponse = """
        {
          "result": "success",
          "conversion_rates": {
            "USD": 1.0,
            "EUR": 0.92,
            "GBP": 0.79,
            "VND": 25100.0
          }
        }
        """;

    [Fact]
    public async Task GetLatestRatesAsync_ValidResponse_ReturnsParsedRates()
    {
        var client = BuildClient(ValidResponse);

        var result = await client.GetLatestRatesAsync(CancellationToken.None);

        result.Should().NotBeEmpty();
        result["USD"].Should().Be(1.0m);
        result["EUR"].Should().Be(0.92m);
        result["GBP"].Should().Be(0.79m);
        result["VND"].Should().Be(25100.0m);
    }

    [Fact]
    public async Task GetLatestRatesAsync_NoApiKeyConfigured_ReturnsEmptyDictionary()
    {
        var client = BuildClient(ValidResponse, apiKey: "");

        var result = await client.GetLatestRatesAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestRatesAsync_HttpError_ReturnsEmptyDictionary()
    {
        var client = BuildClient("{}", HttpStatusCode.Unauthorized);

        var result = await client.GetLatestRatesAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestRatesAsync_MissingConversionRatesProperty_ReturnsEmptyDictionary()
    {
        const string json = """{ "result": "success" }""";
        var client = BuildClient(json);

        var result = await client.GetLatestRatesAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestRatesAsync_CurrencyCodesNormalisedToUpperCase()
    {
        const string json = """
            {
              "result": "success",
              "conversion_rates": { "eur": 0.92 }
            }
            """;
        var client = BuildClient(json);

        var result = await client.GetLatestRatesAsync(CancellationToken.None);

        result.Should().ContainKey("EUR");
        result.Should().NotContainKey("eur");
    }
}
