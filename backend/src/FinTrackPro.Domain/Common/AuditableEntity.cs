namespace FinTrackPro.Domain.Common;

public abstract class AuditableEntity : CreatedEntity
{
    public DateTime UpdatedAt { get; internal set; }
}
