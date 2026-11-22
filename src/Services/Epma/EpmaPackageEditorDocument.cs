using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GeoChemistryNexus.Services.Epma;

public partial class EpmaEditableFile : ObservableObject
{
    private readonly string _displayKey;
    private readonly int _displayIndex;
    public string Path { get; }
    public string DisplayName => EpmaLocalizer.Format(_displayKey) + (_displayIndex > 0 ? " " + _displayIndex : "");
    public bool IsScript => Path.EndsWith(".js", StringComparison.Ordinal);
    public bool IsJson => !IsScript;
    [ObservableProperty] private string _text;
    public EpmaEditableFile(string path, string text, string? displayKey = null, int displayIndex = 0)
    {
        Path = path; _text = text;
        _displayKey = displayKey ?? (IsScript ? "epma_editor_file_calculation" : "epma_json_field_resource");
        _displayIndex = displayIndex;
    }
    public string CaptureText() => Text;
}

/// <summary>Preserves the script/example and every untranslated metadata value.</summary>
public sealed class EpmaPackageEditorDocument
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
    private readonly EpmaScriptPackage _original;
    private readonly Dictionary<string, byte[]> _bytes = new(StringComparer.Ordinal);
    public IReadOnlyList<EpmaEditableFile> Files { get; }
    public string DefinitionJson { get; }
    public EpmaPackageEditorDocument(EpmaScriptPackage package)
    {
        _original = package;
        using var zip = new ZipArchive(new MemoryStream(package.CopyBytes()));
        foreach (var entry in zip.Entries)
        {
            using var memory = new MemoryStream(); using var stream = entry.Open(); stream.CopyTo(memory);
            _bytes.Add(entry.FullName, memory.ToArray());
        }
        string DisplayKey(string path)
        {
            if (path == "examples.json") return "epma_json_field_examples";
            if (path == package.Manifest.Scripts.Last()) return "epma_editor_file_calculation";
            if (path.EndsWith(".js", StringComparison.Ordinal)) return System.IO.Path.GetFileName(path) switch
            {
                "common.js" => "epma_editor_file_common", "chemistry.js" => "epma_editor_file_chemistry_script",
                _ => "epma_editor_file_helper"
            };
            return "epma_editor_file_calculation";
        }
        var paths = _bytes.Keys.Where(path => path != "package.json").ToArray();
        var labels = paths.GroupBy(DisplayKey).SelectMany(group => group.Select((path, index) =>
            (Path: path, Key: group.Key, Index: group.Count() > 1 ? index + 1 : 0))).ToDictionary(label => label.Path);
        var manifest = JsonNode.Parse(Decode(_bytes["package.json"]))!.AsObject();
        Files = _bytes.Where(p => p.Key != "package.json")
            .OrderBy(p => p.Key.EndsWith(".js", StringComparison.Ordinal) ? 0 : 1).ThenBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new EpmaEditableFile(p.Key, Decode(p.Value), labels[p.Key].Key, labels[p.Key].Index)).ToArray();
        var definitions = new JsonObject();
        foreach (string key in new[] { "inputs", "outputs" })
            definitions[key] = manifest[key]?.DeepClone() ?? new JsonArray();
        DefinitionJson = definitions.ToJsonString(Pretty);
    }
    private static string Decode(byte[] bytes)
    {
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, true);
        return reader.ReadToEnd();
    }
    public EpmaScriptPackage Build(string name, string version, string author, string description, string tags, string definitions,
        string? packageId = null, EpmaBasicLocalization? localization = null, IReadOnlyDictionary<string, string>? helpDocuments = null,
        string? icon = null)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version) || packageId is not null && string.IsNullOrWhiteSpace(packageId))
            throw new InvalidDataException(EpmaLocalizer.Format("epma_editor_required"));
        var manifest = JsonNode.Parse(Decode(_bytes["package.json"]))!.AsObject();
        if (packageId is not null) manifest["packageId"] = packageId.Trim();
        if (icon is not null && icon != _original.Manifest.Icon) manifest["icon"] = EpmaTemplateIcon.Normalize(icon);
        void Text(string key, string value, string previous)
        {
            if (value == previous) return;
            if (manifest[key] is JsonObject translations) translations[CultureInfo.CurrentUICulture.Name] = value;
            else manifest[key] = value;
        }
        if (version.Trim() != _original.Manifest.Version) manifest["version"] = version.Trim();
        if (author.Trim() != (_original.Manifest.Author ?? "")) manifest["author"] = author.Trim();
        if (localization is not null) localization.Apply(manifest, _original.Manifest);
        else
        {
            Text("name", name.Trim(), EpmaScriptText.Format(_original.Manifest.Name));
            Text("description", description.Trim(), EpmaScriptText.Format(_original.Manifest.Description));
            if (tags != string.Join(", ", _original.Manifest.Tags.Select(EpmaScriptText.Format)))
                manifest["tags"] = JsonSerializer.SerializeToNode(tags.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        // Help documents are rewritten only when they differ, so untouched packages keep their original bytes.
        if (helpDocuments is not null && !EpmaHelpDocuments.Same(EpmaHelpDocuments.Read(_original.Manifest.Help), helpDocuments))
        {
            if (helpDocuments.Count == 0) manifest.Remove("help");
            else manifest["help"] = EpmaHelpDocuments.ToNode(helpDocuments);
        }
        var fields = JsonNode.Parse(definitions)?.AsObject() ?? throw new InvalidDataException("Invalid field definitions.");
        if (fields.Any(p => p.Key is not ("inputs" or "outputs")) || !fields.ContainsKey("inputs") || !fields.ContainsKey("outputs"))
            throw new InvalidDataException("Definitions must contain only inputs and outputs.");
        foreach (string key in new[] { "inputs", "outputs" }) manifest[key] = fields[key]?.DeepClone() ?? new JsonArray();
        // Packages no longer have settings; an old package opened in the editor drops them when saved.
        manifest.Remove("settings");
        manifest["updatedAt"] = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        using var memory = new MemoryStream();
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            foreach (var (path, originalBytes) in _bytes)
            {
                byte[] content;
                if (path == "package.json") content = Encoding.UTF8.GetBytes(manifest.ToJsonString(Pretty));
                else
                {
                    string edited = Files.Single(f => f.Path == path).CaptureText();
                    content = edited == Decode(originalBytes) ? originalBytes : Encoding.UTF8.GetBytes(edited);
                }
                using var stream = zip.CreateEntry(path).Open(); stream.Write(content);
            }
        }
        return EpmaScriptPackage.Read(memory.ToArray());
    }

}
