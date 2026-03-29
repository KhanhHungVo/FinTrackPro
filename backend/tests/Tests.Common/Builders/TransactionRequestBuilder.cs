using Bogus;
using FinTrackPro.Domain.Enums;

namespace Tests.Common.Builders;

public static class TransactionRequestBuilder
{
    static TransactionRequestBuilder() => Randomizer.Seed = new Random(42);

    private static readonly string[] Categories = ["Food", "Transport", "Salary", "Utilities", "Entertainment"];

    private static readonly Faker _faker = new();

    public static object Build(
        TransactionType? type = null,
        decimal? amount = null,
        string? category = null,
        string? budgetMonth = null,
        string? currency = null)
    {
        return new
        {
            type = type ?? _faker.PickRandom<TransactionType>(),
            amount = amount ?? _faker.Finance.Amount(1, 1000),
            category = category ?? _faker.PickRandom(Categories),
            note = _faker.Lorem.Sentence(),
            budgetMonth = budgetMonth ?? DateTime.UtcNow.ToString("yyyy-MM"),
            currency = currency ?? "USD"
        };
    }
}
