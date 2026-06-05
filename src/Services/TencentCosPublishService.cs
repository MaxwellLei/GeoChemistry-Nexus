using COSXML;
using COSXML.Auth;
using COSXML.Model.Object;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Services
{
    public static class TencentCosPublishService
    {
        private const string ProbeKey = ".publish_probe";

        private sealed class UploadProgressTracker
        {
            private readonly IProgress<(int current, int total)> _progress;

            public int Total { get; }
            public int Current { get; private set; }

            public UploadProgressTracker(IProgress<(int current, int total)> progress, int total)
            {
                _progress = progress;
                Total = total;
            }

            public void Advance()
            {
                Current++;
                _progress?.Report((Current, Total));
            }
        }

        public static async Task<CosUploadResult> UploadPublishResultAsync(
            string outputDir,
            PublishResult publishResult,
            CosPublishSettings settings,
            IProgress<string> log = null)
        {
            return await UploadPublishResultCoreAsync(outputDir, publishResult, settings, log, null);
        }

        private static async Task<CosUploadResult> UploadPublishResultCoreAsync(
            string outputDir,
            PublishResult publishResult,
            CosPublishSettings settings,
            IProgress<string> log,
            UploadProgressTracker uploadTracker)
        {
            if (settings == null || !settings.IsConfigured)
                throw new InvalidOperationException("COS publish settings are not configured.");

            string secretKey = CosPublishSettingsService.UnprotectSecretKey(settings);
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("Failed to decrypt COS SecretKey.");

            var cosXml = CreateCosClient(settings, secretKey);
            var uploadedKeys = new List<string>();

            void Log(string message)
            {
                log?.Report(message);
            }

            var manifestFiles = new[]
            {
                (Local: publishResult.GraphMapListPath, Key: OfficialContentEndpoints.GraphMapListFileName),
                (Local: publishResult.HomeLinksCatalogPath, Key: OfficialContentEndpoints.HomeLinksCatalogFileName),
                (Local: publishResult.CategoriesPath, Key: OfficialContentEndpoints.PlotTemplateCategoriesFileName),
                (Local: publishResult.ServerInfoPath, Key: OfficialContentEndpoints.ServerInfoFileName)
            };

            foreach (var file in manifestFiles)
            {
                if (string.IsNullOrEmpty(file.Local) || !File.Exists(file.Local))
                    continue;

                UploadFile(cosXml, settings.Bucket, file.Key, file.Local);
                uploadedKeys.Add(file.Key);
                uploadTracker?.Advance();
                Log($"Uploaded: {file.Key}");
            }

            foreach (var entry in publishResult.ManifestEntries)
            {
                if (entry.Action == PublishAction.Skip.ToString().ToLowerInvariant())
                    continue;

                if (string.IsNullOrEmpty(entry.LocalPath) || !File.Exists(entry.LocalPath))
                    continue;

                UploadFile(cosXml, settings.Bucket, entry.CosKey, entry.LocalPath);
                uploadedKeys.Add(entry.CosKey);
                uploadTracker?.Advance();
                Log($"Uploaded: {entry.CosKey}");
            }

            bool verified = await VerifyServerInfoAsync(publishResult.ListHash, publishResult.HomeLinksHash);
            Log(verified
                ? "server_info.json verification passed."
                : "Warning: server_info.json verification failed or CDN not yet refreshed.");

            return new CosUploadResult
            {
                Success = true,
                UploadedFileCount = uploadedKeys.Count,
                ServerInfoVerified = verified,
                Message = verified ? "Publish completed successfully." : "Publish completed with verification warning.",
                UploadedKeys = uploadedKeys
            };
        }

        public static async Task<CosUploadResult> UploadHomeLinksPublishResultAsync(
            HomeLinksPublishResult publishResult,
            CosPublishSettings settings,
            IProgress<string> log = null)
        {
            return await UploadHomeLinksPublishResultCoreAsync(publishResult, settings, log, null);
        }

        private static async Task<CosUploadResult> UploadHomeLinksPublishResultCoreAsync(
            HomeLinksPublishResult publishResult,
            CosPublishSettings settings,
            IProgress<string> log,
            UploadProgressTracker uploadTracker)
        {
            if (settings == null || !settings.IsConfigured)
                throw new InvalidOperationException("COS publish settings are not configured.");

            string secretKey = CosPublishSettingsService.UnprotectSecretKey(settings);
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("Failed to decrypt COS SecretKey.");

            var cosXml = CreateCosClient(settings, secretKey);
            var uploadedKeys = new List<string>();

            void Log(string message) => log?.Report(message);

            var manifestFiles = new[]
            {
                (Local: publishResult.HomeLinksCatalogPath, Key: OfficialContentEndpoints.HomeLinksCatalogFileName),
                (Local: publishResult.ServerInfoPath, Key: OfficialContentEndpoints.ServerInfoFileName)
            };

            foreach (var file in manifestFiles)
            {
                if (string.IsNullOrEmpty(file.Local) || !File.Exists(file.Local))
                    continue;

                UploadFile(cosXml, settings.Bucket, file.Key, file.Local);
                uploadedKeys.Add(file.Key);
                uploadTracker?.Advance();
                Log($"Uploaded: {file.Key}");
            }

            bool verified = await VerifyHomeLinksHashAsync(publishResult.HomeLinksHash);
            Log(verified
                ? "server_info.json home_links_hash verification passed."
                : "Warning: home_links_hash verification failed or CDN not yet refreshed.");

            return new CosUploadResult
            {
                Success = true,
                UploadedFileCount = uploadedKeys.Count,
                ServerInfoVerified = verified,
                Message = verified ? "Home links publish completed successfully." : "Home links publish completed with verification warning.",
                UploadedKeys = uploadedKeys
            };
        }

        public static async Task<CosUploadResult> UploadGeothermometerPublishResultAsync(
            GeothermometerPublishResult publishResult,
            CosPublishSettings settings,
            IProgress<string> log = null)
        {
            return await UploadGeothermometerPublishResultCoreAsync(publishResult, settings, log, null);
        }

        private static async Task<CosUploadResult> UploadGeothermometerPublishResultCoreAsync(
            GeothermometerPublishResult publishResult,
            CosPublishSettings settings,
            IProgress<string> log,
            UploadProgressTracker uploadTracker)
        {
            if (settings == null || !settings.IsConfigured)
                throw new InvalidOperationException("COS publish settings are not configured.");

            string secretKey = CosPublishSettingsService.UnprotectSecretKey(settings);
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("Failed to decrypt COS SecretKey.");

            var cosXml = CreateCosClient(settings, secretKey);
            var uploadedKeys = new List<string>();

            void Log(string message) => log?.Report(message);

            var manifestFiles = new[]
            {
                (Local: publishResult.ListPath, Key: $"{OfficialContentEndpoints.GeothermometerFolderName}/{OfficialContentEndpoints.GeoTListFileName}"),
                (Local: publishResult.IndexPath, Key: $"{OfficialContentEndpoints.GeothermometerFolderName}/{OfficialContentEndpoints.GeoTIndexFileName}")
            };

            foreach (var file in manifestFiles)
            {
                if (string.IsNullOrEmpty(file.Local) || !File.Exists(file.Local))
                    continue;

                UploadFile(cosXml, settings.Bucket, file.Key, file.Local);
                uploadedKeys.Add(file.Key);
                uploadTracker?.Advance();
                Log($"Uploaded: {file.Key}");
            }

            foreach (var entry in publishResult.ManifestEntries)
            {
                if (entry.Action == PublishAction.Skip.ToString().ToLowerInvariant())
                    continue;

                if (string.IsNullOrEmpty(entry.LocalPath) || !File.Exists(entry.LocalPath))
                    continue;

                UploadFile(cosXml, settings.Bucket, entry.CosKey, entry.LocalPath);
                uploadedKeys.Add(entry.CosKey);
                uploadTracker?.Advance();
                Log($"Uploaded: {entry.CosKey}");
            }

            bool verified = await VerifyGeoTIndexAsync(publishResult.ListHash);
            Log(verified
                ? "GeoT-index.json verification passed."
                : "Warning: GeoT-index.json verification failed or CDN not yet refreshed.");

            return new CosUploadResult
            {
                Success = true,
                UploadedFileCount = uploadedKeys.Count,
                ServerInfoVerified = verified,
                Message = verified ? "Geothermometer publish completed successfully." : "Geothermometer publish completed with verification warning.",
                UploadedKeys = uploadedKeys
            };
        }

        public static async Task<CosUploadResult> UploadAnnouncementPublishResultAsync(
            AnnouncementPublishResult publishResult,
            CosPublishSettings settings,
            IProgress<string> log = null)
        {
            return await UploadAnnouncementPublishResultCoreAsync(publishResult, settings, log, null);
        }

        private static async Task<CosUploadResult> UploadAnnouncementPublishResultCoreAsync(
            AnnouncementPublishResult publishResult,
            CosPublishSettings settings,
            IProgress<string> log,
            UploadProgressTracker uploadTracker)
        {
            if (settings == null || !settings.IsConfigured)
                throw new InvalidOperationException("COS publish settings are not configured.");

            string secretKey = CosPublishSettingsService.UnprotectSecretKey(settings);
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("Failed to decrypt COS SecretKey.");

            var cosXml = CreateCosClient(settings, secretKey);
            var uploadedKeys = new List<string>();

            void Log(string message) => log?.Report(message);

            if (string.IsNullOrEmpty(publishResult?.ServerInfoPath) || !File.Exists(publishResult.ServerInfoPath))
                throw new InvalidOperationException("server_info.json was not generated.");

            UploadFile(cosXml, settings.Bucket, OfficialContentEndpoints.ServerInfoFileName, publishResult.ServerInfoPath);
            uploadedKeys.Add(OfficialContentEndpoints.ServerInfoFileName);
            uploadTracker?.Advance();
            Log($"Uploaded: {OfficialContentEndpoints.ServerInfoFileName}");

            bool verified = await VerifyAnnouncementAsync(publishResult.Announcement);
            Log(verified
                ? "server_info.json announcement verification passed."
                : "Warning: announcement verification failed or CDN not yet refreshed.");

            return new CosUploadResult
            {
                Success = true,
                UploadedFileCount = uploadedKeys.Count,
                ServerInfoVerified = verified,
                Message = verified ? "Announcement publish completed successfully." : "Announcement publish completed with verification warning.",
                UploadedKeys = uploadedKeys
            };
        }

        public static async Task<CosUploadResult> UploadCombinedPublishAsync(
            string stagingRoot,
            PublishResult diagramResult,
            GeothermometerPublishResult geothermometerResult,
            HomeLinksPublishResult homeLinksResult,
            AnnouncementPublishResult announcementResult,
            CosPublishSettings settings,
            bool uploadDiagrams,
            bool uploadGeothermometers,
            bool uploadHomeLinks,
            bool uploadAnnouncement,
            IProgress<string> log = null,
            IProgress<(int current, int total)> uploadProgress = null)
        {
            var allKeys = new List<string>();
            bool diagramVerified = true;
            bool geoVerified = true;
            bool homeLinksVerified = true;
            bool announcementVerified = true;

            int totalFiles = CountCombinedUploadFiles(
                diagramResult,
                geothermometerResult,
                homeLinksResult,
                announcementResult,
                uploadDiagrams,
                uploadGeothermometers,
                uploadHomeLinks,
                uploadAnnouncement);
            var uploadTracker = uploadProgress != null ? new UploadProgressTracker(uploadProgress, totalFiles) : null;

            if (uploadDiagrams && diagramResult != null)
            {
                var diagramUpload = await UploadPublishResultCoreAsync(stagingRoot, diagramResult, settings, log, uploadTracker);
                allKeys.AddRange(diagramUpload.UploadedKeys);
                diagramVerified = diagramUpload.ServerInfoVerified;
            }
            else if (uploadHomeLinks && homeLinksResult != null)
            {
                var homeUpload = await UploadHomeLinksPublishResultCoreAsync(homeLinksResult, settings, log, uploadTracker);
                allKeys.AddRange(homeUpload.UploadedKeys);
                homeLinksVerified = homeUpload.ServerInfoVerified;
            }
            else if (uploadAnnouncement && announcementResult != null)
            {
                var announcementUpload = await UploadAnnouncementPublishResultCoreAsync(announcementResult, settings, log, uploadTracker);
                allKeys.AddRange(announcementUpload.UploadedKeys);
                announcementVerified = announcementUpload.ServerInfoVerified;
            }

            if (uploadGeothermometers && geothermometerResult != null)
            {
                var geoUpload = await UploadGeothermometerPublishResultCoreAsync(geothermometerResult, settings, log, uploadTracker);
                allKeys.AddRange(geoUpload.UploadedKeys);
                geoVerified = geoUpload.ServerInfoVerified;
            }

            bool allVerified = (!uploadDiagrams || diagramVerified)
                && (!uploadGeothermometers || geoVerified)
                && (!uploadHomeLinks || uploadDiagrams || homeLinksVerified)
                && (!uploadAnnouncement || uploadDiagrams || uploadHomeLinks || announcementVerified);

            return new CosUploadResult
            {
                Success = true,
                UploadedFileCount = allKeys.Count,
                ServerInfoVerified = allVerified,
                Message = allVerified ? "Publish completed successfully." : "Publish completed with verification warning.",
                UploadedKeys = allKeys
            };
        }

        public static async Task<bool> TestConnectionAsync(CosPublishSettings settings, string plainSecretKey = null)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.SecretId))
                return false;

            string secretKey = plainSecretKey ?? CosPublishSettingsService.UnprotectSecretKey(settings);
            if (string.IsNullOrEmpty(secretKey))
                return false;

            var cosXml = CreateCosClient(settings, secretKey);
            string probePath = Path.Combine(Path.GetTempPath(), $"gcn_publish_probe_{Guid.NewGuid():N}.txt");
            string probeContent = $"probe-{DateTime.UtcNow:O}";

            try
            {
                await File.WriteAllTextAsync(probePath, probeContent);
                UploadFile(cosXml, settings.Bucket, ProbeKey, probePath);

                try
                {
                    var deleteRequest = new DeleteObjectRequest(settings.Bucket, ProbeKey);
                    cosXml.DeleteObject(deleteRequest);
                }
                catch
                {
                    // probe cleanup is best-effort
                }

                return true;
            }
            finally
            {
                if (File.Exists(probePath))
                    File.Delete(probePath);
            }
        }

        private static int CountCombinedUploadFiles(
            PublishResult diagramResult,
            GeothermometerPublishResult geothermometerResult,
            HomeLinksPublishResult homeLinksResult,
            AnnouncementPublishResult announcementResult,
            bool uploadDiagrams,
            bool uploadGeothermometers,
            bool uploadHomeLinks,
            bool uploadAnnouncement)
        {
            int total = 0;

            if (uploadDiagrams && diagramResult != null)
                total += CountPublishResultFiles(diagramResult);
            else if (uploadHomeLinks && homeLinksResult != null)
                total += CountHomeLinksPublishFiles(homeLinksResult);
            else if (uploadAnnouncement && announcementResult != null)
                total += CountAnnouncementPublishFiles(announcementResult);

            if (uploadGeothermometers && geothermometerResult != null)
                total += CountGeothermometerPublishFiles(geothermometerResult);

            return total;
        }

        private static int CountPublishResultFiles(PublishResult publishResult)
        {
            if (publishResult == null)
                return 0;

            int count = 0;
            var manifestFiles = new[]
            {
                publishResult.GraphMapListPath,
                publishResult.HomeLinksCatalogPath,
                publishResult.CategoriesPath,
                publishResult.ServerInfoPath
            };

            count += manifestFiles.Count(path => !string.IsNullOrEmpty(path) && File.Exists(path));
            count += publishResult.ManifestEntries?.Count(entry =>
                entry.Action != PublishAction.Skip.ToString().ToLowerInvariant()
                && !string.IsNullOrEmpty(entry.LocalPath)
                && File.Exists(entry.LocalPath)) ?? 0;

            return count;
        }

        private static int CountHomeLinksPublishFiles(HomeLinksPublishResult publishResult)
        {
            if (publishResult == null)
                return 0;

            var manifestFiles = new[]
            {
                publishResult.HomeLinksCatalogPath,
                publishResult.ServerInfoPath
            };

            return manifestFiles.Count(path => !string.IsNullOrEmpty(path) && File.Exists(path));
        }

        private static int CountGeothermometerPublishFiles(GeothermometerPublishResult publishResult)
        {
            if (publishResult == null)
                return 0;

            int count = 0;
            var manifestFiles = new[]
            {
                publishResult.ListPath,
                publishResult.IndexPath
            };

            count += manifestFiles.Count(path => !string.IsNullOrEmpty(path) && File.Exists(path));
            count += publishResult.ManifestEntries?.Count(entry =>
                entry.Action != PublishAction.Skip.ToString().ToLowerInvariant()
                && !string.IsNullOrEmpty(entry.LocalPath)
                && File.Exists(entry.LocalPath)) ?? 0;

            return count;
        }

        private static int CountAnnouncementPublishFiles(AnnouncementPublishResult publishResult)
        {
            if (publishResult == null)
                return 0;

            return !string.IsNullOrEmpty(publishResult.ServerInfoPath) && File.Exists(publishResult.ServerInfoPath) ? 1 : 0;
        }

        private static CosXml CreateCosClient(CosPublishSettings settings, string secretKey)
        {
            var config = new CosXmlConfig.Builder()
                .IsHttps(true)
                .SetRegion(settings.Region)
                .SetDebugLog(false)
                .Build();

            var credentialProvider = new DefaultQCloudCredentialProvider(
                settings.SecretId,
                secretKey,
                600);

            return new CosXmlServer(config, credentialProvider);
        }

        private static void UploadFile(CosXml cosXml, string bucket, string key, string localPath)
        {
            var request = new PutObjectRequest(bucket, key, localPath);
            cosXml.PutObject(request);
        }

        private static async Task<bool> VerifyServerInfoAsync(string expectedListHash, string expectedHomeLinksHash = null)
        {
            if (string.IsNullOrEmpty(expectedListHash))
                return false;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (attempt > 0)
                        await Task.Delay(2000);

                    string json = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.ServerInfoUrl);
                    var serverInfo = JsonSerializer.Deserialize<ServerInfo>(json);
                    if (serverInfo != null
                        && string.Equals(serverInfo.ListHash, expectedListHash, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(expectedHomeLinksHash))
                            return true;

                        return string.Equals(serverInfo.HomeLinksHash, expectedHomeLinksHash, StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    // retry
                }
            }

            return false;
        }

        private static async Task<bool> VerifyHomeLinksHashAsync(string expectedHomeLinksHash)
        {
            if (string.IsNullOrEmpty(expectedHomeLinksHash))
                return false;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (attempt > 0)
                        await Task.Delay(2000);

                    string json = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.ServerInfoUrl);
                    var serverInfo = JsonSerializer.Deserialize<ServerInfo>(json);
                    if (serverInfo != null
                        && string.Equals(serverInfo.HomeLinksHash, expectedHomeLinksHash, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    // retry
                }
            }

            return false;
        }

        private static async Task<bool> VerifyAnnouncementAsync(string expectedAnnouncement)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (attempt > 0)
                        await Task.Delay(2000);

                    string json = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.ServerInfoUrl);
                    var serverInfo = JsonSerializer.Deserialize<ServerInfo>(json);
                    if (serverInfo != null
                        && string.Equals(serverInfo.Announcement?.Trim(), expectedAnnouncement?.Trim(), StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                catch
                {
                    // retry
                }
            }

            return false;
        }

        private static async Task<bool> VerifyGeoTIndexAsync(string expectedListHash)
        {
            if (string.IsNullOrEmpty(expectedListHash))
                return false;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (attempt > 0)
                        await Task.Delay(2000);

                    string json = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.GeoTIndexUrl);
                    var index = JsonSerializer.Deserialize<GeoTIndex>(json);
                    if (index != null
                        && string.Equals(index.ListHash, expectedListHash, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    // retry
                }
            }

            return false;
        }
    }
}
