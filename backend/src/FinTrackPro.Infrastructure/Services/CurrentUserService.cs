using System.Security.Claims;
using FinTrackPro.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FinTrackPro.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? KeycloakUserId => User?.FindFirstValue("sub");
    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
                         ?? User?.FindFirstValue("email");
    public string? DisplayName => User?.FindFirstValue("name")
                               ?? User?.FindFirstValue(ClaimTypes.Name);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
