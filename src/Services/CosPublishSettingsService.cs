using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GeoChemistryNexus.Services
{
    public static class CosPublishSettingsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private static string SettingsPath => AppDataPathHelper.GetDataPath("Config", "cos_publish.json");

        public static CosPublishSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new CosPublishSettings();

                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<CosPublishSettings>(json, JsonOptions) ?? new CosPublishSettings();
            }
            catch
            {
                return new CosPublishSettings();
            }
        }

        public static void Save(CosPublishSettings settings, string? plainSecretKey = null)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            string? dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!string.IsNullOrEmpty(plainSecretKey))
                settings.ProtectedSecretKey = ProtectSecret(plainSecretKey);

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }

        public static string UnprotectSecretKey(CosPublishSettings settings)
        {
            if (settings == null || string.IsNullOrEmpty(settings.ProtectedSecretKey))
                return string.Empty;

            try
            {
                byte[] protectedBytes = Convert.FromBase64String(settings.ProtectedSecretKey);
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
    }
}
