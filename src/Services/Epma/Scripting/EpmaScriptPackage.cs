using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Immutable bytes/sources/resource snapshot; a calculation never rereads an installed file.</summary>
public sealed class EpmaScriptPackage
{
    public const string Extension = TemplatePackageFileExtensions.EpmaPrimary;
    public const int MaximumBytes = 10 * 1024 * 1024;
    /// <summary>
    /// Id of the first input of every template: a text column that only names the sample of a row (same convention as the geothermometers).
    /// It never takes part in the calculation, and a row holding nothing but a sample name counts as empty.
    /// </summary>
    public const string SampleInputId = "Sample";
    /// <summary>
    /// Unknown properties are skipped everywhere (manifest, flags, script results, examples), so a package written for a newer
    /// build can still be read by this one as long as the fields it adds are optional presentation data.
    /// Anything that changes calculation semantics must instead be declared in <c>requires</c> (see <see cref="SupportedCapabilities"/>).
    /// </summary>
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip, MaxDepth = 48,
    };
    /// <summary>Capability names this build understands. A package listing any other name in <c>requires</c> is refused with an upgrade message.</summary>
    public static readonly IReadOnlySet<string> SupportedCapabilities = new HashSet<string>(StringComparer.Ordinal);
    /// <summary>
    /// True when this build cannot run a package: its version "x.y.z" has an x.y above the EPMA module version of the app
    /// (<c>EpmaFormatVersion</c> in the project file), or it requires a capability this build does not know.
    /// The package revision (z) never matters.
    /// </summary>
    public static bool RequiresAppUpgrade(string? version, IEnumerable<string>? requires)
        => ContentVersionHelper.RequiresAppUpgrade(version, ContentVersionHelper.GetEpmaFormatVersion())
           || (requires ?? []).Any(capability => !SupportedCapabilities.Contains(capability));
    private readonly byte[] _bytes;
    public EpmaScriptManifest Manifest { get; }
    public string Sha256 { get; }
    public IReadOnlyDictionary<string, string> Scripts { get; }
    public EpmaExampleDataset Examples { get; }
    private EpmaScriptPackage(byte[] bytes, EpmaScriptManifest manifest, Dictionary<string, string> scripts,
        EpmaExampleDataset examples)
    {
        _bytes = bytes.ToArray(); Manifest = manifest; Sha256 = HashOf(bytes);
        Scripts = new ReadOnlyDictionary<string, string>(scripts);
        Examples = examples;
    }
    public byte[] CopyBytes() => _bytes.ToArray();

    /// <summary>SHA-256 of the package bytes (lower-case hex), the same value as <see cref="Sha256"/> of a package read from them.</summary>
    public static string HashOf(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    /// <summary>
    /// Rewrites only <c>packageId</c> in the manifest and copies every other entry byte for byte.
    /// The package is not parsed or validated beyond the new identifier, so a package that needs a newer app
    /// (and therefore cannot be read here) can still move between the official and personal catalogs.
    /// </summary>
    internal static byte[] WithPackageId(byte[] bytes, string newPackageId)
    {
        Identifier(newPackageId);
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("EPMA package exceeds 10 MiB.");
        using var source = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        using var output = new MemoryStream();
        using (var target = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            bool rewritten = false;
            foreach (var entry in source.Entries)
            {
                using var input = entry.Open();
                using var buffer = new MemoryStream();
                input.CopyTo(buffer);
                byte[] content = buffer.ToArray();
                if (entry.FullName == "package.json")
                {
                    using var reader = new StreamReader(new MemoryStream(content), Encoding.UTF8, true);
                    var manifest = JsonNode.Parse(reader.ReadToEnd())?.AsObject() ?? throw new InvalidDataException("Empty manifest.");
                    manifest["packageId"] = newPackageId;
                    content = new UTF8Encoding(false).GetBytes(manifest.ToJsonString(PrettyJson));
                    rewritten = true;
                }
                using var stream = target.CreateEntry(entry.FullName).Open();
                stream.Write(content);
            }
            if (!rewritten) throw new InvalidDataException("The package has no manifest.");
        }
        return output.ToArray();
    }
    public static EpmaScriptPackage Read(string path)
    {
        if (new FileInfo(path).Length > MaximumBytes) throw new InvalidDataException("EPMA package exceeds 10 MiB.");
        return Read(File.ReadAllBytes(path));
    }
    public static EpmaScriptPackage Read(byte[] bytes)
    {
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("EPMA package exceeds 10 MiB.");
        try
        {
            using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
            if (zip.Entries.Count is 0 or > 64 || zip.Entries.Sum(e => e.Length) > MaximumBytes) throw new InvalidDataException("Expanded package exceeds limits.");
            var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            long total = 0;
            foreach (var entry in zip.Entries)
            {
                CheckPath(entry.FullName);
                using var stream = entry.Open(); using var memory = new MemoryStream();
                byte[] buffer = new byte[8192]; int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    total += read; if (total > MaximumBytes) throw new InvalidDataException("Expanded package exceeds 10 MiB.");
                    memory.Write(buffer, 0, read);
                }
                if (!files.TryAdd(entry.FullName, memory.ToArray())) throw new InvalidDataException("Duplicate ZIP path.");
            }
            byte[] manifestBytes = files["package.json"];
            if (manifestBytes.Length > 512 * 1024) throw new InvalidDataException("Manifest exceeds 512 KiB.");
            var manifest = ReadManifest(Parse(manifestBytes));
            ValidateManifest(manifest);
            var declared = manifest.Scripts.Concat(new[] { "package.json", "examples.json" }).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (files.Keys.Any(f => !declared.Contains(f))) throw new InvalidDataException("ZIP contains an undeclared file.");
            var scripts = manifest.Scripts.ToDictionary(p => p, p => new UTF8Encoding(false, true).GetString(files[p]), StringComparer.Ordinal);
            var examples = Parse(files["examples.json"]).Deserialize<EpmaExampleDataset>(JsonOptions) ?? throw new InvalidDataException("Invalid example dataset.");
            if (examples.Inputs is null || examples.Inputs.Length is 0 or > 100) throw new InvalidDataException("Provide 1–100 examples.");
            bool InvalidInput(Dictionary<string, JsonElement>? input) => input is null || input.Any(p =>
                !manifest.Inputs.Any(i => i.Id == p.Key) || p.Value.ValueKind is not (JsonValueKind.Number or JsonValueKind.String or JsonValueKind.Null));
            if (examples.Inputs.Any(InvalidInput)) throw new InvalidDataException("Invalid example input.");
            return new EpmaScriptPackage(bytes, manifest, scripts, examples);
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or KeyNotFoundException or InvalidOperationException or NullReferenceException)
        { throw new InvalidDataException("Invalid EPMA package: " + ex.Message, ex); }
    }
    private static EpmaScriptManifest ReadManifest(JsonElement json)
    {
        var manifest = json.Deserialize<EpmaScriptManifest>(JsonOptions) ?? throw new InvalidDataException("Empty manifest.");
        if (manifest.Requires is null || manifest.Requires.Length > 32 || manifest.Requires.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Invalid requires list.");
        if (RequiresAppUpgrade(manifest.Version, manifest.Requires))
            throw new InvalidDataException("This package needs a newer version of the application (package version " + manifest.Version
                + ", application EPMA version " + ContentVersionHelper.FormatBaseline(ContentVersionHelper.GetEpmaFormatVersion())
                + (manifest.Requires.Length > 0 ? ", requires " + string.Join(", ", manifest.Requires) : "") + ").");
        return manifest;
    }
    private static JsonElement Parse(byte[] bytes)
    {
        var data = bytes.AsSpan(); if (data.StartsWith(new byte[] { 239, 187, 191 })) data = data[3..];
        using var doc = JsonDocument.Parse(data.ToArray(), new JsonDocumentOptions { MaxDepth = 48 });
        CheckJson(doc.RootElement); return doc.RootElement.Clone();
    }
    private static void CheckJson(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in value.EnumerateObject()) { if (!seen.Add(p.Name)) throw new InvalidDataException("Duplicate JSON key: " + p.Name); CheckJson(p.Value); }
        }
        else if (value.ValueKind == JsonValueKind.Array) foreach (var v in value.EnumerateArray()) CheckJson(v);
        else if (value.ValueKind == JsonValueKind.Number && (!value.TryGetDouble(out double d) || !double.IsFinite(d))) throw new InvalidDataException("Nonfinite JSON number.");
    }
    private static void CheckPath(string path)
    {
        if (path.Length > 180 || path.Contains('\\') || path.Contains(':') || path.StartsWith('/') || path.Any(c => char.IsControl(c))
            || path.Split('/').Any(p => p is "" or "." or "..")
            || Path.GetExtension(path) is not (".json" or ".js")) throw new InvalidDataException("Unsafe or unsupported ZIP path: " + path);
    }
    private static void Identifier(string id) { if (id is null || !Regex.IsMatch(id, @"\A[A-Za-z][A-Za-z0-9_.-]{0,79}\z")) throw new InvalidDataException("Invalid identifier: " + id); }
    private static void Unique(IEnumerable<string> ids) { if (ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count()) throw new InvalidDataException("Duplicate identifiers."); }
    private static void ValidateManifest(EpmaScriptManifest m)
    {
        Identifier(m.PackageId);
        if (m.Icon is null || EpmaTemplateIcon.Normalize(m.Icon) != m.Icon)
            throw new InvalidDataException("A template icon must contain at most three plain-text characters.");
        if(m.Tags.Length>16)throw new InvalidDataException("Too many search tags.");
        EpmaHelpDocuments.Validate(m.Help);
        if (string.IsNullOrWhiteSpace(m.Version) || !Regex.IsMatch(m.Version, @"\A\d{1,4}\.\d{1,4}\.\d{1,4}\z"))
            throw new InvalidDataException("The package version must be \"x.y.z\": x.y is the EPMA module version, z the package revision.");
        if (string.IsNullOrWhiteSpace(EpmaScriptText.Format(m.Name))) throw new InvalidDataException("Missing package name.");
        if (m.Scripts.Length != 1 || m.Inputs.Length is 0 or > 256 || m.Outputs.Length is 0 or > 512)
            throw new InvalidDataException("Invalid declaration size.");
        if (m.Inputs[0].Id != SampleInputId || m.Inputs[0].Type != "text")
            throw new InvalidDataException("The first input must be the text column \"" + SampleInputId + "\".");
        Unique(m.Scripts); Unique(m.Inputs.Select(i => i.Id));
        Unique(m.Outputs.Select(o => o.Id));
        foreach (string path in m.Scripts) CheckPath(path);
        if (m.Scripts.Any(p => !p.EndsWith(".js", StringComparison.Ordinal))) throw new InvalidDataException("Incorrect script extension.");
        foreach (var i in m.Inputs)
        {
            Identifier(i.Id);
            if (i.Type is not ("number" or "text")) throw new InvalidDataException("Invalid input.");
            if (!EpmaScriptText.ValidGroup(i.Group)) throw new InvalidDataException("Invalid input group.");
        }
        foreach (var f in m.Outputs)
        {
            Identifier(f.Id);
            if (f.Type is not ("number" or "text")) throw new InvalidDataException("Invalid output declaration.");
            if (!EpmaScriptText.ValidGroup(f.Group)) throw new InvalidDataException("Invalid output group.");
        }
    }

}
