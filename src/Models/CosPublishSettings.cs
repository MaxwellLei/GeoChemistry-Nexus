using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    public class CosPublishSettings
    {
        [JsonPropertyName("secretId")]
        public string SecretId { get; set; } = string.Empty;

        /// <summary>
        /// DPAPI 加密后的 SecretKey（Base64）
        /// </summary>
        [JsonPropertyName("protectedSecretKey")]
        public string ProtectedSecretKey { get; set; } = string.Empty;

        [JsonPropertyName("region")]
        public string Region { get; set; } = OfficialContentEndpoints.DefaultRegion;

        [JsonPropertyName("bucket")]
        public string Bucket { get; set; } = OfficialContentEndpoints.DefaultBucket;

        [JsonPropertyName("stagingDirectory")]
        public string StagingDirectory { get; set; } = string.Empty;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(SecretId)
            && !string.IsNullOrWhiteSpace(ProtectedSecretKey)
            && !string.IsNullOrWhiteSpace(Region)
            && !string.IsNullOrWhiteSpace(Bucket);
    }
}
