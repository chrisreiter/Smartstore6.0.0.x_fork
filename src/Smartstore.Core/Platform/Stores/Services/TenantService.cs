using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Stores.Services
{
    public class TenantService : ITenantService
    {
        private const string TENANT_KEY = "smartstore.tenant";
        
        private readonly SmartDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TenantService> _logger;

        public TenantService(
            SmartDbContext db,
            IHttpContextAccessor httpContextAccessor,
            ILogger<TenantService> logger)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<Tenant> GetCurrentTenantAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            // Check if tenant is already resolved and cached in HttpContext
            if (httpContext.Items.TryGetValue(TENANT_KEY, out var tenantObj) && tenantObj is Tenant cachedTenant)
            {
                return cachedTenant;
            }

            // Resolve tenant by hostname
            var host = httpContext.Request.Host.Host;
            var tenant = await ResolveTenantAsync(host);

            // Cache in HttpContext for this request
            if (tenant != null)
            {
                httpContext.Items[TENANT_KEY] = tenant;
            }

            return tenant;
        }

        public async Task<int?> GetCurrentTenantIdAsync()
        {
            var tenant = await GetCurrentTenantAsync();
            return tenant?.Id;
        }

        public async Task<Tenant> ResolveTenantAsync(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return null;

            try
            {
                // Normalize host (remove port if present)
                var normalizedHost = host.Split(':')[0].ToLowerInvariant();

                // Try exact domain match first
                var tenant = await _db.Tenants
                    .Where(t => t.IsActive && t.Domain.ToLower() == normalizedHost)
                    .FirstOrDefaultAsync();

                if (tenant != null)
                {
                    _logger.LogDebug("Resolved tenant '{TenantName}' for host '{Host}'", tenant.Name, host);
                    return tenant;
                }

                // Fallback: Try to find a tenant with wildcard or subdomain matching
                var tenants = await _db.Tenants
                    .Where(t => t.IsActive)
                    .ToListAsync();

                foreach (var t in tenants)
                {
                    if (IsHostMatch(normalizedHost, t.Domain.ToLowerInvariant()))
                    {
                        _logger.LogDebug("Resolved tenant '{TenantName}' for host '{Host}' via pattern matching", t.Name, host);
                        return t;
                    }
                }

                _logger.LogWarning("No tenant found for host '{Host}'", host);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving tenant for host '{Host}'", host);
                return null;
            }
        }

        public async Task<IList<Tenant>> GetAllActiveTenantsAsync()
        {
            return await _db.Tenants
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Tenant> GetTenantByIdAsync(int tenantId)
        {
            return await _db.Tenants
                .Include(t => t.Stores)
                .FirstOrDefaultAsync(t => t.Id == tenantId);
        }

        public async Task<Tenant> CreateTenantAsync(Tenant tenant)
        {
            Guard.NotNull(tenant);

            // Validate domain uniqueness
            if (!await IsDomainAvailableAsync(tenant.Domain))
            {
                throw new SmartException($"Domain '{tenant.Domain}' is already in use by another tenant.");
            }

            tenant.CreatedOnUtc = DateTime.UtcNow;
            tenant.UpdatedOnUtc = DateTime.UtcNow;

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created new tenant '{TenantName}' with domain '{Domain}'", tenant.Name, tenant.Domain);
            
            return tenant;
        }

        public async Task UpdateTenantAsync(Tenant tenant)
        {
            Guard.NotNull(tenant);

            // Validate domain uniqueness
            if (!await IsDomainAvailableAsync(tenant.Domain, tenant.Id))
            {
                throw new SmartException($"Domain '{tenant.Domain}' is already in use by another tenant.");
            }

            tenant.UpdatedOnUtc = DateTime.UtcNow;

            _db.Tenants.Update(tenant);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Updated tenant '{TenantName}' with domain '{Domain}'", tenant.Name, tenant.Domain);
        }

        public async Task DeleteTenantAsync(int tenantId)
        {
            var tenant = await GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return;

            // Check if tenant has associated stores
            var storeCount = await _db.Stores.CountAsync(s => s.TenantId == tenantId);
            if (storeCount > 0)
            {
                throw new SmartException($"Cannot delete tenant '{tenant.Name}' because it has {storeCount} associated store(s). Please reassign or delete the stores first.");
            }

            _db.Tenants.Remove(tenant);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Deleted tenant '{TenantName}' with domain '{Domain}'", tenant.Name, tenant.Domain);
        }

        public async Task<bool> IsDomainAvailableAsync(string domain, int? excludeTenantId = null)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return false;

            var normalizedDomain = domain.Trim().ToLowerInvariant();

            var query = _db.Tenants.Where(t => t.Domain.ToLower() == normalizedDomain);
            
            if (excludeTenantId.HasValue)
            {
                query = query.Where(t => t.Id != excludeTenantId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<IList<Store>> GetStoresByTenantAsync(int tenantId)
        {
            return await _db.Stores
                .Where(s => s.TenantId == tenantId)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }

        private static bool IsHostMatch(string requestHost, string tenantDomain)
        {
            // Direct match
            if (requestHost == tenantDomain)
                return true;

            // Wildcard subdomain support (*.example.com)
            if (tenantDomain.StartsWith("*."))
            {
                var baseDomain = tenantDomain.Substring(2);
                return requestHost.EndsWith("." + baseDomain) || requestHost == baseDomain;
            }

            // Subdomain matching for multi-level domains
            if (requestHost.Contains('.') && tenantDomain.Contains('.'))
            {
                var requestParts = requestHost.Split('.');
                var tenantParts = tenantDomain.Split('.');

                // Match if the base domain is the same (last two parts)
                if (requestParts.Length >= 2 && tenantParts.Length >= 2)
                {
                    var requestBase = string.Join(".", requestParts.Skip(requestParts.Length - 2));
                    var tenantBase = string.Join(".", tenantParts.Skip(tenantParts.Length - 2));
                    return requestBase == tenantBase;
                }
            }

            return false;
        }
    }
}