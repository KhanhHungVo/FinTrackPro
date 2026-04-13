using FluentValidation;

namespace FinTrackPro.Application.Trading.Queries.GetTrades;

public class GetTradesQueryValidator : AbstractValidator<GetTradesQuery>
{
    private static readonly HashSet<string> AllowedSortBy =
        ["date", "pnl", "symbol", "entryprice", "size", "fees"];

    public GetTradesQueryValidator()
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

        RuleFor(v => v)
            .Must(v => v.DateFrom == null || v.DateTo == null || v.DateFrom <= v.DateTo)
            .WithMessage("DateFrom must be less than or equal to DateTo.")
            .When(v => v.DateFrom.HasValue && v.DateTo.HasValue);
    }
}
