using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>
/// Developer tool: previews and stages the official EPMA catalog for upload (same pipeline as the geothermobarometer publisher).
/// Staging layout: {root}/Epma/{packageId}.epma.zip + EPMA-List.json + EPMA-index.json + epma_publish_manifest.json.
/// </summary>
public static class EpmaPublishService
{
    private static EpmaTemplateCatalog Catalog => EpmaCompositionRoot.Templates;

    private static string ZipFileName(string packageId) => packageId + EpmaScriptPackage.Extension;

    /// <summary>Compares the installed official templates with the server's published EPMA-List.json.</summary>
    public static async Task<PublishPreview> GetPublishPreviewAsync()
    {
        var preview = new PublishPreview();
        var localOfficial = Catalog.Current.OfficialTemplates.Where(e => e.IsInstalled).ToList();

        var remoteMap = new Dictionary<string, EpmaIndexEntry>(StringComparer.OrdinalIgnoreCase);
        try
        {
            string remoteJson = await OfficialContentMirrorClient.GetStringAsync(
                $"{OfficialContentEndpoints.EpmaFolderName}/{OfficialContentEndpoints.EpmaListFileName}");
            var remoteList = JsonHelper.Deserialize<EpmaIndex>(remoteJson);
            foreach (var entry in remoteList?.Templates ?? [])
                if (!string.IsNullOrWhiteSpace(entry.Id)) remoteMap[entry.Id] = entry;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EpmaPublish] Failed to fetch remote list: {ex.Message}");
        }

        var localIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var local in localOfficial)
        {
            localIds.Add(local.PackageId);
            if (!remoteMap.TryGetValue(local.PackageId, out var remote))
            {
                preview.NewItems.Add(CreatePreviewItem(local, null, PublishAction.New));
                continue;
            }

            if (string.Equals(local.FileHash, remote.Hash, StringComparison.OrdinalIgnoreCase))
                preview.UnchangedItems.Add(CreatePreviewItem(local, remote.Hash, PublishAction.Skip));
            else
                preview.UpdatedItems.Add(CreatePreviewItem(local, remote.Hash, PublishAction.Update));
        }

        foreach (var remote in remoteMap.Values.Where(r => !localIds.Contains(r.Id)))
            preview.RemovedItems.Add(new PublishPreviewItem
            {
                Id = Guid.Empty,
                GraphMapPath = remote.Id,
                Name = remote.Id,
                RemoteHash = remote.Hash,
                Action = PublishAction.Remove,
            });

        return preview;
    }

    /// <summary>Incrementally writes changed packages, the full list, its hash index and the upload manifest.</summary>
    public static EpmaPublishResult ExportToDirectory(string stagingRoot)
    {
        if (string.IsNullOrWhiteSpace(stagingRoot))
            throw new ArgumentException("Staging root directory is required.", nameof(stagingRoot));

        string outputDir = Path.Combine(stagingRoot, OfficialContentEndpoints.EpmaFolderName);
        Directory.CreateDirectory(outputDir);

        string listPath = Path.Combine(outputDir, OfficialContentEndpoints.EpmaListFileName);
        string indexPath = Path.Combine(outputDir, OfficialContentEndpoints.EpmaIndexFileName);
        var baselineHashes = LoadBaselineHashes(listPath);

        var entries = new List<EpmaIndexEntry>();
        var manifestEntries = new List<PublishManifestEntry>();
        int exported = 0;

        foreach (var summary in Catalog.Current.OfficialTemplates.Where(e => e.IsInstalled).OrderBy(e => e.PackageId, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var full = Catalog.Database.GetEntity(summary.Id);
                if (full is not { HasPackage: true }) continue;

                var package = EpmaScriptPackage.Read(full.PackageBytes);
                var manifest = package.Manifest;
                string fileName = ZipFileName(manifest.PackageId);
                if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
                    throw new InvalidDataException("Package id cannot be used as a file name.");

                string zipPath = Path.Combine(outputDir, fileName);
                baselineHashes.TryGetValue(manifest.PackageId, out string? baseline);
                bool zipMissing = !File.Exists(zipPath);
                PublishAction action = string.IsNullOrEmpty(baseline) ? PublishAction.New
                    : !string.Equals(baseline, package.Sha256, StringComparison.OrdinalIgnoreCase) ? PublishAction.Update : PublishAction.Skip;

                if (action != PublishAction.Skip || zipMissing)
                {
                    File.WriteAllBytes(zipPath, package.CopyBytes());
                    exported++;
                    if (action == PublishAction.Skip) action = PublishAction.Update;
                }

                entries.Add(new EpmaIndexEntry
                {
                    Id = manifest.PackageId,
                    FormatVersion = manifest.FormatVersion,
                    Version = manifest.Version,
                    Name = manifest.Name,
                    Description = manifest.Description,
                    Tags = JsonSerializer.SerializeToElement(manifest.Tags),
                    Author = manifest.Author ?? string.Empty,
                    UpdatedAt = manifest.UpdatedAt ?? string.Empty,
                    Icon = manifest.Icon,
                    DownloadUrl = fileName,
                    Hash = package.Sha256,
                });

                manifestEntries.Add(new PublishManifestEntry
                {
                    Id = full.Id.ToString(),
                    GraphMapPath = manifest.PackageId,
                    Name = EpmaScriptText.Label(manifest.Name, manifest.PackageId),
                    FileHash = package.Sha256,
                    Action = action.ToString().ToLowerInvariant(),
                    CosKey = $"{OfficialContentEndpoints.EpmaFolderName}/{fileName}",
                    LocalPath = action != PublishAction.Skip ? zipPath : null,
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EpmaPublish] Export failed [{summary.PackageId}]: {ex.Message}");
            }
        }

        var list = new EpmaIndex { IndexVersion = DateTime.Now.ToString("yyyy.MM.dd"), Templates = entries };
        string listContent = JsonSerializer.Serialize(list, JsonHelper.DefaultOptions);
        File.WriteAllText(listPath, listContent);

        string listHash = EpmaTemplateDatabaseService.ComputeHash(listContent);
        File.WriteAllText(indexPath, JsonSerializer.Serialize(
            new EpmaListIndex { ListHash = listHash, IndexVersion = list.IndexVersion }, JsonHelper.DefaultOptions));

        string manifestPath = Path.Combine(outputDir, OfficialContentEndpoints.EpmaPublishManifestFileName);
        File.WriteAllText(manifestPath, JsonHelper.Serialize(manifestEntries));

        return new EpmaPublishResult
        {
            OutputDirectory = outputDir,
            TotalOfficialCount = entries.Count,
            ExportedZipCount = exported,
            SkippedZipCount = manifestEntries.Count(m => m.Action == PublishAction.Skip.ToString().ToLowerInvariant()),
            ListPath = listPath,
            IndexPath = indexPath,
            ManifestPath = manifestPath,
            ListHash = listHash,
            ManifestEntries = manifestEntries,
        };
    }

    /// <summary>Records the published hashes on the local official templates once the upload succeeded.</summary>
    public static Task ApplyPublishedResultAsync(EpmaPublishResult result)
        => Catalog.MarkPublishedAsync(result.ManifestEntries.Select(e => (e.GraphMapPath, e.FileHash)));

    private static Dictionary<string, string> LoadBaselineHashes(string listPath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(listPath)) return result;
        try
        {
            var list = JsonHelper.Deserialize<EpmaIndex>(File.ReadAllText(listPath));
            foreach (var entry in list?.Templates ?? [])
                result[entry.Id] = entry.Hash;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EpmaPublish] Failed to load baseline: {ex.Message}");
        }
        return result;
    }

    private static PublishPreviewItem CreatePreviewItem(EpmaTemplateEntity local, string? remoteHash, PublishAction action) => new()
    {
        Id = local.Id,
        GraphMapPath = local.PackageId,
        Name = EpmaTemplateDisplay.Name(local),
        LocalHash = local.FileHash,
        RemoteHash = remoteHash ?? string.Empty,
        Action = action,
    };
}
