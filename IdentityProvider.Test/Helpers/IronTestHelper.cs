using IdpUtilities;
using System.Text.Json;
using System.Threading.Tasks;

namespace IdentityProvider.Test.Helpers
{
    public static class IronTestHelper
    {
        public const string TestPassword = "test-password-32-characters-long!";

        public static async Task<string> SealStateAsync<T>(T data)
        {
            var json = JsonSerializer.Serialize(data);
            var options = new Iron.Options();
            return await Iron.Seal(json, TestPassword, options);
        }

        public static async Task<T> UnsealStateAsync<T>(string sealedData)
        {
            var options = new Iron.Options();
            var json = await Iron.Unseal<string>(sealedData, TestPassword, options);
            return JsonSerializer.Deserialize<T>(json)!;
        }
    }
}