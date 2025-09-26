// Filters/OrganizationFilter.cs
using IdentityProvider.Models;
using IdentityProvider.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Filters
{
    public class OrganizationFilter : IAsyncActionFilter
    {
        private readonly ITenantService _tenantService;
        private readonly EcAuthDbContext _dbContext;
        private readonly ILogger<OrganizationFilter> _logger;

        public OrganizationFilter(ITenantService tenantService, EcAuthDbContext dbContext, ILogger<OrganizationFilter> logger)
        {
            _tenantService = tenantService;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_tenantService == null)
            {
                _logger.LogError("TenantService is null");
                context.Result = new ObjectResult(new { error = "tenant_service_unavailable", message = "Tenant service is not available" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
                return;
            }

            if (string.IsNullOrEmpty(_tenantService.TenantName))
            {
                _logger.LogWarning("TenantName is null or empty. TenantService: {TenantService}", _tenantService?.GetType().Name ?? "null");
                context.Result = new ObjectResult(new { error = "tenant_name_missing", message = "Tenant name could not be determined" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            _logger.LogInformation("Looking up organization for tenant: {TenantName}", _tenantService.TenantName);

            var organization = await _dbContext.Organizations
                .FirstOrDefaultAsync(o => o.TenantName == _tenantService.TenantName);

            if (organization == null)
            {
                _logger.LogWarning("Organization not found for tenant: {TenantName}", _tenantService.TenantName);

                // デバッグ用: 存在するorganizationを確認
                var allOrganizations = await _dbContext.Organizations.ToListAsync();
                _logger.LogInformation("Available organizations: {Organizations}",
                    string.Join(", ", allOrganizations.Select(o => $"Code={o.Code}, TenantName={o.TenantName ?? "NULL"}")));

                context.Result = new ObjectResult(new {
                    error = "organization_not_found",
                    message = $"Organization not found for tenant '{_tenantService.TenantName}'"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            _logger.LogInformation("Organization found: {OrganizationName} (Code: {OrganizationCode})", organization.Name, organization.Code);

            //tenantService.SetOrganization(organization);

            await next();
        }
    }
}
