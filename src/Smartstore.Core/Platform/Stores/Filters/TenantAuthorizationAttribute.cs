using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Stores.Services;

namespace Smartstore.Core.Stores.Filters
{
    /// <summary>
    /// Authorization filter to ensure users can only access data from their assigned tenant
    /// </summary>
    public class TenantAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var tenantAccessService = context.HttpContext.RequestServices.GetRequiredService<ITenantAccessService>();
            var tenantService = context.HttpContext.RequestServices.GetRequiredService<ITenantService>();

            // Get current tenant from request
            var currentTenant = await tenantService.GetCurrentTenantAsync();
            
            // Check if user has access to this tenant
            if (currentTenant != null)
            {
                var hasAccess = await tenantAccessService.HasTenantAccessAsync(currentTenant.Id);
                
                if (!hasAccess)
                {
                    // User doesn't have access to this tenant
                    context.Result = new ForbidResult();
                    return;
                }
            }

            // Super admins can access tenant management regardless of tenant context
            var isSuperAdmin = await tenantAccessService.IsSuperAdminAsync();
            if (!isSuperAdmin)
            {
                // Regular tenant admins cannot access tenant management
                var controllerName = context.RouteData.Values["controller"]?.ToString();
                if (string.Equals(controllerName, "Tenant", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Authorization filter for Super Admin only actions (like tenant management)
    /// </summary>
    public class SuperAdminOnlyAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var tenantAccessService = context.HttpContext.RequestServices.GetRequiredService<ITenantAccessService>();
            
            var isSuperAdmin = await tenantAccessService.IsSuperAdminAsync();
            if (!isSuperAdmin)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}