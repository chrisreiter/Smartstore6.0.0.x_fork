using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Stores.Services
{
    /// <summary>
    /// Service for managing tenant access control and permissions
    /// </summary>
    public interface ITenantAccessService
    {
        /// <summary>
        /// Gets the tenant that the current user has access to
        /// </summary>
        /// <returns>The tenant or null if no access</returns>
        Task<Tenant> GetUserTenantAsync();

        /// <summary>
        /// Gets the tenant that the specified user has access to
        /// </summary>
        /// <param name="customer">The customer/user</param>
        /// <returns>The tenant or null if no access</returns>
        Task<Tenant> GetUserTenantAsync(Customer customer);

        /// <summary>
        /// Checks if the current user has access to the specified tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID to check</param>
        /// <returns>True if user has access, false otherwise</returns>
        Task<bool> HasTenantAccessAsync(int tenantId);

        /// <summary>
        /// Checks if the specified user has access to the specified tenant
        /// </summary>
        /// <param name="customer">The customer/user</param>
        /// <param name="tenantId">The tenant ID to check</param>
        /// <returns>True if user has access, false otherwise</returns>
        Task<bool> HasTenantAccessAsync(Customer customer, int tenantId);

        /// <summary>
        /// Assigns a user to a tenant (makes them a tenant admin)
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <returns>Task</returns>
        Task AssignUserToTenantAsync(int customerId, int tenantId);

        /// <summary>
        /// Removes a user from a tenant
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <returns>Task</returns>
        Task RemoveUserFromTenantAsync(int customerId, int tenantId);

        /// <summary>
        /// Gets all users that have access to a specific tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <returns>List of customers</returns>
        Task<IList<Customer>> GetTenantUsersAsync(int tenantId);

        /// <summary>
        /// Checks if the current user is a super admin (has access to all tenants)
        /// </summary>
        /// <returns>True if super admin, false otherwise</returns>
        Task<bool> IsSuperAdminAsync();

        /// <summary>
        /// Checks if the specified user is a super admin
        /// </summary>
        /// <param name="customer">The customer/user</param>
        /// <returns>True if super admin, false otherwise</returns>
        Task<bool> IsSuperAdminAsync(Customer customer);
    }
}