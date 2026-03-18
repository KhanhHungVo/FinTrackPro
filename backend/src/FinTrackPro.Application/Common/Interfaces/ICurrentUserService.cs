namespace FinTrackPro.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? ExternalUserId { get; }
    string ProviderName { get; }
    string? Email { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
}
