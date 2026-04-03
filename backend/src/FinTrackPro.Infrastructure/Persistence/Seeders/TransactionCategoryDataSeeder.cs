using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Seeders;

public class TransactionCategoryDataSeeder(ApplicationDbContext context) : IDataSeeder
{
    private static readonly IReadOnlyList<(TransactionType Type, string Slug, string LabelEn, string LabelVi, string Icon, int Order)> SystemCategories =
    [
        (TransactionType.Income, "salary",       "Salary",       "Lương",               "💰", 1),
        (TransactionType.Income, "bonus",        "Bonus",        "Thưởng",              "🎁", 2),
        (TransactionType.Income, "investment",   "Investment",   "Đầu tư",              "📈", 3),
        (TransactionType.Income, "freelance",    "Freelance",    "Công việc tự do",     "💻", 4),
        (TransactionType.Income, "other_income", "Other Income", "Thu nhập khác",       "➕", 5),

        (TransactionType.Expense, "food_beverage",  "Food & Beverage",   "Ăn uống",           "🍜",  1),
        (TransactionType.Expense, "transportation", "Transportation",    "Di chuyển",          "🚗",  2),
        (TransactionType.Expense, "rent",           "Rent",              "Thuê nhà",           "🏠",  3),
        (TransactionType.Expense, "utilities",      "Utilities",         "Tiện ích",           "💡",  4),
        (TransactionType.Expense, "shopping",       "Shopping",          "Mua sắm",            "🛍️", 5),
        (TransactionType.Expense, "entertainment",  "Entertainment",     "Giải trí",           "🎬",  6),
        (TransactionType.Expense, "healthcare",     "Healthcare",        "Sức khỏe",           "🏥",  7),
        (TransactionType.Expense, "education",      "Education",         "Giáo dục",           "📚",  8),
        (TransactionType.Expense, "travel",         "Travel",            "Du lịch",            "✈️",  9),
        (TransactionType.Expense, "family_child",   "Family & Children", "Gia đình & trẻ em",  "👨‍👩‍👧", 10),
        (TransactionType.Expense, "other_expense",  "Other Expense",     "Chi tiêu khác",      "📦", 11),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingSlugs = await context.TransactionCategories
            .Where(c => c.IsSystem)
            .Select(c => c.Slug)
            .ToHashSetAsync(cancellationToken);

        var toAdd = SystemCategories
            .Where(s => !existingSlugs.Contains(s.Slug))
            .Select(s => TransactionCategory.Create(
                userId: null,
                type: s.Type,
                slug: s.Slug,
                labelEn: s.LabelEn,
                labelVi: s.LabelVi,
                icon: s.Icon,
                isSystem: true,
                sortOrder: s.Order))
            .ToList();

        if (toAdd.Count == 0)
            return;

        context.TransactionCategories.AddRange(toAdd);
        await context.SaveChangesAsync(cancellationToken);
    }
}
