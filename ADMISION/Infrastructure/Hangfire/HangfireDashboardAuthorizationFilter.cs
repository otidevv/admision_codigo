using ADMISION.ENTITIES.Constants;
using Hangfire.Dashboard;

namespace ADMISION.Infrastructure.Hangfire
{
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            var user = httpContext.User;

            if (user?.Identity?.IsAuthenticated != true) return false;

            return user.IsInRole(AppConstants.Roles.SuperAdmin);
        }
    }
}
