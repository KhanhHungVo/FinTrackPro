using Hangfire.Dashboard;

namespace FinTrackPro.API.Infrastructure;

public class HangfireRoleFilter(string role) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.IsInRole(role);
    }
}
