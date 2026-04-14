using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
    private Guid _expenseCategoryId;

    public TransactionsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();

        var token = AuthTokenFactory.GenerateToken("test-keycloak-id", "User");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();

        // Fetch the first expense system category so all tests have a real CategoryId
        var catResponse = await _client.GetAsync("/api/transaction-categories?type=Expense");
        catResponse.EnsureSuccessStatusCode();
        var categories = await catResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        _expenseCategoryId = Guid.Parse(categories![0].GetProperty("id").GetString()!);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateTransaction_ValidRequest_Returns201()
    {
        var request = TransactionRequestBuilder.Build(categoryId: _expenseCategoryId);

        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTransaction_ZeroAmount_Returns400()
    {
        var request = TransactionRequestBuilder.Build(amount: 0m, categoryId: _expenseCategoryId);

        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransactions_ReturnsCreatedTransactions()
    {
        await _client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build(categoryId: _expenseCategoryId));
        await _client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build(categoryId: _expenseCategoryId));

        var response = await _client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();
        paged!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetTransactions_WithMonthFilter_ReturnsOnlyMatchingMonth()
    {
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var otherMonth = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM");

        await _client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build(budgetMonth: currentMonth, categoryId: _expenseCategoryId));
        await _client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build(budgetMonth: otherMonth, categoryId: _expenseCategoryId));

        var response = await _client.GetAsync($"/api/transactions?month={currentMonth}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();
        paged!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteTransaction_ExistingId_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", TransactionRequestBuilder.Build(categoryId: _expenseCategoryId));
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
        var request = TransactionRequestBuilder.Build(categoryId: _expenseCategoryId);

        var response = await unauthClient.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTransaction_InvalidEnumType_Returns400()
    {
        var json = $$$"""{"type":"InvalidType","amount":100,"categoryId":"{{{_expenseCategoryId}}}","note":null,"budgetMonth":"2026-03","currency":"USD"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/transactions", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_InvalidBudgetMonthFormat_Returns400()
    {
        var request = TransactionRequestBuilder.Build(budgetMonth: "26-3", categoryId: _expenseCategoryId);

        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTransaction_ValidRequest_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/transactions",
            TransactionRequestBuilder.Build(categoryId: _expenseCategoryId));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var update = new
        {
            type = "Expense",
            amount = 999m,
            currency = "USD",
            category = "food_beverage",
            note = "updated",
            categoryId = _expenseCategoryId
        };

        var response = await _client.PatchAsJsonAsync($"/api/transactions/{id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateTransaction_ChangingCurrency_RefreshesPersistedRateToUsd()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/transactions",
            TransactionRequestBuilder.Build(amount: 75_000m, currency: "VND", categoryId: _expenseCategoryId));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var update = new
        {
            type = "Expense",
            amount = 75_000m,
            currency = "USD",
            category = "food_beverage",
            note = "updated",
            categoryId = _expenseCategoryId
        };

        var updateResponse = await _client.PatchAsJsonAsync($"/api/transactions/{id}", update);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync("/api/transactions");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var paged = await getResponse.Content.ReadFromJsonAsync<PagedResponse<JsonElement>>();
        var updated = paged!.Items.Single(item => item.GetProperty("id").GetGuid() == id);

        updated.GetProperty("currency").GetString().Should().Be("USD");
        updated.GetProperty("amount").GetDecimal().Should().Be(75_000m);
        updated.GetProperty("rateToUsd").GetDecimal().Should().Be(1m);
    }

    [Fact]
    public async Task UpdateTransaction_NonExistentId_Returns404()
    {
        var update = new
        {
            type = "Expense",
            amount = 100m,
            currency = "USD",
            category = "food_beverage",
            note = (string?)null,
            categoryId = _expenseCategoryId
        };

        var response = await _client.PatchAsJsonAsync($"/api/transactions/{Guid.NewGuid()}", update);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTransaction_InvalidAmount_Returns400()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/transactions",
            TransactionRequestBuilder.Build(categoryId: _expenseCategoryId));
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var update = new
        {
            type = "Expense",
            amount = 0m,
            currency = "USD",
            category = "food_beverage",
            note = (string?)null,
            categoryId = _expenseCategoryId
        };

        var response = await _client.PatchAsJsonAsync($"/api/transactions/{id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
