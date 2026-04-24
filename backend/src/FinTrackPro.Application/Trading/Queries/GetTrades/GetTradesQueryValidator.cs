using FluentValidation;

namespace FinTrackPro.Application.Trading.Queries.GetTrades;

public class GetTradesQueryValidator : AbstractValidator<GetTradesQuery>
{
    private static readonly HashSet<string> AllowedSortBy =
        ["date", "pnl", "symbol", "entryprice", "size", "fees"];

    private static readonly HashSet<string> AllowedSortDir =
        ["asc", "desc"];

    private static readonly HashSet<string> AllowedStatus =
        ["open", "closed"];

    private static readonly HashSet<string> AllowedDirection =
        ["long", "short"];

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

        RuleFor(v => v.SortDir)
            .Must(s => AllowedSortDir.Contains(s.ToLower()))
            .WithMessage($"SortDir must be one of: {string.Join(", ", AllowedSortDir)}.")
            .When(v => !string.IsNullOrWhiteSpace(v.SortDir));

        RuleFor(v => v.Status)
            .Must(s => AllowedStatus.Contains(s!.ToLower()))
            .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatus)}.")
            .When(v => !string.IsNullOrWhiteSpace(v.Status));

        RuleFor(v => v.Direction)
            .Must(s => AllowedDirection.Contains(s!.ToLower()))
            .WithMessage($"Direction must be one of: {string.Join(", ", AllowedDirection)}.")
            .When(v => !string.IsNullOrWhiteSpace(v.Direction));

        RuleFor(v => v.Search)
            .MaximumLength(200).WithMessage("Search must not exceed 200 characters.")
            .When(v => !string.IsNullOrWhiteSpace(v.Search));

        RuleFor(v => v)
            .Must(v => v.DateFrom == null || v.DateTo == null || v.DateFrom <= v.DateTo)
            .WithMessage("DateFrom must be less than or equal to DateTo.")
            .When(v => v.DateFrom.HasValue && v.DateTo.HasValue);
    }
}
