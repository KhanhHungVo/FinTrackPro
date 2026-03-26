using System.Security.Claims;
using FinTrackPro.Application.Common.Models;

namespace FinTrackPro.Application.Common.Interfaces;

public interface IIdentityService
{
    /// <summary>
    /// Resolves or provisions the local AppUser for the authenticated principal.
    /// Called once per HTTP request by UserContextMiddleware.
    /// </summary>
    Task<CurrentUser> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
