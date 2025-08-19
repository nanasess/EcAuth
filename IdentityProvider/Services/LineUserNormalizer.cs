using IdentityProvider.Models;
using Microsoft.Extensions.Logging;

namespace IdentityProvider.Services
{
    /// <summary>
    /// LINE Loginからのユーザー情報を正規化するクラス
    /// </summary>
    public class LineUserNormalizer : IExternalUserNormalizer
    {
        private readonly ILogger<LineUserNormalizer> _logger;

        public string ProviderName => "line";

        public LineUserNormalizer(ILogger<LineUserNormalizer> logger)
        {
            _logger = logger;
        }

        public ExternalUserInfo Normalize(dynamic lineResponse)
        {
            if (lineResponse == null)
            {
                _logger.LogError("LINE response is null");
                throw new InvalidOperationException("Failed to normalize LINE user information: response is null");
            }

            try
            {
                var externalUserInfo = new ExternalUserInfo
                {
                    Subject = GetPropertyValue(lineResponse, "userId"),
                    Email = null, // LINEは基本的にメールを提供しない
                    Name = GetPropertyValue(lineResponse, "displayName"),
                    Provider = ProviderName,
                    Claims = ExtractClaims(lineResponse)
                };

                _logger.LogInformation("Successfully normalized LINE user. Subject: {Subject}, DisplayName: {DisplayName}",
                    externalUserInfo.Subject, externalUserInfo.Name);

                return externalUserInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to normalize LINE user response");
                throw new InvalidOperationException("Failed to normalize LINE user information", ex);
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

        private Dictionary<string, object> ExtractClaims(dynamic lineResponse)
        {
            var claims = new Dictionary<string, object>();

            try
            {
                var properties = new[]
                {
                    "pictureUrl", "statusMessage", "language"
                };

                foreach (var property in properties)
                {
                    var value = GetPropertyValue(lineResponse, property);
                    if (!string.IsNullOrEmpty(value))
                    {
                        claims[property] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract some claims from LINE response");
            }

            return claims;
        }
    }
}