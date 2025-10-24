using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Stores
{
    internal class TenantUserMap : IEntityTypeConfiguration<TenantUser>
    {
        public void Configure(EntityTypeBuilder<TenantUser> builder)
        {
            builder.HasKey(x => x.Id);
            
            builder.HasOne(x => x.Tenant)
                .WithMany(x => x.TenantUsers)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasIndex(x => new { x.TenantId, x.CustomerId })
                .IsUnique()
                .HasDatabaseName("IX_TenantUser_Tenant_Customer");
                
            builder.HasIndex(x => x.CustomerId)
                .HasDatabaseName("IX_TenantUser_Customer");
        }
    }

    /// <summary>
    /// Represents the relationship between a tenant and a user (admin access)
    /// </summary>
    public partial class TenantUser : BaseEntity
    {
        /// <summary>
        /// Gets or sets the tenant identifier
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets whether this user is a super admin (can manage the tenant itself)
        /// </summary>
        public bool IsSuperAdmin { get; set; } = false;

        /// <summary>
        /// Gets or sets the date when this assignment was created
        /// </summary>
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        private Tenant _tenant;
        /// <summary>
        /// Gets or sets the tenant
        /// </summary>
        public Tenant Tenant
        {
            get => _tenant ?? LazyLoader.Load(this, ref _tenant);
            set => _tenant = value;
        }

        private Customer _customer;
        /// <summary>
        /// Gets or sets the customer
        /// </summary>
        public Customer Customer
        {
            get => _customer ?? LazyLoader.Load(this, ref _customer);
            set => _customer = value;
        }
    }
}