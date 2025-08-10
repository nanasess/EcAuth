namespace IdentityProvider.Models
{
    public class State
    {
        public int OpenIdProviderId { get; set; }
        public string RedirectUri { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public int OrganizationId { get; set; }
        public string? Scope { get; set; }
    }
}
