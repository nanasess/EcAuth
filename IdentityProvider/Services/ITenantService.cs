namespace IdentityProvider.Services
{
    public interface ITenantService
    {
        string TenantName { get; }
        void SetTenant(string tenantName);
    }
}
