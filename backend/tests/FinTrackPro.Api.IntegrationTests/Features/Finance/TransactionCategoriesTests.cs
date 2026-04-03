using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Tests.Common;
using Tests.Common.Builders;

namespace FinTrackPro.Api.IntegrationTests.Features.Finance;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestCollection))]
public class TransactionCategoriesTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public TransactionCategoriesTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();

        var token = AuthTokenFactory.GenerateToken("tc-keycloak-id", "User");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task InitializeAsync() => await _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetTransactionCategories_Authenticated_Returns16SystemCategories()
    {
        var response = await _client.GetAsync("/api/transaction-categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        categories.Should().HaveCount(16);
    }

    [Fact]
    public async Task GetTransactionCategories_WithTypeFilter_ReturnsOnlyExpense()
    {
        var response = await _client.GetAsync("/api/transaction-categories?type=Expense");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        categories.Should().HaveCount(11);
        categories!.Select(c => c.GetProperty("type").GetString()).Should().AllBe("Expense");
    }

    [Fact]
    public async Task GetTransactionCategories_Unauthenticated_Returns401()
    {
        var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/transaction-categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTransactionCategory_ValidRequest_Returns201()
    {
        var request = TransactionCategoryRequestBuilder.Build();

        var response = await _client.PostAsJsonAsync("/api/transaction-categories", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTransactionCategory_DuplicateSlug_Returns409()
    {
        var slug = $"dup_{Guid.NewGuid():N}"[..20];
        var request = TransactionCategoryRequestBuilder.Build(slug: slug);

        await _client.PostAsJsonAsync("/api/transaction-categories", request);
        var response = await _client.PostAsJsonAsync("/api/transaction-categories", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateTransactionCategory_InvalidSlug_Returns400()
    {
        var request = TransactionCategoryRequestBuilder.Build(slug: "My Category");

        var response = await _client.PostAsJsonAsync("/api/transaction-categories", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTransactionCategory_OwnCustomCategory_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/transaction-categories",
            TransactionCategoryRequestBuilder.Build());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateResponse = await _client.PatchAsJsonAsync($"/api/transaction-categories/{id}",
            new { labelEn = "Updated", labelVi = "Đã cập nhật", icon = "🔄" });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateTransactionCategory_SystemCategory_Returns403()
    {
        var catResponse = await _client.GetAsync("/api/transaction-categories?type=Expense");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        var systemId = Guid.Parse(categories![0].GetProperty("id").GetString()!);

        var updateResponse = await _client.PatchAsJsonAsync($"/api/transaction-categories/{systemId}",
            new { labelEn = "Hacked", labelVi = "Bị hack", icon = "💀" });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTransactionCategory_OwnCustomCategory_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/transaction-categories",
            TransactionCategoryRequestBuilder.Build());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var deleteResponse = await _client.DeleteAsync($"/api/transaction-categories/{id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it no longer appears in GET
        var getResponse = await _client.GetAsync("/api/transaction-categories");
        var categories = await getResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        categories.Should().NotContain(c => c.GetProperty("id").GetString() == id.ToString());
    }

    [Fact]
    public async Task DeleteTransactionCategory_SystemCategory_Returns403()
    {
        var catResponse = await _client.GetAsync("/api/transaction-categories?type=Expense");
        var categories = await catResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        var systemId = Guid.Parse(categories![0].GetProperty("id").GetString()!);

        var response = await _client.DeleteAsync($"/api/transaction-categories/{systemId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
