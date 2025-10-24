using Smartstore.Core.Stores;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Store> Stores { get; set; }
        public DbSet<StoreMapping> StoreMappings { get; set; }
        
        // Multi-tenant entities
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TenantSettings> TenantSettings { get; set; }
        public DbSet<TenantUser> TenantUsers { get; set; }
    }
}
