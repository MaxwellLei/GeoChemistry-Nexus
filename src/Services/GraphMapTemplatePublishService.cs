using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Services
{
    public static class GraphMapTemplatePublishService
    {
        private static readonly JsonSerializerOptions ListJsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly JsonSerializerOptions ManifestJsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// 对比线上清单与本地 DB，返回发布预览
        /// </summary>
        public static async Task<PublishPreview> GetPublishPreviewAsync()
        {
            var preview = new PublishPreview();
            var dbService = GraphMapDatabaseService.Instance;
            var localOfficial = dbService.GetSummaries().Where(x => !x.IsCustom).ToList();

            Dictionary<Guid, GraphMapTemplateService.JsonTemplateItem> remoteMap = new();
            try
            {
                string remoteJson = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.GraphMapListUrl);
                var remoteList = JsonSerializer.Deserialize<List<GraphMapTemplateService.JsonTemplateItem>>(remoteJson);
                if (remoteList != null)
                {
                    foreach (var item in remoteList)
                    {
                        if (Guid.TryParse(item.ID, out Guid id))
                            remoteMap[id] = item;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PublishService] Failed to fetch remote list: {ex.Message}");
            }

            var localIds = new HashSet<Guid>();
            foreach (var local in localOfficial)
            {
                localIds.Add(local.Id);
                string localHash = GetEffectiveHash(local);

                if (!remoteMap.TryGetValue(local.Id, out var remote))
                {
                    preview.NewItems.Add(CreatePreviewItem(local, localHash, null, PublishAction.New));
                    continue;
                }

                if (string.Equals(localHash, remote.FileHash, StringComparison.OrdinalIgnoreCase))
                    preview.UnchangedItems.Add(CreatePreviewItem(local, localHash, remote.FileHash, PublishAction.Skip));
                else
                    preview.UpdatedItems.Add(CreatePreviewItem(local, localHash, remote.FileHash, PublishAction.Update));
            }

            foreach (var remote in remoteMap.Values)
            {
                if (!Guid.TryParse(remote.ID, out Guid remoteId) || localIds.Contains(remoteId))
                    continue;

                preview.RemovedItems.Add(new PublishPreviewItem
                {
                    Id = remoteId,
                    GraphMapPath = remote.GraphMapPath,
                    Name = remote.GraphMapPath,
                    RemoteHash = remote.FileHash,
                    Action = PublishAction.Remove
                });
            }

            return preview;
        }

        /// <summary>
        /// 增量导出官方模板发布包到指定目录
        /// </summary>
        public static PublishResult ExportToDirectory(string outputDir, PublishOptions? options = null)
        {
            options ??= new PublishOptions();
            if (string.IsNullOrWhiteSpace(outputDir))
                throw new ArgumentException("Output directory is required.", nameof(outputDir));

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var dbService = GraphMapDatabaseService.Instance;
            var officialSummaries = dbService.GetSummaries().Where(x => !x.IsCustom).ToList();

            var baselineHashes = LoadBaselineHashes(outputDir);
            string templatesDir = Path.Combine(outputDir, OfficialContentEndpoints.TemplatesFolderName);
            if (!Directory.Exists(templatesDir))
                Directory.CreateDirectory(templatesDir);

            var manifestEntries = new List<PublishManifestEntry>();
            int exportedZipCount = 0;
            int skippedZipCount = 0;

            foreach (var summary in officialSummaries)
            {
                var fullTemplate = dbService.GetTemplate(summary.Id);
                if (fullTemplate?.Content == null)
                    continue;

                string currentHash = GraphMapTemplateService.ComputeTemplateContentHash(fullTemplate.Content);
                string zipFileName = $"{summary.GraphMapPath}.zip";
                string zipPath = Path.Combine(templatesDir, zipFileName);
                string cosKey = $"{OfficialContentEndpoints.TemplatesFolderName}/{zipFileName}";

                baselineHashes.TryGetValue(summary.Id, out string? baselineHash);
                bool hashChanged = !string.Equals(currentHash, baselineHash, StringComparison.OrdinalIgnoreCase);
                bool zipMissing = !File.Exists(zipPath);
                bool needExport = options.ForceExportAll || hashChanged || zipMissing || string.IsNullOrEmpty(baselineHash);

                PublishAction action;
                if (string.IsNullOrEmpty(baselineHash))
                    action = PublishAction.New;
                else if (needExport)
                    action = PublishAction.Update;
                else
                    action = PublishAction.Skip;

                if (needExport)
                {
                    ExportTemplateZip(fullTemplate, zipPath);
                    exportedZipCount++;
                }
                else
                {
                    skippedZipCount++;
                }

                manifestEntries.Add(new PublishManifestEntry
                {
                    Id = summary.Id.ToString(),
                    GraphMapPath = summary.GraphMapPath,
                    Name = summary.Name ?? summary.GraphMapPath,
                    FileHash = currentHash,
                    Action = action.ToString().ToLowerInvariant(),
                    CosKey = cosKey,
                    LocalPath = needExport ? zipPath : null
                });

                if (fullTemplate.FileHash != currentHash)
                {
                    fullTemplate.FileHash = currentHash;
                    dbService.UpsertTemplate(fullTemplate);
                }
            }

            var exportList = officialSummaries
                .Where(s => manifestEntries.Any(m => m.Id == s.Id.ToString()))
                .Select(s =>
                {
                    var hash = manifestEntries.First(m => m.Id == s.Id.ToString()).FileHash;
                    var templateEntity = dbService.GetTemplate(s.Id);
                    return new GraphMapTemplateService.JsonTemplateItem
                    {
                        ID = s.Id.ToString(),
                        NodeList = s.NodeList,
                        GraphMapPath = s.GraphMapPath,
                        FileHash = hash,
                        Version = ContentVersionHelper.Normalize(
                            templateEntity?.Content?.Version ?? s.Version)
                    };
                })
                .ToList();

            string graphMapListPath = Path.Combine(outputDir, OfficialContentEndpoints.GraphMapListFileName);
            File.WriteAllText(graphMapListPath, JsonSerializer.Serialize(exportList, ListJsonOptions));

            string categoriesFileName = OfficialContentEndpoints.PlotTemplateCategoriesFileName;
            string srcCategoriesPath = AppDataPathHelper.GetDataPath("PlotData", categoriesFileName);
            string dstCategoriesPath = Path.Combine(outputDir, categoriesFileName);
            if (File.Exists(srcCategoriesPath))
                File.Copy(srcCategoriesPath, dstCategoriesPath, true);

            string listHash = UpdateHelper.ComputeFileMd5(graphMapListPath);
            string categoriesHash = File.Exists(dstCategoriesPath) ? UpdateHelper.ComputeFileMd5(dstCategoriesPath) : string.Empty;

            string homeLinksCatalogPath = ExportHomeLinksCatalog(outputDir);
            string homeLinksHash = !string.IsNullOrEmpty(homeLinksCatalogPath) && File.Exists(homeLinksCatalogPath)
                ? UpdateHelper.ComputeFileMd5(homeLinksCatalogPath)
                : LoadExistingHomeLinksHash(outputDir);

            var serverInfo = LoadMergedServerInfo(outputDir);
            serverInfo.ListHash = listHash;
            serverInfo.ListPlotCategoriesHash = categoriesHash;
            serverInfo.HomeLinksHash = homeLinksHash;
            if (options.PreserveAnnouncement != null)
                serverInfo.Announcement = options.PreserveAnnouncement;
            if (options.PreserveMinimumSupportedVersion != null)
                serverInfo.MinimumSupportedVersion = options.PreserveMinimumSupportedVersion;
            if (options.PreserveLatestAppVersion != null)
                serverInfo.LatestAppVersion = options.PreserveLatestAppVersion;

            string serverInfoPath = Path.Combine(outputDir, OfficialContentEndpoints.ServerInfoFileName);
            File.WriteAllText(serverInfoPath, JsonSerializer.Serialize(serverInfo, ListJsonOptions));

            string manifestPath = Path.Combine(outputDir, OfficialContentEndpoints.PublishManifestFileName);
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifestEntries, ManifestJsonOptions));

            return new PublishResult
            {
                OutputDirectory = outputDir,
                TotalOfficialCount = officialSummaries.Count,
                ExportedZipCount = exportedZipCount,
                SkippedZipCount = skippedZipCount,
                GraphMapListPath = graphMapListPath,
                HomeLinksCatalogPath = homeLinksCatalogPath,
                HomeLinksHash = homeLinksHash,
                ServerInfoPath = serverInfoPath,
                CategoriesPath = dstCategoriesPath,
                ManifestPath = manifestPath,
                ListHash = listHash,
                MinimumSupportedVersion = serverInfo.MinimumSupportedVersion ?? string.Empty,
                LatestAppVersion = serverInfo.LatestAppVersion ?? string.Empty,
                ManifestEntries = manifestEntries
            };
        }

        /// <summary>
        /// 发布成功后清除所有官方模板的 PendingPublish 标记
        /// </summary>
        public static void ClearPendingPublishFlags()
        {
            var dbService = GraphMapDatabaseService.Instance;
            foreach (var summary in dbService.GetSummaries().Where(x => !x.IsCustom && x.PendingPublish))
            {
                var entity = dbService.GetTemplate(summary.Id);
                if (entity == null) continue;
                entity.PendingPublish = false;
                dbService.UpsertTemplate(entity);
            }
        }

        private static Dictionary<Guid, string> LoadBaselineHashes(string outputDir)
        {
            var result = new Dictionary<Guid, string>();
            string listPath = Path.Combine(outputDir, OfficialContentEndpoints.GraphMapListFileName);
            if (!File.Exists(listPath))
                return result;

            try
            {
                string json = File.ReadAllText(listPath);
                var items = JsonSerializer.Deserialize<List<GraphMapTemplateService.JsonTemplateItem>>(json);
                if (items == null) return result;

                foreach (var item in items)
                {
                    if (Guid.TryParse(item.ID, out Guid id))
                        result[id] = item.FileHash;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PublishService] Failed to load baseline: {ex.Message}");
            }

            return result;
        }

        private static string ExportHomeLinksCatalog(string outputDir)
        {
            string srcPath = HomeLinksCatalogService.GetBundledCatalogPath();
            if (!File.Exists(srcPath))
                return string.Empty;

            string dstPath = Path.Combine(outputDir, OfficialContentEndpoints.HomeLinksCatalogFileName);
            File.Copy(srcPath, dstPath, true);
            return dstPath;
        }

        private static string LoadExistingHomeLinksHash(string outputDir)
        {
            return LoadMergedServerInfo(outputDir).HomeLinksHash ?? string.Empty;
        }

        private static ServerInfo LoadMergedServerInfo(string outputDir)
        {
            string path = Path.Combine(outputDir, OfficialContentEndpoints.ServerInfoFileName);
            if (File.Exists(path))
            {
                try
                {
                    var local = JsonSerializer.Deserialize<ServerInfo>(File.ReadAllText(path));
                    if (local != null)
                        return local;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PublishService] Load local server_info failed: {ex.Message}");
                }
            }

            try
            {
                string json = UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.ServerInfoUrl)
                    .GetAwaiter()
                    .GetResult();
                var remote = JsonSerializer.Deserialize<ServerInfo>(json);
                if (remote != null)
                    return remote;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PublishService] Load remote server_info failed: {ex.Message}");
            }

            return new ServerInfo();
        }

        private static string GetEffectiveHash(GraphMapTemplateEntity summary)
        {
            if (!string.IsNullOrEmpty(summary.FileHash))
                return summary.FileHash;

            var full = GraphMapDatabaseService.Instance.GetTemplate(summary.Id);
            if (full?.Content == null) return string.Empty;
            return GraphMapTemplateService.ComputeTemplateContentHash(full.Content);
        }

        private static PublishPreviewItem CreatePreviewItem(
            GraphMapTemplateEntity local, string localHash, string remoteHash, PublishAction action)
        {
            return new PublishPreviewItem
            {
                Id = local.Id,
                GraphMapPath = local.GraphMapPath,
                Name = local.Name ?? local.GraphMapPath,
                LocalHash = localHash,
                RemoteHash = remoteHash,
                Action = action
            };
        }

        private static void ExportTemplateZip(GraphMapTemplateEntity entity, string zipPath)
        {
            string fileName = entity.GraphMapPath;
            string templateJson = GraphMapTemplateService.SerializeTemplateContent(entity.Content);

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            var jsonEntry = archive.CreateEntry($"{fileName}.json");
            using (var entryStream = jsonEntry.Open())
            using (var streamWriter = new StreamWriter(entryStream))
            {
                streamWriter.Write(templateJson);
            }

            using (var thumbStream = GraphMapDatabaseService.Instance.GetThumbnail(entity.Id))
            {
                if (thumbStream != null)
                {
                    try
                    {
                        using var skBitmap = SKBitmap.Decode(thumbStream);
                        if (skBitmap != null)
                        {
                            var thumbEntry = archive.CreateEntry("thumbnail.jpg");
                            using var thumbEntryStream = thumbEntry.Open();
                            using var wStream = new SKManagedWStream(thumbEntryStream);
                            skBitmap.Encode(wStream, SKEncodedImageFormat.Jpeg, 85);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[PublishService] Thumbnail export error: {ex.Message}");
                    }
                }
            }

            if (entity.HelpDocuments != null)
            {
                foreach (var kvp in entity.HelpDocuments)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Value)) continue;
                    var helpEntry = archive.CreateEntry($"{kvp.Key}.rtf");
                    using var helpEntryStream = helpEntry.Open();
                    using var streamWriter = new StreamWriter(helpEntryStream);
                    streamWriter.Write(kvp.Value);
                }
            }
        }
    }
}
