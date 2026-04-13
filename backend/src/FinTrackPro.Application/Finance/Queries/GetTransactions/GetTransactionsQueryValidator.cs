using FluentValidation;

namespace FinTrackPro.Application.Finance.Queries.GetTransactions;

public class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
{
    private static readonly HashSet<string> AllowedSortBy =
        ["date", "amount", "category"];

    public GetTransactionsQueryValidator()
    {
        RuleFor(v => v.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(v => v.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(v => v.SortBy)
            .Must(s => AllowedSortBy.Contains(s.ToLower()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortBy)}.");

        RuleFor(v => v.Month)
            .Matches(@"^\d{4}-\d{2}$")
            .WithMessage("Month must be in YYYY-MM format.")
            .When(v => !string.IsNullOrWhiteSpace(v.Month));
    }
}
