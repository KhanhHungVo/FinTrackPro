using Bogus;

namespace Tests.Common.Builders;

public static class BudgetRequestBuilder
{
    private static readonly Faker _faker = new();

    private static readonly string[] Categories = ["Food", "Transport", "Utilities", "Entertainment", "Health"];

    public static object Build(string? category = null, decimal? limitAmount = null, string? month = null)
    {
        return new
        {
            category = category ?? _faker.PickRandom(Categories),
            limitAmount = limitAmount ?? _faker.Finance.Amount(100, 5000),
            month = month ?? DateTime.UtcNow.ToString("yyyy-MM"),
        };
    }
}
