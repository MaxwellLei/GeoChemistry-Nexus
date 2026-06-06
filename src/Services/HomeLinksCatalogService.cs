using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Services
{
    public static class HomeLinksCatalogService
    {
        private static readonly string LocalCatalogPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Data", "Config", OfficialContentEndpoints.HomeLinksCatalogFileName);

        private static readonly string BundledCatalogPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Data", "Home", OfficialContentEndpoints.HomeLinksCatalogFileName);

        /// <summary>
        /// 从服务器同步主页链接目录；若已是最新则跳过下载。
        /// </summary>
        /// <returns>若本地目录文件有变更则为 true。</returns>
        public static async Task<bool> SyncFromServerAsync()
        {
            try
            {
                string json = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.ServerInfoUrl);
                if (string.IsNullOrWhiteSpace(json))
                    return EnsureLocalCatalogExists();

                var serverInfo = JsonSerializer.Deserialize<ServerInfo>(json);
                if (serverInfo == null || string.IsNullOrWhiteSpace(serverInfo.HomeLinksHash))
                    return EnsureLocalCatalogExists();

                string localHash = UpdateHelper.ComputeFileMd5(LocalCatalogPath);
                if (string.Equals(localHash, serverInfo.HomeLinksHash, StringComparison.OrdinalIgnoreCase))
                    return false;

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                string catalogJson = await client.GetStringAsync(OfficialContentEndpoints.HomeLinksCatalogUrl);
                if (string.IsNullOrWhiteSpace(catalogJson))
                    return EnsureLocalCatalogExists();

                string downloadedHash = ComputeContentMd5(catalogJson);
                if (!string.Equals(downloadedHash, serverInfo.HomeLinksHash, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine("[HomeLinksCatalogService] Downloaded catalog hash mismatch.");
                    return EnsureLocalCatalogExists();
                }

                WriteLocalCatalog(catalogJson);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeLinksCatalogService] Sync failed: {ex.Message}");
                return EnsureLocalCatalogExists();
            }
        }

        public static HomeLinksCatalog LoadLocalCatalog()
        {
            EnsureLocalCatalogExists();

            try
            {
                string json = JsonHelper.ReadJsonFile(LocalCatalogPath);
                if (string.IsNullOrWhiteSpace(json))
                    return new HomeLinksCatalog();

                var catalog = JsonHelper.Deserialize<HomeLinksCatalog>(json);
                if (catalog?.Groups == null)
                    return new HomeLinksCatalog();

                catalog.Groups = catalog.Groups
                    .OrderBy(g => g.SortOrder)
                    .ThenBy(g => HomeLinksLocalization.GetSortKey(g.Title), StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return catalog;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeLinksCatalogService] Load failed: {ex.Message}");
                return new HomeLinksCatalog();
            }
        }

        public static string GetBundledCatalogPath() => BundledCatalogPath;

        public static string GetLocalCatalogPath() => LocalCatalogPath;

        private static bool EnsureLocalCatalogExists()
        {
            if (File.Exists(LocalCatalogPath))
                return false;

            if (!File.Exists(BundledCatalogPath))
                return false;

            try
            {
                string dir = Path.GetDirectoryName(LocalCatalogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.Copy(BundledCatalogPath, LocalCatalogPath, overwrite: false);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeLinksCatalogService] Copy bundled catalog failed: {ex.Message}");
                return false;
            }
        }

        private static void WriteLocalCatalog(string json)
        {
            string dir = Path.GetDirectoryName(LocalCatalogPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(LocalCatalogPath, json);
        }

        private static string ComputeContentMd5(string content)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"home_catalog_{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(tempPath, content);
                return UpdateHelper.ComputeFileMd5(tempPath);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { /* ignore */ }
                }
            }
        }
    }
}
