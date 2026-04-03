using FinTrackPro.Domain.Enums;

namespace Tests.Common.Builders;

public static class TransactionCategoryRequestBuilder
{
    public static object Build(
        string? slug = null,
        string? labelEn = null,
        string? labelVi = null,
        string? icon = null,
        TransactionType? type = null) => new
    {
        slug    = slug    ?? $"custom_{Guid.NewGuid():N}"[..20],
        labelEn = labelEn ?? "Custom Category",
        labelVi = labelVi ?? "Danh mục tùy chỉnh",
        icon    = icon    ?? "📌",
        type    = type    ?? TransactionType.Expense
    };
}
