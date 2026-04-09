namespace FinTrackPro.Application.Common.Interfaces;

/// <summary>
/// Exposes the resolved DB identity of the authenticated user.
/// Populated by UserContextMiddleware before any MediatR handler runs.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    /// <summary>
    /// True when the authenticated user holds the Admin IAM role.
    /// Admin users bypass all subscription plan limits.
    /// Defaults to false; overridden by <c>CurrentUserAccessor</c> which reads live JWT claims.
    /// </summary>
    bool IsAdmin => false;
}
