using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Immutable bytes/sources/resource snapshot; a calculation never rereads an installed file.</summary>
public sealed class EpmaScriptPackage
{
    public const string Extension = TemplatePackageFileExtensions.EpmaPrimary;
    // One protocol version covers both the manifest and the synchronous script contract.
    public const int CurrentFormatVersion = 5;
    /// <summary>Oldest manifest protocol this build still reads. Version 5 only adds the optional input unit row of the editor,
    /// so version 4 packages (including every bundled template) keep working unchanged.</summary>
    public const int MinimumReadableFormatVersion = 4;
    public const int MaximumBytes = 10 * 1024 * 1024;
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 48,
    };
    private readonly byte[] _bytes;
    public EpmaScriptManifest Manifest { get; }
    public string Sha256 { get; }
    public IReadOnlyDictionary<string, string> Scripts { get; }
    public IReadOnlyList<EpmaScriptSetting> Settings { get; private set; } = [];
    public EpmaExampleDataset Examples { get; }
    public EpmaIronOptions? IronOptions { get; private set; }
    private EpmaScriptPackage(byte[] bytes, EpmaScriptManifest manifest, Dictionary<string, string> scripts,
        EpmaExampleDataset examples)
    {
        _bytes = bytes.ToArray(); Manifest = manifest; Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        Scripts = new ReadOnlyDictionary<string, string>(scripts);
        Examples = examples;
    }
    public byte[] CopyBytes() => _bytes.ToArray();
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
            if (examples.Parameters is null || examples.Inputs.Any(InvalidInput)) throw new InvalidDataException("Invalid example input.");
            var package = new EpmaScriptPackage(bytes, manifest, scripts, examples);
            package.Settings = Array.AsReadOnly(new EpmaScriptRuntime().ReadSettings(package, out var ironOptions));
            package.IronOptions = ironOptions;
            ValidateSettings(package);
            return package;
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or KeyNotFoundException or InvalidOperationException or NullReferenceException)
        { throw new InvalidDataException("Invalid EPMA package: " + ex.Message, ex); }
    }
    private static EpmaScriptManifest ReadManifest(JsonElement json)
    {
        if (!json.TryGetProperty("formatVersion", out var version) || !version.TryGetInt32(out int format))
            throw new InvalidDataException("EPMA formatVersion must be declared as an integer.");
        if(format < MinimumReadableFormatVersion || format > CurrentFormatVersion)
            throw new InvalidDataException($"Unsupported EPMA formatVersion; versions {MinimumReadableFormatVersion} to {CurrentFormatVersion} are supported.");
        return json.Deserialize<EpmaScriptManifest>(JsonOptions) ?? throw new InvalidDataException("Empty manifest.");
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
        if (string.IsNullOrWhiteSpace(m.Version) || string.IsNullOrWhiteSpace(EpmaScriptText.Format(m.Name))) throw new InvalidDataException("Missing package name/version.");
        if (m.Scripts.Length != 1 || m.Inputs.Length is 0 or > 256 || m.Outputs.Length is 0 or > 512)
            throw new InvalidDataException("Invalid declaration size.");
        Unique(m.Scripts); Unique(m.Inputs.Select(i => i.Id));
        Unique(m.Outputs.Select(o => o.Id));
        foreach (string path in m.Scripts) CheckPath(path);
        if (m.Scripts.Any(p => !p.EndsWith(".js", StringComparison.Ordinal))) throw new InvalidDataException("Incorrect script extension.");
        foreach (var i in m.Inputs) { Identifier(i.Id); if (i.Type is not ("number" or "text")) throw new InvalidDataException("Invalid input."); }
        foreach (var f in m.Outputs)
        {
            Identifier(f.Id);
            if (f.Type is not ("number" or "text")) throw new InvalidDataException("Invalid output declaration.");
        }
    }

    private static void ValidateSettings(EpmaScriptPackage package)
    {
        var m = package.Manifest;
        if (package.Settings.Count > 128) throw new InvalidDataException("Too many settings.");
        Unique(package.Settings.Select(p => p.Id));
        if (package.IronOptions is { } iron)
        {
            iron.Validate();
            var form = package.Settings.SingleOrDefault(p => p.Id == "ironForm");
            var method = package.Settings.SingleOrDefault(p => p.Id == "ironMethod");
            if (iron.Enabled)
            {
                if (!EpmaInputPolicy.IronForms(m).ToHashSet(StringComparer.Ordinal).SetEquals(iron.Forms))
                    throw new InvalidDataException("getIronOptions forms must match iron columns FeOt, Fe2O3t, or FeO together with Fe2O3.");
                if (form is null || form.Type != "choice" || !form.Choices.Select(c => c.Value).SequenceEqual(iron.Forms)
                    || form.Value.GetString() != iron.DefaultForm)
                    throw new InvalidDataException("getSettings must use the script's iron reporting options.");
                var methods = iron.Methods.Where(option => option.Enabled).Select(option => option.Id).ToArray();
                if (iron.Forms.Any(f => f != "Separated") && (method is null || method.Type != "choice"
                    || !method.Choices.Select(c => c.Value).SequenceEqual(methods) || method.Value.GetString() != iron.DefaultMethod))
                    throw new InvalidDataException("getSettings must use the script's iron treatment methods.");
            }
            else if (form is not null || method is not null) throw new InvalidDataException("Disabled iron options must not generate settings.");
        }

        void When(IReadOnlyDictionary<string, string[]>? condition)
        {
            if (condition is null) return;
            if (condition.Count != 1) throw new InvalidDataException("A condition must have exactly one controller.");
            var (key, values) = condition.Single();
            var controller = package.Settings.SingleOrDefault(p => p.Id == key && p.Type == "choice" && p.When is null)
                ?? throw new InvalidDataException("Conditions must reference an unconditional choice parameter.");
            if (values is null || values.Length == 0 || values.Any(v => !controller.Choices.Any(c => c.Value == v)))
                throw new InvalidDataException("Condition must contain an array of declared choice values.");
        }
        foreach (var i in m.Inputs) { Identifier(i.Id); if (i.Type is not ("number" or "text")) throw new InvalidDataException("Invalid input."); When(i.When); }
        foreach (var p in package.Settings)
        {
            Identifier(p.Id); When(p.When);
            if (p.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                throw new InvalidDataException("Settings need an initial value.");
            if (p.Type == "number" && (p.Value.ValueKind != JsonValueKind.Number || !double.IsFinite(p.Value.GetDouble()) || p.Value.GetDouble() < p.Min || p.Value.GetDouble() > p.Max)
                || p.Type == "choice" && (p.Value.ValueKind != JsonValueKind.String || !p.Choices.Any(c => c.Value == p.Value.GetString())))
                throw new InvalidDataException("Invalid setting value.");
            switch (p.Type)
            {
                case "number":
                    if (p.Choices.Length > 0) throw new InvalidDataException("Number parameters cannot declare choices.");
                    if (p.Min is { } min && !double.IsFinite(min) || p.Max is { } max && !double.IsFinite(max)
                        || p.Min > p.Max) throw new InvalidDataException("Invalid numeric parameter bounds.");
                    break;
                case "choice":
                    if (p.When is not null) throw new InvalidDataException("Choice parameters cannot have conditions.");
                    if (p.Min is not null || p.Max is not null) throw new InvalidDataException("Choice parameters cannot declare numeric bounds.");
                    if (p.Choices.Length == 0) throw new InvalidDataException("Empty parameter choices.");
                    if (p.Choices.Any(c => string.IsNullOrWhiteSpace(c.Value)))
                        throw new InvalidDataException("Choices need nonempty values.");
                    Unique(p.Choices.Select(c => c.Value));
                    break;
                default:
                    throw new InvalidDataException("Invalid parameter type.");
            }
        }
    }

}
