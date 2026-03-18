using System.Security.Claims;
using FinTrackPro.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FinTrackPro.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? ExternalUserId => User?.FindFirstValue("sub");
    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
                         ?? User?.FindFirstValue("email");
    public string? DisplayName => User?.FindFirstValue("name")
                               ?? User?.FindFirstValue(ClaimTypes.Name);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public string ProviderName => _configuration["IdentityProvider:Provider"] ?? "keycloak";
}
