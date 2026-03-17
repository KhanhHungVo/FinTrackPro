using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tests.Common;
using Tests.Common.Builders;

namespace FinTrackPro.Api.IntegrationTests.Features.Finance;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestCollection))]
public class TransactionsTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public TransactionsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();

        var token = AuthTokenFactory.GenerateToken("test-keycloak-id", "User");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task InitializeAsync() => await _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateTransaction_ValidRequest_Returns201()
    {
        var request = TransactionRequestBuilder.Build();

        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTransaction_ZeroAmount_Returns400()
    {
        var request = TransactionRequestBuilder.Build(amount: 0m);

        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransactions_ReturnsCreatedTransactions()
    {
        await _client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build());
        await _client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build());

        var response = await _client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<object>>();
        items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetTransactions_WithMonthFilter_ReturnsOnlyMatchingMonth()
    {
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var otherMonth = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM");

        await _client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build(budgetMonth: currentMonth));
        await _client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build(budgetMonth: otherMonth));

        var response = await _client.GetAsync($"/api/transactions?month={currentMonth}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<object>>();
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteTransaction_ExistingId_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var deleteResponse = await _client.DeleteAsync($"/api/transactions/{id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTransaction_NonExistentId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/transactions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTransaction_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();
        var request = TransactionRequestBuilder.Build();

        var response = await unauthClient.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
