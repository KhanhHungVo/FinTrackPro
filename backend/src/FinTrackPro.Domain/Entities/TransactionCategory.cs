using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;

namespace FinTrackPro.Domain.Entities;

public class TransactionCategory : AuditableEntity
{
    public Guid? UserId { get; private set; }
    public TransactionType Type { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string LabelEn { get; private set; } = string.Empty;
    public string LabelVi { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }


    private TransactionCategory() { }

    public static TransactionCategory Create(
        Guid? userId,
        TransactionType type,
        string slug,
        string labelEn,
        string labelVi,
        string icon,
        bool isSystem = false,
        int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Slug is required.");
        if (string.IsNullOrWhiteSpace(labelEn))
            throw new DomainException("English label is required.");
        if (string.IsNullOrWhiteSpace(labelVi))
            throw new DomainException("Vietnamese label is required.");

        return new TransactionCategory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Slug = slug.Trim().ToLowerInvariant(),
            LabelEn = labelEn.Trim(),
            LabelVi = labelVi.Trim(),
            Icon = icon.Trim(),
            IsSystem = isSystem,
            IsActive = true,
            SortOrder = sortOrder
        };
    }

    public void UpdateLabels(string labelEn, string labelVi, string icon)
    {
        if (IsSystem)
            throw new AuthorizationException("System categories cannot be modified.");

        LabelEn = labelEn.Trim();
        LabelVi = labelVi.Trim();
        Icon = icon.Trim();
    }

    public void SoftDelete()
    {
        if (IsSystem)
            throw new AuthorizationException("System categories cannot be deleted.");

        IsActive = false;
    }
}
