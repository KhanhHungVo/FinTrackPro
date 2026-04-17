namespace FinTrackPro.Domain.Common;

public abstract class CreatedEntity : BaseEntity
{
    public DateTime CreatedAt { get; internal set; }
}
