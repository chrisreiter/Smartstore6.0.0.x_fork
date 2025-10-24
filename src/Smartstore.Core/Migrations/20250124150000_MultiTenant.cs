using System.Data;
using FluentMigrator;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2025-01-24 15:00:00", "MultiTenant")]
    internal class MultiTenant : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        public override void Up()
        {
            // Create Tenant table
            if (!Schema.Table("Tenant").Exists())
            {
                Create.Table("Tenant")
                    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                    .WithColumn("Name").AsString(255).NotNullable()
                    .WithColumn("Domain").AsString(255).NotNullable()
                    .WithColumn("DatabaseSchema").AsString(50).Nullable()
                    .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("CreatedOnUtc").AsDateTime2().NotNullable()
                    .WithColumn("UpdatedOnUtc").AsDateTime2().NotNullable();

                // Create indexes for Tenant table
                Create.Index("IX_Tenant_Domain")
                    .OnTable("Tenant")
                    .OnColumn("Domain").Ascending()
                    .WithOptions().Unique();

                Create.Index("IX_Tenant_IsActive")
                    .OnTable("Tenant")
                    .OnColumn("IsActive").Ascending();
            }

            // Create TenantSettings table
            if (!Schema.Table("TenantSettings").Exists())
            {
                Create.Table("TenantSettings")
                    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                    .WithColumn("TenantId").AsInt32().NotNullable()
                    .WithColumn("Name").AsString(200).NotNullable()
                    .WithColumn("Value").AsString(2000).Nullable();

                // Create foreign key constraint
                Create.ForeignKey("FK_TenantSettings_Tenant_TenantId")
                    .FromTable("TenantSettings").ForeignColumn("TenantId")
                    .ToTable("Tenant").PrimaryColumn("Id")
                    .OnDelete(Rule.Cascade);

                // Create unique index for tenant settings
                Create.Index("IX_TenantSettings_Tenant_Name")
                    .OnTable("TenantSettings")
                    .OnColumn("TenantId").Ascending()
                    .OnColumn("Name").Ascending()
                    .WithOptions().Unique();
            }

            // Create TenantUser table for tenant-user assignments
            if (!Schema.Table("TenantUser").Exists())
            {
                Create.Table("TenantUser")
                    .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                    .WithColumn("TenantId").AsInt32().NotNullable()
                    .WithColumn("CustomerId").AsInt32().NotNullable()
                    .WithColumn("IsSuperAdmin").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("CreatedOnUtc").AsDateTime2().NotNullable();

                // Create foreign key constraints
                Create.ForeignKey("FK_TenantUser_Tenant_TenantId")
                    .FromTable("TenantUser").ForeignColumn("TenantId")
                    .ToTable("Tenant").PrimaryColumn("Id")
                    .OnDelete(Rule.Cascade);

                Create.ForeignKey("FK_TenantUser_Customer_CustomerId")
                    .FromTable("TenantUser").ForeignColumn("CustomerId")
                    .ToTable("Customer").PrimaryColumn("Id")
                    .OnDelete(Rule.Cascade);

                // Create indexes
                Create.Index("IX_TenantUser_Tenant_Customer")
                    .OnTable("TenantUser")
                    .OnColumn("TenantId").Ascending()
                    .OnColumn("CustomerId").Ascending()
                    .WithOptions().Unique();

                Create.Index("IX_TenantUser_Customer")
                    .OnTable("TenantUser")
                    .OnColumn("CustomerId").Ascending();
            }

            // Extend Store table with tenant columns
            if (!Schema.Table("Store").Column("TenantId").Exists())
            {
                Alter.Table("Store")
                    .AddColumn("TenantId").AsInt32().Nullable();

                // Create foreign key constraint for Store -> Tenant
                Create.ForeignKey("FK_Store_Tenant_TenantId")
                    .FromTable("Store").ForeignColumn("TenantId")
                    .ToTable("Tenant").PrimaryColumn("Id")
                    .OnDelete(Rule.Restrict);

                // Create index on TenantId
                Create.Index("IX_Store_TenantId")
                    .OnTable("Store")
                    .OnColumn("TenantId").Ascending();
            }

            if (!Schema.Table("Store").Column("IsSharedResource").Exists())
            {
                Alter.Table("Store")
                    .AddColumn("IsSharedResource").AsBoolean().NotNullable().WithDefaultValue(false);
            }
        }

        public override void Down()
        {
            // Remove foreign key constraints first
            if (Schema.Table("Store").Constraint("FK_Store_Tenant_TenantId").Exists())
            {
                Delete.ForeignKey("FK_Store_Tenant_TenantId").OnTable("Store");
            }

            if (Schema.Table("TenantSettings").Constraint("FK_TenantSettings_Tenant_TenantId").Exists())
            {
                Delete.ForeignKey("FK_TenantSettings_Tenant_TenantId").OnTable("TenantSettings");
            }

            // Remove indexes
            if (Schema.Table("Store").Index("IX_Store_TenantId").Exists())
            {
                Delete.Index("IX_Store_TenantId").OnTable("Store");
            }

            if (Schema.Table("TenantSettings").Index("IX_TenantSettings_Tenant_Name").Exists())
            {
                Delete.Index("IX_TenantSettings_Tenant_Name").OnTable("TenantSettings");
            }

            if (Schema.Table("Tenant").Index("IX_Tenant_Domain").Exists())
            {
                Delete.Index("IX_Tenant_Domain").OnTable("Tenant");
            }

            if (Schema.Table("Tenant").Index("IX_Tenant_IsActive").Exists())
            {
                Delete.Index("IX_Tenant_IsActive").OnTable("Tenant");
            }

            // Remove columns from Store table
            if (Schema.Table("Store").Column("TenantId").Exists())
            {
                Delete.Column("TenantId").FromTable("Store");
            }

            if (Schema.Table("Store").Column("IsSharedResource").Exists())
            {
                Delete.Column("IsSharedResource").FromTable("Store");
            }

            // Drop tables
            if (Schema.Table("TenantSettings").Exists())
            {
                Delete.Table("TenantSettings");
            }

            if (Schema.Table("Tenant").Exists())
            {
                Delete.Table("Tenant");
            }
        }

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await SeedDefaultTenantAsync(context, cancelToken);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            // Admin resources for tenant management
            builder.AddOrUpdate("Admin.System.Tenants", "Tenants", "Mandanten");
            builder.AddOrUpdate("Admin.System.Tenants.List", "Tenant list", "Mandantenliste");
            builder.AddOrUpdate("Admin.System.Tenants.AddNew", "Add new tenant", "Neuen Mandanten hinzufügen");
            builder.AddOrUpdate("Admin.System.Tenants.EditDetails", "Edit tenant details", "Mandantendetails bearbeiten");
            
            builder.AddOrUpdate("Admin.System.Tenants.Fields.Name", "Name", "Name");
            builder.AddOrUpdate("Admin.System.Tenants.Fields.Name.Hint", "The display name of the tenant.", "Der Anzeigename des Mandanten.");
            builder.AddOrUpdate("Admin.System.Tenants.Fields.Domain", "Domain", "Domain");
            builder.AddOrUpdate("Admin.System.Tenants.Fields.Domain.Hint", "The primary domain/hostname for this tenant.", "Die primäre Domain/Hostname für diesen Mandanten.");
            builder.AddOrUpdate("Admin.System.Tenants.Fields.DatabaseSchema", "Database schema", "Datenbankschema");
            builder.AddOrUpdate("Admin.System.Tenants.Fields.DatabaseSchema.Hint", "Optional database schema for tenant isolation.", "Optionales Datenbankschema für Mandantentrennung.");
            builder.AddOrUpdate("Admin.System.Tenants.Fields.IsActive", "Active", "Aktiv");
            builder.AddOrUpdate("Admin.System.Tenants.Fields.IsActive.Hint", "Determines whether the tenant is active.", "Bestimmt, ob der Mandant aktiv ist.");
            builder.AddOrUpdate("Admin.System.Tenants.Fields.CreatedOn", "Created on", "Erstellt am");
            builder.AddOrUpdate("Admin.System.Tenants.Fields.UpdatedOn", "Updated on", "Aktualisiert am");

            // Store multi-tenant resources
            builder.AddOrUpdate("Admin.Configuration.Stores.Fields.TenantId", "Tenant", "Mandant");
            builder.AddOrUpdate("Admin.Configuration.Stores.Fields.TenantId.Hint", "The tenant this store belongs to.", "Der Mandant, zu dem dieser Shop gehört.");
            builder.AddOrUpdate("Admin.Configuration.Stores.Fields.IsSharedResource", "Shared resource", "Geteilte Ressource");
            builder.AddOrUpdate("Admin.Configuration.Stores.Fields.IsSharedResource.Hint", "Indicates whether this store is a shared resource across tenants.", "Gibt an, ob dieser Shop eine geteilte Ressource zwischen Mandanten ist.");

            // General multi-tenant messages
            builder.AddOrUpdate("MultiTenant.TenantNotFound", "Tenant not found for domain '{0}'.", "Mandant für Domain '{0}' nicht gefunden.");
            builder.AddOrUpdate("MultiTenant.TenantInactive", "Tenant '{0}' is currently inactive.", "Mandant '{0}' ist derzeit inaktiv.");
            builder.AddOrUpdate("MultiTenant.DomainAlreadyExists", "A tenant with domain '{0}' already exists.", "Ein Mandant mit der Domain '{0}' existiert bereits.");
            
            // Additional tenant messages
            builder.AddOrUpdate("Admin.System.Tenants.Added", "Tenant has been created successfully.", "Mandant wurde erfolgreich erstellt.");
            builder.AddOrUpdate("Admin.System.Tenants.Updated", "Tenant has been updated successfully.", "Mandant wurde erfolgreich aktualisiert.");
            builder.AddOrUpdate("Admin.System.Tenants.Deleted", "Tenant has been deleted successfully.", "Mandant wurde erfolgreich gelöscht.");
            builder.AddOrUpdate("Admin.System.Tenants.NotFound", "Tenant not found.", "Mandant nicht gefunden.");
            builder.AddOrUpdate("Admin.System.Tenants.CannotDeleteWithStores", "Cannot delete tenant '{0}' because it has {1} associated store(s). Please reassign or delete the stores first.", "Mandant '{0}' kann nicht gelöscht werden, da er {1} zugeordnete Shop(s) hat. Bitte weisen Sie die Shops neu zu oder löschen Sie sie zuerst.");
            builder.AddOrUpdate("Admin.System.Tenants.Fields.Domain.Required", "Domain is required.", "Domain ist erforderlich.");
        }

        private async Task SeedDefaultTenantAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            // Create a default tenant if none exists
            var tenantExists = await context.Set<Smartstore.Core.Stores.Tenant>().AnyAsync(cancelToken);
            if (!tenantExists)
            {
                var defaultTenant = new Smartstore.Core.Stores.Tenant
                {
                    Name = "Default Tenant",
                    Domain = "localhost",
                    IsActive = true,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow
                };

                context.Set<Smartstore.Core.Stores.Tenant>().Add(defaultTenant);
                await context.SaveChangesAsync(cancelToken);

                // Update existing stores to belong to the default tenant
                var stores = await context.Stores.ToListAsync(cancelToken);
                foreach (var store in stores)
                {
                    store.TenantId = defaultTenant.Id;
                }

                await context.SaveChangesAsync(cancelToken);
            }
        }
    }
}