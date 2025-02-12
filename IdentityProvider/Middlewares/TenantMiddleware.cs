using IdentityProvider.Services;

namespace IdentityProvider.Middlewares
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            var host = context.Request.Host.Host;
            var tenantName = ExtractTenantNameFromHost(host);
            var defaultOrganizationTenantName = Environment.GetEnvironmentVariable("DEFAULT_ORGANIZATION_TENANT_NAME") ?? string.Empty;

            tenantService.SetTenant(string.IsNullOrEmpty(tenantName) ? defaultOrganizationTenantName : tenantName);

            await _next(context);
        }

        private string ExtractTenantNameFromHost(string host)
        {
            var segments = host.Split('.');
            if (segments.Length > 2)
            {
                return segments[0];
            }

            return string.Empty;
        }
    }
}
