using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GeoChemistryNexus.Services.Epma;

public sealed record EpmaRecentTemplate(string Id, bool IsCustom);

public sealed class EpmaRecentTemplates
{
    private readonly string _path;
    public IReadOnlyList<EpmaRecentTemplate> Items { get; private set; } = [];
    private readonly int _limit;
    public EpmaRecentTemplates(string directory, string fileName = "recent-templates.json", int limit = 8)
    {
        _limit = limit;
        _path = Path.Combine(directory, fileName);
        try
        {
            if (File.Exists(_path)) Items = (JsonSerializer.Deserialize<EpmaRecentTemplate[]>(File.ReadAllText(_path)) ?? [])
                .Where(i => i is not null && !string.IsNullOrWhiteSpace(i.Id)).Distinct().Take(_limit).ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
    }
    public bool Contains(string id, bool isCustom)
        => Items.Any(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && i.IsCustom == isCustom);
    public void Record(string id, bool isCustom)
    {
        var entry = new EpmaRecentTemplate(id, isCustom);
        Items = new[] { entry }.Concat(Items.Where(i => !i.Id.Equals(id, StringComparison.OrdinalIgnoreCase) || i.IsCustom != isCustom)).Take(_limit).ToArray();
        Save();
    }
    public void Remove(string id, bool isCustom)
    {
        Items = Items.Where(i => !i.Id.Equals(id, StringComparison.OrdinalIgnoreCase) || i.IsCustom != isCustom).ToArray();
        Save();
    }
    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        string temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(Items)); File.Move(temporary, _path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
