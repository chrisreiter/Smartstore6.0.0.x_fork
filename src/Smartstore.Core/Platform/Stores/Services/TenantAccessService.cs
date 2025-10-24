using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Stores.Services
{
    public class TenantAccessService : ITenantAccessService
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IPermissionService _permissionService;

        public TenantAccessService(
            SmartDbContext db,
            IWorkContext workContext,
            IPermissionService permissionService)
        {
            _db = db;
            _workContext = workContext;
            _permissionService = permissionService;
        }

        public async Task<Tenant> GetUserTenantAsync()
        {
            var customer = _workContext.CurrentCustomer;
            return await GetUserTenantAsync(customer);
        }

        public async Task<Tenant> GetUserTenantAsync(Customer customer)
        {
            if (customer == null)
                return null;

            // Check if user is super admin first (has access to all tenants)
            if (await IsSuperAdminAsync(customer))
                return null; // Super admin can access all tenants

            // Get the tenant this user has access to
            var tenantUser = await _db.TenantUsers
                .Include(tu => tu.Tenant)
                .Where(tu => tu.CustomerId == customer.Id && tu.Tenant.IsActive)
                .FirstOrDefaultAsync();

            return tenantUser?.Tenant;
        }

        public async Task<bool> HasTenantAccessAsync(int tenantId)
        {
            var customer = _workContext.CurrentCustomer;
            return await HasTenantAccessAsync(customer, tenantId);
        }

        public async Task<bool> HasTenantAccessAsync(Customer customer, int tenantId)
        {
            if (customer == null)
                return false;

            // Super admin has access to all tenants
            if (await IsSuperAdminAsync(customer))
                return true;

            // Check if user is assigned to this specific tenant
            return await _db.TenantUsers
                .AnyAsync(tu => tu.CustomerId == customer.Id && tu.TenantId == tenantId);
        }

        public async Task AssignUserToTenantAsync(int customerId, int tenantId)
        {
            // Check if assignment already exists
            var existingAssignment = await _db.TenantUsers
                .Where(tu => tu.CustomerId == customerId && tu.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (existingAssignment != null)
                return; // Already assigned

            // Remove user from any other tenant (one user = one tenant, unless super admin)
            var existingAssignments = await _db.TenantUsers
                .Where(tu => tu.CustomerId == customerId)
                .ToListAsync();

            if (existingAssignments.Any())
            {
                _db.TenantUsers.RemoveRange(existingAssignments);
            }

            // Create new assignment
            var tenantUser = new TenantUser
            {
                CustomerId = customerId,
                TenantId = tenantId,
                CreatedOnUtc = DateTime.UtcNow
            };

            _db.TenantUsers.Add(tenantUser);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveUserFromTenantAsync(int customerId, int tenantId)
        {
            var tenantUser = await _db.TenantUsers
                .Where(tu => tu.CustomerId == customerId && tu.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (tenantUser != null)
            {
                _db.TenantUsers.Remove(tenantUser);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<IList<Customer>> GetTenantUsersAsync(int tenantId)
        {
            var customers = await _db.TenantUsers
                .Include(tu => tu.Customer)
                .Where(tu => tu.TenantId == tenantId)
                .Select(tu => tu.Customer)
                .ToListAsync();

            return customers;
        }

        public async Task<bool> IsSuperAdminAsync()
        {
            var customer = _workContext.CurrentCustomer;
            return await IsSuperAdminAsync(customer);
        }

        public async Task<bool> IsSuperAdminAsync(Customer customer)
        {
            if (customer == null)
                return false;

            // Check if user is in Administrator role AND has super admin permissions
            return await _permissionService.AuthorizeAsync(Permissions.System.AccessShop, customer) &&
                   customer.IsAdmin();
        }
    }
}