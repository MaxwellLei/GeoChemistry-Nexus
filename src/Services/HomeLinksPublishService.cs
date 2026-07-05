using GeoChemistryNexus.Converter;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Services
{
    public static class HomeLinksPublishService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new LocalizedStringJsonConverter() }
        };

        public static HomeLinksCatalog LoadBundledCatalog()
        {
            string path = HomeLinksCatalogService.GetBundledCatalogPath();
            if (!File.Exists(path))
                return new HomeLinksCatalog();

            try
            {
                string json = File.ReadAllText(path);
                return JsonHelper.Deserialize<HomeLinksCatalog>(json) ?? new HomeLinksCatalog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeLinksPublishService] Load bundled failed: {ex.Message}");
                return new HomeLinksCatalog();
            }
        }

        public static void SaveBundledCatalog(HomeLinksCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            string path = HomeLinksCatalogService.GetBundledCatalogPath();
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            NormalizeCatalog(catalog);
            File.WriteAllText(path, JsonSerializer.Serialize(catalog, JsonOptions));
        }

        public static async Task<HomeLinksPublishPreview> GetPublishPreviewAsync(HomeLinksCatalog catalog)
        {
            var preview = new HomeLinksPublishPreview();
            string localHash = ComputeCatalogHash(catalog);
            preview.LocalHash = localHash;
            preview.GroupCount = catalog?.Groups?.Count ?? 0;
            preview.LinkCount = catalog?.Groups?
                .SelectMany(g => g.Links ?? Enumerable.Empty<HomeLinkEntry>())
                .Count() ?? 0;

            try
            {
                string json = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.ServerInfoUrl);
                var serverInfo = JsonSerializer.Deserialize<ServerInfo>(json);
                preview.RemoteHash = serverInfo?.HomeLinksHash ?? string.Empty;
                preview.HasRemoteChanges = !string.Equals(localHash, preview.RemoteHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeLinksPublishService] Preview failed: {ex.Message}");
                preview.HasRemoteChanges = true;
            }

            return preview;
        }

        public static AnnouncementPublishResult ExportAnnouncementToDirectory(
            string outputDir,
            string announcement,
            string? minimumSupportedVersion = null,
            string? latestAppVersion = null)
        {
            if (string.IsNullOrWhiteSpace(outputDir))
                throw new ArgumentException("Output directory is required.", nameof(outputDir));

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var serverInfo = LoadMergedServerInfo(outputDir);
            serverInfo.Announcement = announcement ?? string.Empty;
            if (minimumSupportedVersion != null)
                serverInfo.MinimumSupportedVersion = minimumSupportedVersion;
            if (latestAppVersion != null)
                serverInfo.LatestAppVersion = latestAppVersion;

            string serverInfoPath = Path.Combine(outputDir, OfficialContentEndpoints.ServerInfoFileName);
            File.WriteAllText(serverInfoPath, JsonSerializer.Serialize(serverInfo, JsonOptions));

            return new AnnouncementPublishResult
            {
                OutputDirectory = outputDir,
                ServerInfoPath = serverInfoPath,
                Announcement = announcement ?? string.Empty,
                MinimumSupportedVersion = serverInfo.MinimumSupportedVersion ?? string.Empty,
                LatestAppVersion = serverInfo.LatestAppVersion ?? string.Empty
            };
        }

        public static HomeLinksPublishResult ExportToDirectory(
            string outputDir,
            HomeLinksCatalog catalog,
            string? preserveAnnouncement = null,
            string? preserveMinimumSupportedVersion = null,
            string? preserveLatestAppVersion = null)
        {
            if (string.IsNullOrWhiteSpace(outputDir))
                throw new ArgumentException("Output directory is required.", nameof(outputDir));

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            NormalizeCatalog(catalog);

            string catalogPath = Path.Combine(outputDir, OfficialContentEndpoints.HomeLinksCatalogFileName);
            File.WriteAllText(catalogPath, JsonSerializer.Serialize(catalog, JsonOptions));
            string homeLinksHash = UpdateHelper.ComputeFileMd5(catalogPath);

            var serverInfo = LoadMergedServerInfo(outputDir);
            serverInfo.HomeLinksHash = homeLinksHash;
            if (preserveAnnouncement != null)
                serverInfo.Announcement = preserveAnnouncement;
            if (preserveMinimumSupportedVersion != null)
                serverInfo.MinimumSupportedVersion = preserveMinimumSupportedVersion;
            if (preserveLatestAppVersion != null)
                serverInfo.LatestAppVersion = preserveLatestAppVersion;

            string serverInfoPath = Path.Combine(outputDir, OfficialContentEndpoints.ServerInfoFileName);
            File.WriteAllText(serverInfoPath, JsonSerializer.Serialize(serverInfo, JsonOptions));

            return new HomeLinksPublishResult
            {
                OutputDirectory = outputDir,
                HomeLinksCatalogPath = catalogPath,
                HomeLinksHash = homeLinksHash,
                ServerInfoPath = serverInfoPath,
                Announcement = serverInfo.Announcement ?? string.Empty,
                MinimumSupportedVersion = serverInfo.MinimumSupportedVersion ?? string.Empty,
                LatestAppVersion = serverInfo.LatestAppVersion ?? string.Empty,
                GroupCount = catalog.Groups?.Count ?? 0,
                LinkCount = catalog.Groups?.SelectMany(g => g.Links ?? Enumerable.Empty<HomeLinkEntry>()).Count() ?? 0
            };
        }

        public static HomeLinksCatalog BuildCatalog(IEnumerable<HomeLinkGroup> groups, int version = 2)
        {
            var catalog = new HomeLinksCatalog
            {
                Version = version,
                Groups = groups?
                    .Where(g => g?.Links != null && g.Links.Count > 0)
                    .OrderBy(g => g.SortOrder)
                    .ThenBy(g => HomeLinksLocalization.GetSortKey(g.Title), StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<HomeLinkGroup>()
            };

            return catalog;
        }

        public static string ComputeCatalogHash(HomeLinksCatalog catalog)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"home_catalog_{Guid.NewGuid():N}.json");
            try
            {
                var copy = new HomeLinksCatalog
                {
                    Version = catalog?.Version ?? 2,
                    Groups = catalog?.Groups?.Select(g => new HomeLinkGroup
                    {
                        Id = g.Id,
                        Title = HomeLinksLocalization.Clone(g.Title),
                        SortOrder = g.SortOrder,
                        Links = g.Links?.Select(l => new HomeLinkEntry
                        {
                            Id = l.Id,
                            Title = HomeLinksLocalization.Clone(l.Title),
                            Description = HomeLinksLocalization.Clone(l.Description),
                            Url = l.Url,
                            Icon = HomeIconHelper.ResolveIcon(l.Icon)
                        }).ToList() ?? new List<HomeLinkEntry>()
                    }).ToList() ?? new List<HomeLinkGroup>()
                };

                NormalizeCatalog(copy);
                File.WriteAllText(tempPath, JsonSerializer.Serialize(copy, JsonOptions));
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

        public static HomeLinkGroup CreateGroup(string id, LocalizedString title, int sortOrder)
        {
            return new HomeLinkGroup
            {
                Id = string.IsNullOrWhiteSpace(id) ? Slugify(HomeLinksLocalization.GetSortKey(title)) : id.Trim(),
                Title = HomeLinksLocalization.Clone(title),
                SortOrder = sortOrder,
                Links = new List<HomeLinkEntry>()
            };
        }

        public static HomeLinkEntry CreateLink(string id, LocalizedString title, string url, LocalizedString description, string icon)
        {
            return new HomeLinkEntry
            {
                Id = string.IsNullOrWhiteSpace(id) ? Slugify(HomeLinksLocalization.GetSortKey(title)) : id.Trim(),
                Title = HomeLinksLocalization.Clone(title),
                Url = url.Trim(),
                Description = HomeLinksLocalization.Clone(description),
                Icon = HomeIconHelper.ResolveIcon(icon)
            };
        }

        private static void NormalizeCatalog(HomeLinksCatalog catalog)
        {
            catalog.Groups = catalog.Groups?
                .Where(g => g.Links != null && g.Links.Count > 0)
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => HomeLinksLocalization.GetSortKey(g.Title), StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<HomeLinkGroup>();
        }

        private static ServerInfo LoadMergedServerInfo(string outputDir)
        {
            string localPath = Path.Combine(outputDir, OfficialContentEndpoints.ServerInfoFileName);
            if (File.Exists(localPath))
            {
                try
                {
                    var info = JsonSerializer.Deserialize<ServerInfo>(File.ReadAllText(localPath));
                    if (info != null)
                        return info;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HomeLinksPublishService] Load local server_info failed: {ex.Message}");
                }
            }

            try
            {
                string json = UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.ServerInfoUrl)
                    .GetAwaiter().GetResult();
                var remote = JsonSerializer.Deserialize<ServerInfo>(json);
                if (remote != null)
                    return remote;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeLinksPublishService] Load remote server_info failed: {ex.Message}");
            }

            return new ServerInfo();
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Guid.NewGuid().ToString("N")[..8];

            var chars = value.Trim().ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray();
            string slug = new string(chars).Trim('-');
            while (slug.Contains("--", StringComparison.Ordinal))
                slug = slug.Replace("--", "-", StringComparison.Ordinal);

            return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
        }
    }

    public class HomeLinksPublishPreview
    {
        public int GroupCount { get; set; }
        public int LinkCount { get; set; }
        public string LocalHash { get; set; } = string.Empty;
        public string RemoteHash { get; set; } = string.Empty;
        public bool HasRemoteChanges { get; set; }
    }
}
