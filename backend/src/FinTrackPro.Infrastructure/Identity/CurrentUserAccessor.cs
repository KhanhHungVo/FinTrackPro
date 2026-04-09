using System.Security.Claims;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using FinTrackPro.Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace FinTrackPro.Infrastructure.Identity;

/// <summary>
/// Reads the resolved <see cref="CurrentUser"/> from <see cref="HttpContext.Items"/>
/// where <see cref="UserContextMiddleware"/> stored it.
/// </summary>
public class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId =>
        httpContextAccessor.HttpContext?.Items[typeof(ICurrentUser)] is CurrentUser u
            ? u.UserId
            : throw new InvalidOperationException(
                "Current user is not available. Ensure UserContextMiddleware ran before the handler.");

    public bool IsAdmin =>
        httpContextAccessor.HttpContext?.User.IsInRole(UserRole.Admin) ?? false;
}
