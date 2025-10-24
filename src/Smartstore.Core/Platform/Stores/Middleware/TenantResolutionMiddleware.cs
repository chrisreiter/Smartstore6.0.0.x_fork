using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Stores.Services;

namespace Smartstore.Core.Stores.Middleware
{
    /// <summary>
    /// Middleware for resolving the current tenant based on the request hostname
    /// </summary>
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantResolutionMiddleware> _logger;

        public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            try
            {
                // Resolve tenant for the current request
                var tenant = await tenantService.GetCurrentTenantAsync();
                
                if (tenant == null)
                {
                    _logger.LogWarning("No tenant found for host '{Host}'. Request will continue without tenant context.", 
                        context.Request.Host.Host);
                }
                else if (!tenant.IsActive)
                {
                    _logger.LogWarning("Tenant '{TenantName}' is inactive for host '{Host}'", 
                        tenant.Name, context.Request.Host.Host);
                    
                    // Could return 503 Service Unavailable or redirect to maintenance page
                    context.Response.StatusCode = 503;
                    await context.Response.WriteAsync($"Tenant '{tenant.Name}' is currently unavailable.");
                    return;
                }
                else
                {
                    _logger.LogDebug("Resolved tenant '{TenantName}' for request to '{Host}{Path}'", 
                        tenant.Name, context.Request.Host.Host, context.Request.Path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in tenant resolution for host '{Host}'", context.Request.Host.Host);
                // Continue with request even if tenant resolution fails
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension methods for registering tenant resolution middleware
    /// </summary>
    public static class TenantResolutionMiddlewareExtensions
    {
        /// <summary>
        /// Adds tenant resolution middleware to the application pipeline
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantResolutionMiddleware>();
        }
    }
}