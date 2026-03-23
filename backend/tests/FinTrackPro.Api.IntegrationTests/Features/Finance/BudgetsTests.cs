using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tests.Common;
using Tests.Common.Builders;

namespace FinTrackPro.Api.IntegrationTests.Features.Finance;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestCollection))]
public class BudgetsTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public BudgetsTests(DatabaseFixture fixture)
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
    public async Task CreateBudget_ValidRequest_Returns201()
    {
        var request = BudgetRequestBuilder.Build();

        var response = await _client.PostAsJsonAsync("/api/budgets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateBudget_ZeroLimit_Returns400()
    {
        var request = BudgetRequestBuilder.Build(limitAmount: 0m);

        var response = await _client.PostAsJsonAsync("/api/budgets", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBudgets_ByMonth_ReturnsCreatedBudgets()
    {
        var month = DateTime.UtcNow.ToString("yyyy-MM");
        await _client.PostAsJsonAsync("/api/budgets", BudgetRequestBuilder.Build(month: month));

        var response = await _client.GetAsync($"/api/budgets/{month}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<object>>();
        items.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GetBudgets_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();
        var month = DateTime.UtcNow.ToString("yyyy-MM");

        var response = await unauthClient.GetAsync($"/api/budgets/{month}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBudgets_InvalidMonthFormat_Returns400()
    {
        var response = await _client.GetAsync("/api/budgets/not-a-month");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBudget_ValidRequest_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/budgets", BudgetRequestBuilder.Build());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PatchAsJsonAsync($"/api/budgets/{id}", new { limitAmount = 999m });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateBudget_ZeroLimit_Returns400()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/budgets", BudgetRequestBuilder.Build());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PatchAsJsonAsync($"/api/budgets/{id}", new { limitAmount = 0m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBudget_NotFound_Returns404()
    {
        var response = await _client.PatchAsJsonAsync($"/api/budgets/{Guid.NewGuid()}", new { limitAmount = 500m });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBudget_OwnedBudget_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/budgets", BudgetRequestBuilder.Build());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.DeleteAsync($"/api/budgets/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteBudget_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/budgets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
