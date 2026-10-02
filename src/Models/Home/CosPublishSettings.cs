using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 一台腾讯云 COS 发布目标。密钥只保存 DPAPI 保护后的值。
    /// </summary>
    public class CosPublishTarget
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 客户端访问用的公共根地址，例如 https://bucket.cos.region.myqcloud.com
        /// </summary>
        [JsonPropertyName("publicBaseUrl")]
        public string PublicBaseUrl { get; set; } = string.Empty;

        [JsonPropertyName("secretId")]
        public string SecretId { get; set; } = string.Empty;

        [JsonPropertyName("protectedSecretKey")]
        public string ProtectedSecretKey { get; set; } = string.Empty;

        [JsonPropertyName("region")]
        public string Region { get; set; } = OfficialContentEndpoints.DefaultRegion;

        [JsonPropertyName("bucket")]
        public string Bucket { get; set; } = string.Empty;

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }

        [JsonIgnore]
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(SecretId)
            && !string.IsNullOrWhiteSpace(ProtectedSecretKey)
            && !string.IsNullOrWhiteSpace(Region)
            && !string.IsNullOrWhiteSpace(Bucket)
            && !string.IsNullOrWhiteSpace(PublicBaseUrl);

        [JsonIgnore]
        public string DisplayName =>
            !string.IsNullOrWhiteSpace(Name) ? Name.Trim()
            : !string.IsNullOrWhiteSpace(Bucket) ? Bucket.Trim()
            : Id;
    }

    /// <summary>
    /// 发布器本地配置：共用暂存目录，以及多台 COS 目标。
    /// </summary>
    public class CosPublishSettings
    {
        [JsonPropertyName("stagingDirectory")]
        public string StagingDirectory { get; set; } = string.Empty;

        [JsonPropertyName("targets")]
        public List<CosPublishTarget> Targets { get; set; } = new();

        [JsonIgnore]
        public IEnumerable<CosPublishTarget> EnabledTargets =>
            Targets
                .Where(target => target.Enabled && target.IsConfigured)
                .OrderBy(target => target.SortOrder);

        [JsonIgnore]
        public bool HasEnabledTarget => EnabledTargets.Any();
    }
}
