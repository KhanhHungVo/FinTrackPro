using System.Net;
using FinTrackPro.Infrastructure.ExternalServices;
using FinTrackPro.Infrastructure.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinTrackPro.Infrastructure.UnitTests.ExternalServices;

public class FearGreedServiceTests
{
    private static FearGreedService BuildService(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(status, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.alternative.me") };
        return new FearGreedService(httpClient, HybridCacheFactory.Create(), NullLogger<FearGreedService>.Instance);
    }

    // Unix timestamp for 2024-01-15 12:00:00 UTC
    private const string ValidResponse = """
        {
          "data": [
            {
              "value": "72",
              "value_classification": "Greed",
              "timestamp": "1705320000"
            }
          ]
        }
        """;

    [Fact]
    public async Task GetLatestAsync_ValidResponse_ReturnsMappedDto()
    {
        var service = BuildService(ValidResponse);

        var result = await service.GetLatestAsync();

        result.Should().NotBeNull();
        result!.Value.Should().Be(72);
        result.Label.Should().Be("Greed");
        result.Timestamp.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1705320000).UtcDateTime);
    }

    [Fact]
    public async Task GetLatestAsync_HttpError_ReturnsNull()
    {
        var service = BuildService("{}", HttpStatusCode.InternalServerError);

        var result = await service.GetLatestAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestAsync_MalformedJson_ReturnsNull()
    {
        var service = BuildService("not-valid-json");

        var result = await service.GetLatestAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestAsync_EmptyDataArray_ReturnsNull()
    {
        const string json = """{ "data": [] }""";
        var service = BuildService(json);

        var result = await service.GetLatestAsync();

        result.Should().BeNull();
    }
}
