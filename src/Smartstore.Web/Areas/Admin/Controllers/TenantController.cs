using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Stores;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Utilities;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class TenantController : AdminController
    {
        private readonly SmartDbContext _db;

        public TenantController(SmartDbContext db)
        {
            _db = db;
        }

        [Permission(Permissions.Configuration.Store.Read)]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Store.Read)]
        public async Task<IActionResult> TenantList(GridCommand command)
        {
            var tenants = await _db.Tenants
                .Include(t => t.Stores)
                .OrderBy(t => t.Name)
                .ToPagedList(command)
                .LoadAsync();

            var gridModel = new GridModel<TenantModel>
            {
                Rows = tenants.Select(t => new TenantModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    Domain = t.Domain,
                    DatabaseSchema = t.DatabaseSchema,
                    IsActive = t.IsActive,
                    CreatedOn = Services.DateTimeHelper.ConvertToUserTime(t.CreatedOnUtc, DateTimeKind.Utc),
                    UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(t.UpdatedOnUtc, DateTimeKind.Utc),
                    StoreCount = t.Stores?.Count ?? 0
                }),
                Total = tenants.TotalCount
            };

            return Json(gridModel);
        }

        [Permission(Permissions.Configuration.Store.Create)]
        public IActionResult Create()
        {
            var model = new TenantModel
            {
                IsActive = true
            };

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Store.Create)]
        public async Task<IActionResult> Create(TenantModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if domain already exists
                    var domainExists = await _db.Tenants
                        .AnyAsync(t => t.Domain.ToLower() == model.Domain.ToLowerInvariant());
                    
                    if (domainExists)
                    {
                        NotifyError(T("MultiTenant.DomainAlreadyExists", model.Domain));
                        return View(model);
                    }

                    var tenant = new Tenant
                    {
                        Name = model.Name,
                        Domain = model.Domain.ToLowerInvariant(),
                        DatabaseSchema = model.DatabaseSchema,
                        IsActive = model.IsActive,
                        CreatedOnUtc = DateTime.UtcNow,
                        UpdatedOnUtc = DateTime.UtcNow
                    };

                    _db.Tenants.Add(tenant);
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.System.Tenants.Added"));

                    return continueEditing 
                        ? RedirectToAction(nameof(Edit), new { id = tenant.Id })
                        : RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    NotifyError(ex.Message);
                }
            }

            return View(model);
        }

        [Permission(Permissions.Configuration.Store.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var tenant = await _db.Tenants
                .Include(t => t.Stores)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (tenant == null)
                return NotFound();

            var model = new TenantModel
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Domain = tenant.Domain,
                DatabaseSchema = tenant.DatabaseSchema,
                IsActive = tenant.IsActive,
                CreatedOn = Services.DateTimeHelper.ConvertToUserTime(tenant.CreatedOnUtc, DateTimeKind.Utc),
                UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(tenant.UpdatedOnUtc, DateTimeKind.Utc)
            };

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Store.Update)]
        public async Task<IActionResult> Edit(TenantModel model, bool continueEditing)
        {
            var tenant = await _db.Tenants.FindAsync(model.Id);
            if (tenant == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if domain already exists for other tenants
                    var domainExists = await _db.Tenants
                        .AnyAsync(t => t.Domain.ToLower() == model.Domain.ToLowerInvariant() && t.Id != model.Id);
                    
                    if (domainExists)
                    {
                        NotifyError(T("MultiTenant.DomainAlreadyExists", model.Domain));
                        return View(model);
                    }

                    tenant.Name = model.Name;
                    tenant.Domain = model.Domain.ToLowerInvariant();
                    tenant.DatabaseSchema = model.DatabaseSchema;
                    tenant.IsActive = model.IsActive;
                    tenant.UpdatedOnUtc = DateTime.UtcNow;

                    _db.Tenants.Update(tenant);
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.System.Tenants.Updated"));

                    return continueEditing 
                        ? RedirectToAction(nameof(Edit), new { id = tenant.Id })
                        : RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    NotifyError(ex.Message);
                }
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Store.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var tenant = await _db.Tenants.Include(t => t.Stores).FirstOrDefaultAsync(t => t.Id == id);
                if (tenant == null)
                {
                    NotifyError(T("Admin.System.Tenants.NotFound"));
                    return RedirectToAction(nameof(Index));
                }

                // Check if tenant has associated stores
                if (tenant.Stores?.Any() == true)
                {
                    NotifyError(T("Admin.System.Tenants.CannotDeleteWithStores", tenant.Name, tenant.Stores.Count));
                    return RedirectToAction(nameof(Index));
                }

                _db.Tenants.Remove(tenant);
                await _db.SaveChangesAsync();
                
                NotifySuccess(T("Admin.System.Tenants.Deleted"));
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Store.Read)]
        public async Task<IActionResult> TenantStoreList(GridCommand command, int tenantId)
        {
            var stores = await _db.Stores
                .Where(s => s.TenantId == tenantId)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();

            var gridModel = new GridModel<StoreModel>
            {
                Rows = stores.Select(s => new StoreModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Url = s.Url,
                    SslEnabled = s.SslEnabled,
                    DisplayOrder = s.DisplayOrder,
                    IsSharedResource = s.IsSharedResource
                }),
                Total = stores.Count
            };

            return Json(gridModel);
        }

        /// <summary>
        /// (AJAX) Gets a list of all available tenants. 
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <returns>List of all tenants as JSON.</returns>
        [Permission(Permissions.Configuration.Store.Read)]
        public async Task<IActionResult> AllTenants(string label, string selectedIds)
        {
            var tenants = new List<Tenant>(await _db.Tenants.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync());
            var ids = selectedIds.ToIntArray();

            if (label.HasValue())
            {
                tenants.Insert(0, new Tenant { Name = label, Id = 0 });
            }

            var list = tenants.Select(t => new
            {
                id = t.Id.ToString(),
                text = t.Name,
                selected = ids.Contains(t.Id)
            })
            .ToList();

            return Json(list);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Store.Read)]
        public async Task<IActionResult> ValidateDomain(string domain, int? excludeId)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return Json(new { isAvailable = false, message = T("Admin.System.Tenants.Fields.Domain.Required") });

            var normalizedDomain = domain.Trim().ToLowerInvariant();
            var query = _db.Tenants.Where(t => t.Domain.ToLower() == normalizedDomain);
            
            if (excludeId.HasValue)
            {
                query = query.Where(t => t.Id != excludeId.Value);
            }

            var isAvailable = !await query.AnyAsync();
            
            return Json(new { 
                isAvailable, 
                message = isAvailable ? string.Empty : T("MultiTenant.DomainAlreadyExists", domain).ToString() 
            });
        }
    }

}