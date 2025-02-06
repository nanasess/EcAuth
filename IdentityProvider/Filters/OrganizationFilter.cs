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

        public OrganizationFilter(ITenantService tenantService, EcAuthDbContext dbContext)
        {
            _tenantService = tenantService;
            _dbContext = dbContext;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_tenantService == null || string.IsNullOrEmpty(_tenantService.TenantName))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            var organization = await _dbContext.Organizations
                .FirstOrDefaultAsync(o => o.TenantName == _tenantService.TenantName);

            if (organization == null)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            Console.WriteLine($"Organization: {organization.Name}");

            //tenantService.SetOrganization(organization);

            await next();
        }
    }
}
