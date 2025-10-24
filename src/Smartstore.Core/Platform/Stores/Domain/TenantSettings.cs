using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Stores
{
    internal class TenantSettingsMap : IEntityTypeConfiguration<TenantSettings>
    {
        public void Configure(EntityTypeBuilder<TenantSettings> builder)
        {
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(x => x.Value)
                .HasMaxLength(2000);
            
            builder.HasOne(x => x.Tenant)
                .WithMany(x => x.Settings)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasIndex(x => new { x.TenantId, x.Name })
                .IsUnique()
                .HasDatabaseName("IX_TenantSettings_Tenant_Name");
        }
    }

    /// <summary>
    /// Represents tenant-specific settings
    /// </summary>
    [CacheableEntity]
    public partial class TenantSettings : BaseEntity
    {
        /// <summary>
        /// Gets or sets the tenant identifier
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// Gets or sets the setting name
        /// </summary>
        [Required, StringLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the setting value
        /// </summary>
        [StringLength(2000)]
        public string Value { get; set; }

        private Tenant _tenant;
        /// <summary>
        /// Gets or sets the tenant
        /// </summary>
        public Tenant Tenant
        {
            get => _tenant ?? LazyLoader.Load(this, ref _tenant);
            set => _tenant = value;
        }
    }
}