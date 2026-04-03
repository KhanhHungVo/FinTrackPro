using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Application.TransactionCategories.Queries.GetTransactionCategories;

public record TransactionCategoryDto(
    Guid Id,
    string Slug,
    string LabelEn,
    string LabelVi,
    string Icon,
    TransactionType Type,
    bool IsSystem,
    int SortOrder)
{
    public static explicit operator TransactionCategoryDto(TransactionCategory c) => new(
        c.Id, c.Slug, c.LabelEn, c.LabelVi, c.Icon, c.Type, c.IsSystem, c.SortOrder);
}
