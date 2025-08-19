using IdentityProvider.Models;
using Microsoft.Extensions.Logging;

namespace IdentityProvider.Services
{
    /// <summary>
    /// Google OAuth2からのユーザー情報を正規化するクラス
    /// </summary>
    public class GoogleUserNormalizer : IExternalUserNormalizer
    {
        private readonly ILogger<GoogleUserNormalizer> _logger;

        public string ProviderName => "google";

        public GoogleUserNormalizer(ILogger<GoogleUserNormalizer> logger)
        {
            _logger = logger;
        }

        public ExternalUserInfo Normalize(dynamic googleResponse)
        {
            if (googleResponse == null)
            {
                _logger.LogError("Google response is null");
                throw new InvalidOperationException("Failed to normalize Google user information: response is null");
            }

            try
            {
                var externalUserInfo = new ExternalUserInfo
                {
                    Subject = GetPropertyValue(googleResponse, "sub"),
                    Email = GetPropertyValue(googleResponse, "email"),
                    Name = GetPropertyValue(googleResponse, "name"),
                    Provider = ProviderName,
                    Claims = ExtractClaims(googleResponse)
                };

                _logger.LogInformation("Successfully normalized Google user. Subject: {Subject}, HasEmail: {HasEmail}",
                    externalUserInfo.Subject, !string.IsNullOrEmpty(externalUserInfo.Email));

                return externalUserInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to normalize Google user response");
                throw new InvalidOperationException("Failed to normalize Google user information", ex);
            }
        }

        private static string GetPropertyValue(dynamic obj, string propertyName)
        {
            try
            {
                var value = obj?.GetType().GetProperty(propertyName)?.GetValue(obj);
                return value?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private Dictionary<string, object> ExtractClaims(dynamic googleResponse)
        {
            var claims = new Dictionary<string, object>();

            try
            {
                var properties = new[]
                {
                    "email_verified", "family_name", "given_name", "locale", "picture"
                };

                foreach (var property in properties)
                {
                    var value = GetPropertyValue(googleResponse, property);
                    if (!string.IsNullOrEmpty(value))
                    {
                        claims[property] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract some claims from Google response");
            }

            return claims;
        }
    }
}