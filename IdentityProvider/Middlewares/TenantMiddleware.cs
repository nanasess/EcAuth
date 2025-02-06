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
            tenantService.SetTenant(tenantName);

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
