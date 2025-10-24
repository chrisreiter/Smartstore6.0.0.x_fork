using Smartstore.Core.Stores;

namespace Smartstore.Core.Stores.Services
{
    /// <summary>
    /// Tenant service interface for multi-tenant operations
    /// </summary>
    public interface ITenantService
    {
        /// <summary>
        /// Gets the current tenant for the request
        /// </summary>
        /// <returns>The current tenant or null if none found</returns>
        Task<Tenant> GetCurrentTenantAsync();

        /// <summary>
        /// Gets the current tenant ID
        /// </summary>
        /// <returns>The current tenant ID or null if none found</returns>
        Task<int?> GetCurrentTenantIdAsync();

        /// <summary>
        /// Resolves a tenant by hostname/domain
        /// </summary>
        /// <param name="host">The hostname to resolve</param>
        /// <returns>The tenant for the given hostname or null if not found</returns>
        Task<Tenant> ResolveTenantAsync(string host);

        /// <summary>
        /// Gets all active tenants
        /// </summary>
        /// <returns>List of active tenants</returns>
        Task<IList<Tenant>> GetAllActiveTenantsAsync();

        /// <summary>
        /// Gets a tenant by ID
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <returns>The tenant or null if not found</returns>
        Task<Tenant> GetTenantByIdAsync(int tenantId);

        /// <summary>
        /// Creates a new tenant
        /// </summary>
        /// <param name="tenant">The tenant to create</param>
        /// <returns>The created tenant</returns>
        Task<Tenant> CreateTenantAsync(Tenant tenant);

        /// <summary>
        /// Updates an existing tenant
        /// </summary>
        /// <param name="tenant">The tenant to update</param>
        /// <returns>Task</returns>
        Task UpdateTenantAsync(Tenant tenant);

        /// <summary>
        /// Deletes a tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID to delete</param>
        /// <returns>Task</returns>
        Task DeleteTenantAsync(int tenantId);

        /// <summary>
        /// Checks if a domain is available for a tenant
        /// </summary>
        /// <param name="domain">The domain to check</param>
        /// <param name="excludeTenantId">Optional tenant ID to exclude from check</param>
        /// <returns>True if domain is available, false otherwise</returns>
        Task<bool> IsDomainAvailableAsync(string domain, int? excludeTenantId = null);

        /// <summary>
        /// Gets stores for a specific tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <returns>List of stores for the tenant</returns>
        Task<IList<Store>> GetStoresByTenantAsync(int tenantId);
    }
}