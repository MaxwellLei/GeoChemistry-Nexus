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
/// EPMA official-template update flow, same shape as the diagram and geothermobarometer modules:
/// EPMA-index.json (hash of the list) -> EPMA-List.json (catalog) -> per-package .epma.zip.
/// Checking only syncs the catalog into the local database (placeholders and statuses); packages are downloaded on demand.
/// </summary>
public static class EpmaUpdateService
{
    private static readonly JsonSerializerOptions JsonOptions = JsonHelper.DefaultOptions;

    private static string LocalListFilePath => AppDataPathHelper.GetDataPath("Epma", OfficialContentEndpoints.EpmaListFileName);

    private static string EpmaRelative(string fileName) => $"{OfficialContentEndpoints.EpmaFolderName}/{fileName.TrimStart('/')}";

    private static EpmaTemplateCatalog Catalog => EpmaCompositionRoot.Templates;

    private static EpmaTemplateDatabaseService Database => Catalog.Database;

    // ---------------------------------------------------------------- check

    /// <summary>Fetches EPMA-index.json, refreshes the cached EPMA-List.json when its hash changed and syncs the catalog.</summary>
    public static async Task<EpmaUpdateCheckResult> CheckForUpdatesAsync(bool downloadListIfOutdated = true)
    {
        try
        {
            string indexJson = await OfficialContentMirrorClient.GetStringAsync(EpmaRelative(OfficialContentEndpoints.EpmaIndexFileName));
            var listIndex = JsonSerializer.Deserialize<EpmaListIndex>(indexJson, JsonOptions);
            if (listIndex == null || string.IsNullOrEmpty(listIndex.ListHash))
                return Failed("Invalid EPMA-index.json");

            bool needDownloadList = true;
            if (File.Exists(LocalListFilePath))
            {
                string localListContent = await File.ReadAllTextAsync(LocalListFilePath);
                needDownloadList = !string.Equals(EpmaTemplateDatabaseService.ComputeHash(localListContent),
                    listIndex.ListHash, StringComparison.OrdinalIgnoreCase);
            }

            bool listDownloaded = false;
            string listJson;
            if (needDownloadList)
            {
                if (!downloadListIfOutdated)
                    return new EpmaUpdateCheckResult
                    {
                        Status = EpmaUpdateCheckStatus.Success,
                        ListOutdated = true,
                        NotInstalledCount = CountByStatus(EpmaInstallStatus.NotInstalled),
                        OutdatedCount = CountByStatus(EpmaInstallStatus.Outdated),
                    };

                listJson = await OfficialContentMirrorClient.GetStringAsync(EpmaRelative(OfficialContentEndpoints.EpmaListFileName));
                if (!string.Equals(EpmaTemplateDatabaseService.ComputeHash(listJson), listIndex.ListHash, StringComparison.OrdinalIgnoreCase))
                    return Failed(LanguageService.Instance["downloaded_file_hash_mismatch"]);

                Directory.CreateDirectory(Path.GetDirectoryName(LocalListFilePath)!);
                await File.WriteAllTextAsync(LocalListFilePath, listJson);
                listDownloaded = true;
            }
            else
            {
                listJson = await File.ReadAllTextAsync(LocalListFilePath);
            }

            var list = JsonSerializer.Deserialize<EpmaIndex>(listJson, JsonOptions);
            if (list?.Templates == null) return Failed("Invalid EPMA-List.json");

            // 与收藏 / 保存等库操作串行，避免同步的读改写覆盖用户刚改的状态
            var sync = await Catalog.RunExclusiveAsync(() => SyncCatalog(list));
            if (sync.CatalogChanged) await Catalog.ReloadAsync();

            return new EpmaUpdateCheckResult
            {
                Status = EpmaUpdateCheckStatus.Success,
                ListDownloaded = listDownloaded,
                CatalogChanged = sync.CatalogChanged,
                NotInstalledCount = sync.NotInstalledCount,
                OutdatedCount = sync.OutdatedCount,
                RemovalCount = sync.RemovalCount,
                RequiresAppUpgrade = sync.RequiresAppUpgrade,
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EpmaUpdateService] check failed: {ex.Message}");
            return Failed(ex.Message);
        }

        static EpmaUpdateCheckResult Failed(string message) => new() { Status = EpmaUpdateCheckStatus.Failed, ErrorMessage = message };
    }

    /// <summary>True when the remote list hash differs from the cached list (used to ask before refreshing).</summary>
    public static async Task<(bool Success, bool ListOutdated, string? ErrorMessage)> IsRemoteListOutdatedAsync()
    {
        try
        {
            string indexJson = await OfficialContentMirrorClient.GetStringAsync(EpmaRelative(OfficialContentEndpoints.EpmaIndexFileName));
            var listIndex = JsonSerializer.Deserialize<EpmaListIndex>(indexJson, JsonOptions);
            if (listIndex == null || string.IsNullOrEmpty(listIndex.ListHash)) return (false, false, "Invalid EPMA-index.json");
            if (!File.Exists(LocalListFilePath)) return (true, true, null);
            string local = EpmaTemplateDatabaseService.ComputeHash(await File.ReadAllTextAsync(LocalListFilePath));
            return (true, !string.Equals(local, listIndex.ListHash, StringComparison.OrdinalIgnoreCase), null);
        }
        catch (Exception ex) { return (false, false, ex.Message); }
    }

    // ---------------------------------------------------------------- catalog sync

    private sealed class CatalogSyncResult
    {
        public bool CatalogChanged { get; set; }
        public int NotInstalledCount { get; set; }
        public int OutdatedCount { get; set; }
        public int RemovalCount { get; set; }
        public List<EpmaIndexEntry> RequiresAppUpgrade { get; } = new();
    }

    /// <summary>Creates placeholders, marks outdated packages and drops officials the server no longer lists.</summary>
    private static CatalogSyncResult SyncCatalog(EpmaIndex list)
    {
        var result = new CatalogSyncResult();
        var existing = Database.GetSummaries(isOfficial: true)
            .ToDictionary(e => e.PackageId, e => e, StringComparer.OrdinalIgnoreCase);
        var serverIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 清单里没有任何有效条目（空清单 / 结构异常）时不做任何删除或改写，
        // 避免一次错误发布把本地所有官方模板清空（与图解的空清单保护一致）
        if (!list.Templates.Any(t => t != null && !string.IsNullOrWhiteSpace(t.Id)))
        {
            result.NotInstalledCount = CountByStatus(EpmaInstallStatus.NotInstalled);
            result.OutdatedCount = CountByStatus(EpmaInstallStatus.Outdated);
            return result;
        }

        foreach (var entry in list.Templates.Where(t => !string.IsNullOrWhiteSpace(t.Id)))
        {
            if (!serverIds.Add(entry.Id)) continue;
            bool requiresUpgrade = entry.FormatVersion > EpmaScriptPackage.CurrentFormatVersion;
            if (requiresUpgrade) result.RequiresAppUpgrade.Add(entry);

            if (!existing.TryGetValue(entry.Id, out var summary))
            {
                Database.UpsertEntity(CreateStub(entry, requiresUpgrade ? EpmaInstallStatus.RequiresAppUpgrade : EpmaInstallStatus.NotInstalled));
                result.CatalogChanged = true;
                continue;
            }

            // 与图解 / 温压计共用同一套状态规则；EPMA 以包内整数 formatVersion 判断是否需要升级软件
            string newStatus = ContentInstallStatus.Resolve(summary.IsInstalled, requiresUpgrade,
                summary.FileHash, entry.Hash, summary.Version, entry.Version);

            bool needsWrite = !string.Equals(summary.Status, newStatus, StringComparison.Ordinal)
                || !string.Equals(summary.ServerHash, entry.Hash ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                || (!summary.IsInstalled && StubMetadataDiffers(summary, entry));
            if (!needsWrite) continue;

            var full = Database.GetEntity(summary.Id);
            if (full == null) continue;
            full.Status = newStatus;
            full.ServerHash = entry.Hash ?? string.Empty;
            // Installed packages keep the metadata of their own bytes; placeholders mirror the catalog.
            if (!full.IsInstalled) ApplyStubMetadata(full, entry);
            Database.UpsertEntity(full);
            result.CatalogChanged = true;
        }

        foreach (var local in existing.Values)
        {
            if (serverIds.Contains(local.PackageId)) continue;
            // Never-published local officials (no server hash) are kept; they are the developer's own work.
            if (local.IsInstalled && string.IsNullOrEmpty(local.ServerHash)) continue;
            Database.DeleteEntity(local.Id);
            result.RemovalCount++;
            result.CatalogChanged = true;
        }

        result.NotInstalledCount = CountByStatus(EpmaInstallStatus.NotInstalled);
        result.OutdatedCount = CountByStatus(EpmaInstallStatus.Outdated);
        return result;
    }

    private static EpmaTemplateEntity CreateStub(EpmaIndexEntry entry, string status)
    {
        var stub = new EpmaTemplateEntity
        {
            Id = EpmaTemplateDatabaseService.GenerateId(entry.Id, true),
            PackageId = entry.Id,
            FileHash = string.Empty,
            ServerHash = entry.Hash ?? string.Empty,
            Status = status,
            LastModified = DateTime.Now,
            IsOfficial = true,
            IsFavorite = false,
            PackageBytes = Array.Empty<byte>(),
        };
        ApplyStubMetadata(stub, entry);
        return stub;
    }

    private static void ApplyStubMetadata(EpmaTemplateEntity entity, EpmaIndexEntry entry)
    {
        entity.Version = string.IsNullOrWhiteSpace(entry.Version) ? "1.0.0" : entry.Version;
        entity.FormatVersion = entry.FormatVersion;
        entity.NameJson = EpmaTemplateCatalog.RawJson(entry.Name, "null");
        entity.DescriptionJson = EpmaTemplateCatalog.RawJson(entry.Description, "null");
        entity.TagsJson = TagsJson(entry);
        entity.Author = entry.Author ?? string.Empty;
        entity.UpdatedAt = entry.UpdatedAt ?? string.Empty;
        entity.Icon = string.IsNullOrEmpty(entry.Icon) ? "Σ" : entry.Icon;
        entity.LastModified = DateTime.Now;
    }

    private static bool StubMetadataDiffers(EpmaTemplateEntity stub, EpmaIndexEntry entry)
        => stub.Version != (string.IsNullOrWhiteSpace(entry.Version) ? "1.0.0" : entry.Version)
           || stub.FormatVersion != entry.FormatVersion
           || stub.NameJson != EpmaTemplateCatalog.RawJson(entry.Name, "null")
           || stub.DescriptionJson != EpmaTemplateCatalog.RawJson(entry.Description, "null")
           || stub.TagsJson != TagsJson(entry)
           || stub.Author != (entry.Author ?? string.Empty)
           || stub.UpdatedAt != (entry.UpdatedAt ?? string.Empty)
           || stub.Icon != (string.IsNullOrEmpty(entry.Icon) ? "Σ" : entry.Icon);

    private static string TagsJson(EpmaIndexEntry entry)
        => entry.Tags.ValueKind == JsonValueKind.Array ? entry.Tags.GetRawText() : "[]";

    private static int CountByStatus(string status)
        => Database.GetSummaries(isOfficial: true).Count(e => string.Equals(e.Status, status, StringComparison.Ordinal));

    // ---------------------------------------------------------------- local catalog access

    /// <summary>Cached EPMA-List.json from the last successful check.</summary>
    public static EpmaIndex? TryLoadLocalList()
    {
        try
        {
            return File.Exists(LocalListFilePath)
                ? JsonSerializer.Deserialize<EpmaIndex>(File.ReadAllText(LocalListFilePath), JsonOptions)
                : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EpmaUpdateService] read local list failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>Forgets the cached catalog so the next check downloads it again.</summary>
    public static void DeleteLocalListCache()
    {
        try { if (File.Exists(LocalListFilePath)) File.Delete(LocalListFilePath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    public static EpmaIndexEntry? FindLocalListEntry(string packageId)
        => string.IsNullOrWhiteSpace(packageId) ? null
            : TryLoadLocalList()?.Templates?.FirstOrDefault(t => string.Equals(t.Id, packageId, StringComparison.OrdinalIgnoreCase));

    public static List<EpmaIndexEntry> GetNotInstalledEntries() => GetEntriesByStatus(EpmaInstallStatus.NotInstalled);

    public static List<EpmaIndexEntry> GetOutdatedEntries() => GetEntriesByStatus(EpmaInstallStatus.Outdated);

    private static List<EpmaIndexEntry> GetEntriesByStatus(string status)
    {
        var list = TryLoadLocalList();
        if (list?.Templates == null || list.Templates.Count == 0) return new();
        var map = list.Templates.Where(t => !string.IsNullOrWhiteSpace(t.Id))
            .GroupBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        return Catalog.Current.OfficialTemplates
            .Where(e => string.Equals(e.Status, status, StringComparison.Ordinal))
            .Select(e => map.TryGetValue(e.PackageId, out var entry) ? entry : null)
            .OfType<EpmaIndexEntry>().ToList();
    }

    public static (int NotInstalled, int Outdated) GetPendingInstallCounts()
        => (CountByStatus(EpmaInstallStatus.NotInstalled), CountByStatus(EpmaInstallStatus.Outdated));

    /// <summary>Pulls a fresh list from the server (ignoring the local hash cache) and returns one entry; used for forced updates.</summary>
    public static async Task<(EpmaIndexEntry? Entry, string? ErrorMessage)> FetchFreshEntryAsync(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId)) return (null, "Package ID is empty");
        try
        {
            string indexJson = await OfficialContentMirrorClient.GetStringAsync(EpmaRelative(OfficialContentEndpoints.EpmaIndexFileName));
            var listIndex = JsonSerializer.Deserialize<EpmaListIndex>(indexJson, JsonOptions);
            if (listIndex == null || string.IsNullOrEmpty(listIndex.ListHash)) return (null, "Invalid EPMA-index.json");

            string listJson = await OfficialContentMirrorClient.GetStringAsync(EpmaRelative(OfficialContentEndpoints.EpmaListFileName));
            if (!string.Equals(EpmaTemplateDatabaseService.ComputeHash(listJson), listIndex.ListHash, StringComparison.OrdinalIgnoreCase))
                return (null, LanguageService.Instance["downloaded_file_hash_mismatch"]);

            Directory.CreateDirectory(Path.GetDirectoryName(LocalListFilePath)!);
            await File.WriteAllTextAsync(LocalListFilePath, listJson);
            var list = JsonSerializer.Deserialize<EpmaIndex>(listJson, JsonOptions);
            if (list?.Templates == null) return (null, "Invalid EPMA-List.json");
            return (list.Templates.FirstOrDefault(t => string.Equals(t.Id, packageId, StringComparison.OrdinalIgnoreCase)), null);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    // ---------------------------------------------------------------- downloads

    private static async Task DownloadPackageAsync(string downloadUrl, string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl)) throw new InvalidOperationException("Package download URL is empty.");

        if (downloadUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            string? relative = OfficialContentMirrorClient.TryGetRelativePath(downloadUrl);
            if (string.IsNullOrEmpty(relative)) await UpdateHelper.DownloadFileAsync(downloadUrl, destinationPath);
            else await OfficialContentMirrorClient.DownloadFileAsync(relative, destinationPath);
            return;
        }

        string file = downloadUrl.Trim().TrimStart('/');
        await OfficialContentMirrorClient.DownloadFileAsync(file.Contains('/') ? file : EpmaRelative(file), destinationPath);
    }

    /// <summary>Downloads, verifies (server hash, package id, script validation) and stores one official package.</summary>
    public static async Task<EpmaDownloadItemResult> DownloadAsync(EpmaIndexEntry entry)
    {
        string temporary = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + EpmaScriptPackage.Extension);
        try
        {
            if (entry.FormatVersion > EpmaScriptPackage.CurrentFormatVersion)
                return EpmaDownloadItemResult.Failed(entry.Id, LanguageService.Instance["template_version_too_high"]);

            await DownloadPackageAsync(entry.DownloadUrl, temporary);
            var package = await Task.Run(() => EpmaScriptPackage.Read(temporary));

            if (!string.IsNullOrEmpty(entry.Hash) && !string.Equals(package.Sha256, entry.Hash, StringComparison.OrdinalIgnoreCase))
                return EpmaDownloadItemResult.Failed(entry.Id, LanguageService.Instance["downloaded_file_hash_mismatch"], isHashMismatch: true);
            if (!package.Manifest.PackageId.Equals(entry.Id, StringComparison.OrdinalIgnoreCase))
                return EpmaDownloadItemResult.Failed(entry.Id, "Package id does not match the catalog entry.");

            await Catalog.ValidateAsync(package);
            await Catalog.SaveDownloadedOfficialAsync(package, entry.Hash ?? string.Empty);
            return EpmaDownloadItemResult.Succeeded(entry.Id);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EpmaUpdateService] download failed [{entry.Id}]: {ex.Message}");
            return EpmaDownloadItemResult.Failed(entry.Id, ex.Message);
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); } catch (IOException) { }
        }
    }

    /// <summary>
    /// Downloads one package; when it fails the hash check against a (possibly stale) cached catalog,
    /// pulls the latest catalog and retries once with the fresh entry (same idea as the diagram module refreshing server metadata).
    /// </summary>
    /// <param name="listRefreshed">Shared by batch downloads so the catalog is refreshed only once.</param>
    public static async Task<EpmaDownloadItemResult> DownloadWithRefreshAsync(EpmaIndexEntry entry,
        System.Runtime.CompilerServices.StrongBox<bool>? listRefreshed = null)
    {
        var result = await DownloadAsync(entry);
        if (result.Success || !result.IsHashMismatch) return result;

        EpmaIndexEntry? fresh;
        if (listRefreshed is { Value: true })
        {
            fresh = FindLocalListEntry(entry.Id);
        }
        else
        {
            (fresh, _) = await FetchFreshEntryAsync(entry.Id);
            if (listRefreshed != null) listRefreshed.Value = true;
        }

        if (fresh == null
            || (string.Equals(fresh.Hash, entry.Hash, StringComparison.OrdinalIgnoreCase)
                && string.Equals(fresh.DownloadUrl, entry.DownloadUrl, StringComparison.Ordinal)))
            return result;

        return await DownloadAsync(fresh);
    }

    public static async Task<EpmaBatchDownloadResult> DownloadAndReloadAsync(IReadOnlyList<EpmaIndexEntry> entries,
        IProgress<(int current, int total, string name)>? progress = null)
    {
        int success = 0, current = 0;
        var failures = new List<EpmaDownloadItemResult>();
        var listRefreshed = new System.Runtime.CompilerServices.StrongBox<bool>(false);
        foreach (var entry in entries)
        {
            progress?.Report((++current, entries.Count, entry.Id));
            var item = await DownloadWithRefreshAsync(entry, listRefreshed);
            if (item.Success) success++; else failures.Add(item);
        }
        if (success > 0) await Catalog.ReloadAsync();
        return new EpmaBatchDownloadResult { SuccessCount = success, Failures = failures };
    }
}
