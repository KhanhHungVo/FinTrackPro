using System.Security.Claims;
using FinTrackPro.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FinTrackPro.Infrastructure.Services;

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? ExternalUserId => User?.FindFirstValue("sub");
    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
                         ?? User?.FindFirstValue("email");
    public string? DisplayName => User?.FindFirstValue("name")
                               ?? User?.FindFirstValue(ClaimTypes.Name);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public string ProviderName => configuration["IdentityProvider:Provider"] ?? "keycloak";
}
