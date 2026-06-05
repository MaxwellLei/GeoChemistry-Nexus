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
    public static class GeothermometerPublishService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task<PublishPreview> GetPublishPreviewAsync()
        {
            GeothermometerService.Initialize();

            var preview = new PublishPreview();
            var localOfficial = GeothermometerService.LoadedEntities.Where(e => e.IsOfficial).ToList();

            Dictionary<string, PluginIndexEntry> remoteMap = new();
            try
            {
                string remoteJson = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.GeoTListUrl);
                var remoteList = JsonSerializer.Deserialize<PluginIndex>(remoteJson, JsonOptions);
                if (remoteList?.Plugins != null)
                {
                    foreach (var entry in remoteList.Plugins)
                        remoteMap[entry.Id] = entry;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerPublish] Failed to fetch remote list: {ex.Message}");
            }

            var localIds = new HashSet<string>();
            foreach (var local in localOfficial)
            {
                localIds.Add(local.PluginId);
                string localHash = GetEffectiveHash(local);

                if (!remoteMap.TryGetValue(local.PluginId, out var remote))
                {
                    preview.NewItems.Add(CreatePreviewItem(local, localHash, null, PublishAction.New));
                    continue;
                }

                if (string.Equals(localHash, remote.Hash, StringComparison.OrdinalIgnoreCase))
                    preview.UnchangedItems.Add(CreatePreviewItem(local, localHash, remote.Hash, PublishAction.Skip));
                else
                    preview.UpdatedItems.Add(CreatePreviewItem(local, localHash, remote.Hash, PublishAction.Update));
            }

            foreach (var remote in remoteMap.Values)
            {
                if (localIds.Contains(remote.Id))
                    continue;

                preview.RemovedItems.Add(new PublishPreviewItem
                {
                    Id = Guid.Empty,
                    GraphMapPath = remote.Id,
                    Name = remote.Id,
                    RemoteHash = remote.Hash,
                    Action = PublishAction.Remove
                });
            }

            return preview;
        }

        public static GeothermometerPublishResult ExportToDirectory(string stagingRoot)
        {
            if (string.IsNullOrWhiteSpace(stagingRoot))
                throw new ArgumentException("Staging root directory is required.", nameof(stagingRoot));

            GeothermometerService.Initialize();

            string outputDir = Path.Combine(stagingRoot, OfficialContentEndpoints.GeothermometerFolderName);
            var baselineHashes = LoadBaselineHashes(outputDir);

            var (exportedZipCount, total) = GeothermometerService.ExportAllOfficialToDirectory(outputDir);

            var manifestEntries = new List<PublishManifestEntry>();
            var newHashes = LoadBaselineHashes(outputDir);

            foreach (var kvp in newHashes)
            {
                string pluginId = kvp.Key;
                string currentHash = kvp.Value;
                baselineHashes.TryGetValue(pluginId, out string baselineHash);

                PublishAction action;
                if (string.IsNullOrEmpty(baselineHash))
                    action = PublishAction.New;
                else if (!string.Equals(currentHash, baselineHash, StringComparison.OrdinalIgnoreCase))
                    action = PublishAction.Update;
                else
                    action = PublishAction.Skip;

                string zipFileName = $"{pluginId}.zip";
                string zipPath = Path.Combine(outputDir, zipFileName);
                string cosKey = $"{OfficialContentEndpoints.GeothermometerFolderName}/{zipFileName}";

                var local = GeothermometerService.LoadedEntities.FirstOrDefault(e => e.PluginId == pluginId);

                manifestEntries.Add(new PublishManifestEntry
                {
                    Id = local?.Id.ToString() ?? pluginId,
                    GraphMapPath = pluginId,
                    Name = local?.FormulaName ?? pluginId,
                    FileHash = currentHash,
                    Action = action.ToString().ToLowerInvariant(),
                    CosKey = cosKey,
                    LocalPath = action != PublishAction.Skip && File.Exists(zipPath) ? zipPath : null
                });
            }

            int skippedZipCount = manifestEntries.Count(m =>
                m.Action == PublishAction.Skip.ToString().ToLowerInvariant());

            string listPath = Path.Combine(outputDir, OfficialContentEndpoints.GeoTListFileName);
            string indexPath = Path.Combine(outputDir, OfficialContentEndpoints.GeoTIndexFileName);
            string listHash = string.Empty;
            if (File.Exists(indexPath))
            {
                try
                {
                    string indexJson = File.ReadAllText(indexPath);
                    var index = JsonSerializer.Deserialize<GeoTIndex>(indexJson, JsonOptions);
                    listHash = index?.ListHash ?? string.Empty;
                }
                catch { /* ignore */ }
            }

            string manifestPath = Path.Combine(outputDir, OfficialContentEndpoints.GeothermometerPublishManifestFileName);
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifestEntries, JsonOptions));

            return new GeothermometerPublishResult
            {
                OutputDirectory = outputDir,
                TotalOfficialCount = total,
                ExportedZipCount = exportedZipCount,
                SkippedZipCount = skippedZipCount,
                ListPath = listPath,
                IndexPath = indexPath,
                ManifestPath = manifestPath,
                ListHash = listHash,
                ManifestEntries = manifestEntries
            };
        }

        private static Dictionary<string, string> LoadBaselineHashes(string outputDir)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string listPath = Path.Combine(outputDir, OfficialContentEndpoints.GeoTListFileName);
            if (!File.Exists(listPath)) return result;

            try
            {
                string json = File.ReadAllText(listPath);
                var list = JsonSerializer.Deserialize<PluginIndex>(json, JsonOptions);
                if (list?.Plugins == null) return result;

                foreach (var entry in list.Plugins)
                    result[entry.Id] = entry.Hash;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GeothermometerPublish] Failed to load baseline: {ex.Message}");
            }

            return result;
        }

        private static string GetEffectiveHash(GeothermometerEntity summary)
        {
            if (!string.IsNullOrEmpty(summary.FileHash))
                return summary.FileHash;

            var full = GeothermometerDatabaseService.Instance.GetEntity(summary.Id);
            if (full == null) return string.Empty;
            return GeothermometerDatabaseService.ComputeHash(full.ScriptContent ?? "");
        }

        private static PublishPreviewItem CreatePreviewItem(
            GeothermometerEntity local, string localHash, string remoteHash, PublishAction action)
        {
            return new PublishPreviewItem
            {
                Id = local.Id,
                GraphMapPath = local.PluginId,
                Name = local.FormulaName ?? local.PluginId,
                LocalHash = localHash,
                RemoteHash = remoteHash,
                Action = action
            };
        }
    }
}
