namespace FinTrackPro.Application.Common.Interfaces;

/// <summary>
/// Exposes the resolved DB identity of the authenticated user.
/// Populated by UserContextMiddleware before any MediatR handler runs.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
}
