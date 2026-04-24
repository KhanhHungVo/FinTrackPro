using FluentValidation;

namespace FinTrackPro.Application.Finance.Queries.GetTransactions;

public class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
{
    private static readonly HashSet<string> AllowedSortBy =
        ["date", "amount", "category"];

    private static readonly HashSet<string> AllowedSortDir =
        ["asc", "desc"];

    private static readonly HashSet<string> AllowedType =
        ["income", "expense"];

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

        RuleFor(v => v.SortDir)
            .Must(s => AllowedSortDir.Contains(s.ToLower()))
            .WithMessage($"SortDir must be one of: {string.Join(", ", AllowedSortDir)}.")
            .When(v => !string.IsNullOrWhiteSpace(v.SortDir));

        RuleFor(v => v.Type)
            .Must(s => AllowedType.Contains(s!.ToLower()))
            .WithMessage($"Type must be one of: {string.Join(", ", AllowedType)}.")
            .When(v => !string.IsNullOrWhiteSpace(v.Type));

        RuleFor(v => v.Search)
            .MaximumLength(200).WithMessage("Search must not exceed 200 characters.")
            .When(v => !string.IsNullOrWhiteSpace(v.Search));

        RuleFor(v => v.Month)
            .MaximumLength(7).WithMessage("Month must not exceed 7 characters.")
            .Matches(@"^\d{4}-\d{2}$")
            .WithMessage("Month must be in YYYY-MM format.")
            .When(v => !string.IsNullOrWhiteSpace(v.Month));
    }
}
