using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Services
{
    public static class CosPublishSettingsService
    {
        private static string SettingsPath => AppDataPathHelper.GetDataPath("Config", "cos_publish.json");

        public static CosPublishSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new CosPublishSettings();

                var file = JsonHelper.LoadFromFileOrNew<CosPublishSettingsFile>(SettingsPath);
                var settings = new CosPublishSettings
                {
                    StagingDirectory = file.StagingDirectory ?? string.Empty
                };

                if (file.Targets != null && file.Targets.Count > 0)
                {
                    settings.Targets = file.Targets;
                    return settings;
                }

                if (!string.IsNullOrWhiteSpace(file.SecretId) || !string.IsNullOrWhiteSpace(file.Bucket))
                {
                    string region = string.IsNullOrWhiteSpace(file.Region)
                        ? OfficialContentEndpoints.DefaultRegion
                        : file.Region.Trim();
                    settings.Targets.Add(new CosPublishTarget
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = "Hong Kong",
                        Enabled = true,
                        SecretId = file.SecretId?.Trim() ?? string.Empty,
                        ProtectedSecretKey = file.ProtectedSecretKey ?? string.Empty,
                        Region = region,
                        Bucket = file.Bucket?.Trim() ?? string.Empty,
                        PublicBaseUrl = OfficialContentEndpoints.BuildDefaultPublicBaseUrl(file.Bucket, region),
                        SortOrder = 0
                    });
                }

                return settings;
            }
            catch
            {
                return new CosPublishSettings();
            }
        }

        public static void Save(CosPublishSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            JsonHelper.SerializeToJsonFile(settings, SettingsPath);
        }

        public static string ProtectSecretKey(string plainSecretKey)
        {
            if (string.IsNullOrEmpty(plainSecretKey))
                return string.Empty;

            return ProtectSecret(plainSecretKey);
        }

        public static string UnprotectSecretKey(CosPublishTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.ProtectedSecretKey))
                return string.Empty;

            try
            {
                byte[] protectedBytes = Convert.FromBase64String(target.ProtectedSecretKey);
                byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ProtectSecret(string plainSecretKey)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainSecretKey);
            byte[] protectedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }

        /// <summary>
        /// 读取旧版单桶配置和新版目标列表。旧字段只用于迁移，保存时不再写出。
        /// </summary>
        private sealed class CosPublishSettingsFile
        {
            [JsonPropertyName("stagingDirectory")]
            public string StagingDirectory { get; set; } = string.Empty;

            [JsonPropertyName("targets")]
            public List<CosPublishTarget> Targets { get; set; } = new();

            [JsonPropertyName("secretId")]
            public string SecretId { get; set; } = string.Empty;

            [JsonPropertyName("protectedSecretKey")]
            public string ProtectedSecretKey { get; set; } = string.Empty;

            [JsonPropertyName("region")]
            public string Region { get; set; } = string.Empty;

            [JsonPropertyName("bucket")]
            public string Bucket { get; set; } = string.Empty;
        }
    }
}
