using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>A template whose package bytes were loaded from the database and parsed.</summary>
public sealed record EpmaInstalledTemplate(EpmaScriptPackage Package, bool IsCustom);
public sealed record EpmaPackageIssue(string File, string Message);

/// <summary>Lightweight catalog view: summaries only, package bytes are read on demand.</summary>
public sealed record EpmaTemplateSnapshot(IReadOnlyList<EpmaTemplateEntity> Templates, IReadOnlyList<EpmaPackageIssue> Issues)
{
    public IEnumerable<EpmaTemplateEntity> OfficialTemplates => Templates.Where(t => t.IsOfficial);
    public IEnumerable<EpmaTemplateEntity> CustomTemplates => Templates.Where(t => t.IsCustom);
}

/// <summary>
/// Local EPMA template catalog backed by LiteDB (same storage model as the diagram and geothermobarometer modules).
/// Package bytes are validated before they are written; a calculation never rereads the database.
/// </summary>
public sealed class EpmaTemplateCatalog
{
    public const string GeneralPackageId = "General";
    private readonly EpmaTemplateDatabaseService _database;
    private readonly EpmaScriptRuntime _runtime = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<EpmaPackageIssue> _startupIssues = [];

    public EpmaTemplateDatabaseService Database => _database;
    public EpmaTemplateSnapshot Current { get; private set; }

    /// <param name="database">LiteDB store for all templates.</param>
    public EpmaTemplateCatalog(EpmaTemplateDatabaseService database)
    {
        _database = database;
        Current = Load();
    }

    /// <summary>Synchronous reload; only for startup / non-UI callers (tests). UI code should use <see cref="ReloadAsync"/>.</summary>
    public void Reload() { _gate.Wait(); try { Current = Load(); } finally { _gate.Release(); } }

    /// <summary>Reloads the catalog snapshot on a worker thread without blocking the caller.</summary>
    public async Task ReloadAsync(CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try { Current = await Task.Run(Load, token); }
        finally { _gate.Release(); }
    }

    /// <summary>Runs a database read-modify-write on a worker thread while holding the catalog gate (serialised with favourites, saves and installs).</summary>
    internal async Task<T> RunExclusiveAsync<T>(Func<T> action, CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try { return await Task.Run(action, token); }
        finally { _gate.Release(); }
    }

    /// <summary>Developer action: wipes every stored template.</summary>
    public async Task ResetDatabaseAsync(CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try
        {
            await Task.Run(() =>
            {
                _database.ClearDatabase();
                Current = Load();
            }, token);
        }
        finally { _gate.Release(); }
    }

    private EpmaTemplateSnapshot Load()
    {
        try { return new(_database.GetSummaries().ToArray(), _startupIssues.ToArray()); }
        catch (Exception ex)
        {
            return new([], _startupIssues.Append(new EpmaPackageIssue("Epma.db", ex.Message)).ToArray());
        }
    }

    // ---------------------------------------------------------------- reading

    /// <summary>Reads and parses the stored package of a summary; not-downloaded placeholders are rejected.</summary>
    public EpmaInstalledTemplate LoadInstalled(EpmaTemplateEntity summary)
    {
        var entity = _database.GetEntity(summary.Id) ?? throw new InvalidOperationException(EpmaLocalizer.Format("epma_library_changed"));
        return ToInstalled(entity);
    }

    public bool TryLoadInstalled(EpmaTemplateEntity summary, out EpmaInstalledTemplate? template, out string? error)
    {
        try { template = LoadInstalled(summary); error = null; return true; }
        catch (Exception ex) { template = null; error = ex.Message; return false; }
    }

    private static EpmaInstalledTemplate ToInstalled(EpmaTemplateEntity entity)
    {
        if (!entity.HasPackage) throw new InvalidOperationException(EpmaLocalizer.Format("epma_library_not_downloaded"));
        var package = EpmaScriptPackage.Read(entity.PackageBytes);
        if (!package.Manifest.PackageId.Equals(entity.PackageId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Stored package id does not match its catalog record.");
        return new(package, entity.IsCustom);
    }

    public bool Exists(string packageId, bool official)
        => _database.GetSummary(EpmaTemplateDatabaseService.GenerateId(packageId, official)) is not null;

    // ---------------------------------------------------------------- personal templates

    public async Task<EpmaInstalledTemplate> ImportAsync(string path,
        Func<EpmaInstalledTemplate, EpmaScriptPackage, Task<bool>> confirmReplacement, CancellationToken token = default)
        => await InstallAsync(EpmaScriptPackage.Read(path), confirmReplacement, token);

    public Task<EpmaInstalledTemplate> CreateAsync(EpmaScriptPackage package, CancellationToken token = default)
        => InstallAsync(package, (_, _) => throw new InvalidOperationException(EpmaLocalizer.Format("epma_editor_id_exists")), token, true);

    private async Task<EpmaInstalledTemplate> InstallAsync(EpmaScriptPackage package,
        Func<EpmaInstalledTemplate, EpmaScriptPackage, Task<bool>> confirmReplacement, CancellationToken token, bool creating = false)
    {
        string id = package.Manifest.PackageId;
        await Task.Run(() => _runtime.ValidateAsync(package, token), token);
        token.ThrowIfCancellationRequested();
        await _gate.WaitAsync(token);
        try
        {
            var custom = _database.GetEntity(id, false);
            var official = _database.GetEntity(id, true);
            if (creating && (custom is not null || official is not null))
                throw new InvalidOperationException(EpmaLocalizer.Format("epma_editor_id_exists"));
            // A personal package shadows the official one with the same id; an empty placeholder never conflicts.
            var existing = custom ?? (official is { HasPackage: true } ? official : null);
            if (existing is not null)
            {
                if (string.Equals(existing.FileHash, package.Sha256, StringComparison.OrdinalIgnoreCase))
                    return new(package, existing.IsCustom);
                if (!await confirmReplacement(ToInstalled(existing), package))
                    throw new OperationCanceledException("Package replacement declined.");
                token.ThrowIfCancellationRequested();
            }
            _database.UpsertEntity(CreateEntity(package, false, custom));
            Current = Load();
            return new(package, true);
        }
        finally { _gate.Release(); }
    }

    public async Task<EpmaInstalledTemplate> UpdateAsync(EpmaInstalledTemplate original, EpmaScriptPackage package,
        bool allowOfficial = false, CancellationToken token = default)
    {
        if (!original.IsCustom && !allowOfficial) throw new InvalidOperationException(EpmaLocalizer.Format("epma_editor_readonly_hint"));
        if (!original.Package.Manifest.PackageId.Equals(package.Manifest.PackageId, StringComparison.Ordinal))
            throw new InvalidOperationException(EpmaLocalizer.Format("epma_editor_fixed_id"));
        await Task.Run(() => _runtime.ValidateAsync(package, token), token);
        await _gate.WaitAsync(token);
        try
        {
            var stored = VerifyCurrent(original);
            token.ThrowIfCancellationRequested();
            var updated = CreateEntity(package, !original.IsCustom, stored);
            if (updated.IsOfficial) updated.Status = OfficialStatus(updated);
            _database.UpsertEntity(updated);
            Current = Load();
            return new(package, original.IsCustom);
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteAsync(EpmaInstalledTemplate item, CancellationToken token = default)
    {
        if (!item.IsCustom) throw new InvalidOperationException(EpmaLocalizer.Format("epma_library_official_delete_denied"));
        await _gate.WaitAsync(token);
        try
        {
            var stored = VerifyCurrent(item);
            token.ThrowIfCancellationRequested();
            _database.DeleteEntity(stored.Id);
            Current = Load();
        }
        finally { _gate.Release(); }
    }

    public Task ExportAsync(EpmaInstalledTemplate item, string path, CancellationToken token = default)
        => WriteAtomicAsync(Path.GetFullPath(path), item.Package.CopyBytes(), token);

    private EpmaTemplateEntity VerifyCurrent(EpmaInstalledTemplate item)
    {
        var stored = _database.GetEntity(item.Package.Manifest.PackageId, !item.IsCustom);
        if (stored is null || !stored.HasPackage || !string.Equals(stored.FileHash, item.Package.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(EpmaLocalizer.Format("epma_library_changed"));
        return stored;
    }

    public async Task SetFavoriteAsync(EpmaTemplateEntity item, bool favorite, CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try
        {
            await Task.Run(() =>
            {
                var stored = _database.GetEntity(item.Id);
                if (stored is null || stored.IsFavorite == favorite) return;
                stored.IsFavorite = favorite;
                _database.UpsertEntity(stored);
                Current = Load();
            }, token);
        }
        finally { _gate.Release(); }
    }

    // ---------------------------------------------------------------- official promotion

    /// <summary>Developer action: turn a personal template into an official one (publishable, replaces a same-id official).</summary>
    public async Task<EpmaTemplateEntity> PromoteToOfficialAsync(EpmaTemplateEntity custom, CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try
        {
            var source = _database.GetEntity(custom.Id);
            if (source is not { IsCustom: true, HasPackage: true }) throw new InvalidOperationException(EpmaLocalizer.Format("epma_library_changed"));
            var previous = _database.GetEntity(source.PackageId, true);
            var promoted = CreateEntity(EpmaScriptPackage.Read(source.PackageBytes), true, previous);
            promoted.IsFavorite = previous?.IsFavorite ?? source.IsFavorite;
            promoted.Status = OfficialStatus(promoted);
            _database.UpsertEntity(promoted);
            _database.DeleteEntity(source.Id);
            Current = Load();
            return promoted;
        }
        finally { _gate.Release(); }
    }

    /// <summary>Developer action: turn a downloaded official template into a personal one (replaces a same-id personal template).</summary>
    public async Task<EpmaTemplateEntity> DemoteToCustomAsync(EpmaTemplateEntity official, CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try
        {
            var source = _database.GetEntity(official.Id);
            if (source is not { IsOfficial: true, HasPackage: true }) throw new InvalidOperationException(EpmaLocalizer.Format("epma_library_changed"));
            var previous = _database.GetEntity(source.PackageId, false);
            var demoted = CreateEntity(EpmaScriptPackage.Read(source.PackageBytes), false, previous);
            demoted.IsFavorite = previous?.IsFavorite ?? source.IsFavorite;
            _database.UpsertEntity(demoted);
            _database.DeleteEntity(source.Id);
            Current = Load();
            return demoted;
        }
        finally { _gate.Release(); }
    }

    // ---------------------------------------------------------------- server downloads

    /// <summary>Stores a downloaded and validated official package; the caller has verified it against the server catalog.</summary>
    internal async Task SaveDownloadedOfficialAsync(EpmaScriptPackage package, string serverHash, CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try
        {
            var existing = _database.GetEntity(package.Manifest.PackageId, true);
            var entity = CreateEntity(package, true, existing);
            entity.ServerHash = string.IsNullOrEmpty(serverHash) ? package.Sha256 : serverHash;
            entity.Status = EpmaInstallStatus.UpToDate;
            _database.UpsertEntity(entity);
            Current = Load();
        }
        finally { _gate.Release(); }
    }

    /// <summary>After a successful publish the local officials count as published, so catalog syncs may withdraw them later.</summary>
    internal async Task MarkPublishedAsync(IEnumerable<(string PackageId, string Hash)> published, CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try
        {
            foreach (var (packageId, hash) in published)
            {
                var entity = _database.GetEntity(packageId, true);
                if (entity is null || !entity.HasPackage || string.Equals(entity.ServerHash, hash, StringComparison.OrdinalIgnoreCase)) continue;
                entity.ServerHash = hash;
                entity.Status = OfficialStatus(entity);
                _database.UpsertEntity(entity);
            }
            Current = Load();
        }
        finally { _gate.Release(); }
    }

    internal Task ValidateAsync(EpmaScriptPackage package, CancellationToken token = default)
        => Task.Run(() => _runtime.ValidateAsync(package, token), token);

    // ---------------------------------------------------------------- entity helpers

    internal static string OfficialStatus(EpmaTemplateEntity entity)
        => string.IsNullOrEmpty(entity.ServerHash) || string.Equals(entity.FileHash, entity.ServerHash, StringComparison.OrdinalIgnoreCase)
            ? EpmaInstallStatus.UpToDate : EpmaInstallStatus.Outdated;

    internal static EpmaTemplateEntity CreateEntity(EpmaScriptPackage package, bool isOfficial, EpmaTemplateEntity? existing = null)
    {
        var m = package.Manifest;
        return new EpmaTemplateEntity
        {
            Id = EpmaTemplateDatabaseService.GenerateId(m.PackageId, isOfficial),
            PackageId = m.PackageId,
            Version = m.Version,
            FormatVersion = m.FormatVersion,
            FileHash = package.Sha256,
            LastModified = DateTime.Now,
            IsOfficial = isOfficial,
            IsFavorite = existing?.IsFavorite ?? false,
            Status = isOfficial ? EpmaInstallStatus.UpToDate : string.Empty,
            ServerHash = isOfficial ? existing?.ServerHash ?? string.Empty : string.Empty,
            NameJson = RawJson(m.Name, "null"),
            DescriptionJson = RawJson(m.Description, "null"),
            TagsJson = JsonSerializer.Serialize(m.Tags),
            Author = m.Author ?? string.Empty,
            UpdatedAt = m.UpdatedAt ?? string.Empty,
            Icon = m.Icon,
            PackageBytes = package.CopyBytes(),
        };
    }

    internal static string RawJson(JsonElement element, string fallback)
        => element.ValueKind is JsonValueKind.Undefined ? fallback : element.GetRawText();

    private static async Task WriteAtomicAsync(string target, byte[] bytes, CancellationToken token)
    {
        string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, token);
            token.ThrowIfCancellationRequested(); File.Move(temporary, target, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
