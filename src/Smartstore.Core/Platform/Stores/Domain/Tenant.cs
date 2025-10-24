using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Common;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Stores
{
    internal class TenantMap : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(x => x.Domain)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(x => x.DatabaseSchema)
                .HasMaxLength(50);
                
            builder.Property(x => x.CreatedOnUtc)
                .IsRequired();
                
            builder.HasIndex(x => x.Domain)
                .IsUnique()
                .HasDatabaseName("IX_Tenant_Domain");
                
            builder.HasIndex(x => x.IsActive)
                .HasDatabaseName("IX_Tenant_IsActive");
        }
    }

    /// <summary>
    /// Represents a tenant in a multi-tenant environment
    /// </summary>
    [CacheableEntity]
    public partial class Tenant : BaseEntity, IAuditable
    {
        /// <summary>
        /// Gets or sets the tenant name
        /// </summary>
        [Required, StringLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the tenant domain (hostname)
        /// </summary>
        [Required, StringLength(255)]
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the database schema name for this tenant (optional for schema-per-tenant isolation)
        /// </summary>
        [StringLength(50)]
        public string DatabaseSchema { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tenant is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the date and time when the tenant was created
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the tenant was last updated
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }

        private ICollection<Store> _stores;
        /// <summary>
        /// Gets or sets the stores associated with this tenant
        /// </summary>
        public ICollection<Store> Stores
        {
            get => _stores ?? LazyLoader.Load(this, ref _stores) ?? (_stores = new HashSet<Store>());
            protected set => _stores = value;
        }

        private ICollection<TenantSettings> _settings;
        /// <summary>
        /// Gets or sets the tenant-specific settings
        /// </summary>
        public ICollection<TenantSettings> Settings
        {
            get => _settings ?? LazyLoader.Load(this, ref _settings) ?? (_settings = new HashSet<TenantSettings>());
            protected set => _settings = value;
        }

        private ICollection<TenantUser> _tenantUsers;
        /// <summary>
        /// Gets or sets the users that have access to this tenant
        /// </summary>
        public ICollection<TenantUser> TenantUsers
        {
            get => _tenantUsers ?? LazyLoader.Load(this, ref _tenantUsers) ?? (_tenantUsers = new HashSet<TenantUser>());
            protected set => _tenantUsers = value;
        }
    }
}