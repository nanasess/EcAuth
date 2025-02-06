namespace IdentityProvider.Services
{
    public class TenantService : ITenantService
    {
        public string TenantName { get; private set; }

        public void SetTenant(string tenantName)
        {
            TenantName = tenantName;
        }
    }
}
