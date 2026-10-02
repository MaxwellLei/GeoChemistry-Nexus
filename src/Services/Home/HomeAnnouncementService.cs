using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 主页公告：先读本地缓存（没有则复制安装包内置文件），再按 hash 后台同步。
    /// 与 HomeLinksCatalogService 相同的 hash 同步策略。
    /// </summary>
    public static class HomeAnnouncementService
    {
        private static string LocalCatalogPath => AppDataPathHelper.GetDataPath("Config", OfficialContentEndpoints.AnnouncementsFileName);

        private static string BundledCatalogPath => AppDataPathHelper.GetBundledDataPath("Home", OfficialContentEndpoints.AnnouncementsFileName);

        /// <summary>
        /// 读取本地公告（已按时间窗口过滤、按优先级排序）。不访问网络。
        /// 本地文件不存在时复制安装包内置目录。
        /// </summary>
        public static List<HomeAnnouncementEntry> LoadLocalAnnouncements()
        {
            return LoadLocalActiveEntries();
        }

        /// <summary>
        /// 用已取得的 server_info 同步公告目录。hash 相同则跳过下载。
        /// 下载或校验失败时保留本地文件。
        /// </summary>
        /// <returns>本地目录文件被新内容覆盖时为 true。</returns>
        public static async Task<bool> SyncFromServerAsync(ServerInfo? serverInfo)
        {
            string? serverHash = serverInfo?.AnnouncementsHash;
            if (string.IsNullOrWhiteSpace(serverHash))
                return false;

            try
            {
                EnsureLocalCatalogExists();
                string localHash = UpdateHelper.ComputeFileMd5(LocalCatalogPath);
                if (string.Equals(localHash, serverHash, StringComparison.OrdinalIgnoreCase))
                    return false;

                string catalogJson = await OfficialContentMirrorClient.GetStringAsync(OfficialContentEndpoints.AnnouncementsFileName);
                if (string.IsNullOrWhiteSpace(catalogJson))
                    return false;

                // 内容能反序列化才落盘，避免坏数据覆盖缓存
                var parsed = JsonHelper.Deserialize<HomeAnnouncementCatalog>(catalogJson);
                if (parsed?.Announcements == null)
                    return false;

                string? dir = Path.GetDirectoryName(LocalCatalogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(LocalCatalogPath, catalogJson);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeAnnouncementService] Sync failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 旧版兼容：服务器还没有 Announcements.json 时，用 server_info 里的纯文本公告。
        /// </summary>
        public static List<HomeAnnouncementEntry> CreateLegacyEntries(ServerInfo? serverInfo)
        {
            string legacy = serverInfo?.Announcement?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(legacy))
                return new List<HomeAnnouncementEntry>();

            return new List<HomeAnnouncementEntry>
            {
                new HomeAnnouncementEntry
                {
                    Id = "legacy",
                    Title = HomeLinksLocalization.FromPlain(legacy),
                    Theme = "blue"
                }
            };
        }

        private static List<HomeAnnouncementEntry> LoadLocalActiveEntries()
        {
            EnsureLocalCatalogExists();

            try
            {
                string? json = JsonHelper.ReadJsonFile(LocalCatalogPath);
                if (string.IsNullOrWhiteSpace(json))
                    return new List<HomeAnnouncementEntry>();

                var catalog = JsonHelper.Deserialize<HomeAnnouncementCatalog>(json);
                if (catalog?.Announcements == null)
                    return new List<HomeAnnouncementEntry>();

                var now = DateTime.Now;
                return catalog.Announcements
                    .Where(a => a != null && a.IsActive(now) && HomeLinksLocalization.HasText(a.Title))
                    .OrderByDescending(a => a.Priority)
                    .ThenByDescending(a => a.Date, StringComparer.Ordinal)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeAnnouncementService] Load failed: {ex.Message}");
                return new List<HomeAnnouncementEntry>();
            }
        }

        private static void EnsureLocalCatalogExists()
        {
            if (File.Exists(LocalCatalogPath) || !File.Exists(BundledCatalogPath))
                return;

            try
            {
                string? dir = Path.GetDirectoryName(LocalCatalogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.Copy(BundledCatalogPath, LocalCatalogPath, overwrite: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeAnnouncementService] Copy bundled announcements failed: {ex.Message}");
            }
        }
    }
}
