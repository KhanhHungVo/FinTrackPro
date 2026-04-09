namespace FinTrackPro.Domain.Exceptions;

public class PlanLimitExceededException(string feature, string message)
    : DomainException(message)
{
    public string Feature { get; } = feature;
}
