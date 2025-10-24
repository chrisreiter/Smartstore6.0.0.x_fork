using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Stores.Middleware;
using Smartstore.Core.Stores.Services;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Stores.Bootstrapping
{
    public class StoreStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            // Register tenant services
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<ITenantAccessService, TenantAccessService>();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            // Add tenant resolution middleware early in the pipeline
            builder.Configure(StarterOrdering.EarlyMiddleware, app =>
            {
                app.UseTenantResolution();
            });
        }
    }
}