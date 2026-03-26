using FinTrackPro.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FinTrackPro.Infrastructure.Identity;

/// <summary>
/// Resolves and provisions the local AppUser once per HTTP request.
/// Stores the result in HttpContext.Items so ICurrentUser (CurrentUserAccessor) can read it.
/// Skips resolution for unauthenticated requests.
/// </summary>
public class UserContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IIdentityService identityService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await identityService.ResolveAsync(
                context.User, context.RequestAborted);

            context.Items[typeof(ICurrentUser)] = currentUser;
        }

        await next(context);
    }
}
