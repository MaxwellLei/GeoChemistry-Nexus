using System;
using System.Collections.Generic;
using System.Linq;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services.Epma;

namespace GeoChemistryNexus.ViewModels;

/// <summary>
/// Shared editing logic for multilingual column group headings (EPMA and geothermobarometer editors).
/// A column keeps its heading in the template's default language in <see cref="EpmaEditorColumn.Group"/> and the other languages
/// in <see cref="EpmaEditorColumn.GroupTranslations"/>. The localization table shows one row per distinct default-language heading.
/// </summary>
public static class EpmaColumnGroupEditing
{
    /// <summary>Moves a column's group heading along with the default language; a language without wording yet reuses the previous wording (like tags).</summary>
    public static void SwitchDefault(EpmaEditorColumn column, string previous, string value)
    {
        string current = column.Group.Trim();
        if (current.Length > 0) column.GroupTranslations[previous] = current;
        else column.GroupTranslations.Remove(previous);
        column.Group = column.GroupTranslations.TryGetValue(value, out var wording) && wording.Length > 0 ? wording : current;
        column.GroupTranslations.Remove(value);
    }

    /// <summary>The columns that carry a group heading, grouped by their default-language heading, in the given column order.</summary>
    public static List<(string Title, List<EpmaEditorColumn> Columns)> ByTitle(IEnumerable<EpmaEditorColumn> columns)
    {
        var result = new List<(string Title, List<EpmaEditorColumn> Columns)>();
        foreach (var column in columns)
        {
            string title = column.Group.Trim();
            if (title.Length == 0) continue;
            int index = result.FindIndex(item => item.Title == title);
            if (index < 0) result.Add((title, new List<EpmaEditorColumn> { column }));
            else result[index].Columns.Add(column);
        }
        return result;
    }

    /// <summary>Reads stored wording into a column: the default language into the group box, the others into the translations.</summary>
    public static void Load(EpmaEditorColumn column, IDictionary<string, string> wording, string defaultLanguage)
    {
        column.GroupTranslations.Clear();
        var texts = new Dictionary<string, string>(wording, StringComparer.Ordinal);
        // A heading without default wording shows its first available wording instead (like a tag).
        if (texts.Count > 0 && !texts.ContainsKey(defaultLanguage)) texts[defaultLanguage] = texts.Values.First();
        column.Group = texts.TryGetValue(defaultLanguage, out var text) ? text : "";
        foreach (var pair in texts)
            if (pair.Key != defaultLanguage) column.GroupTranslations[pair.Key] = pair.Value;
    }

    /// <summary>The column's heading per language (default language included), without empty wording.</summary>
    public static Dictionary<string, string> Wording(EpmaEditorColumn column, IEnumerable<string> languages, string defaultLanguage)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        // Without default-language wording the column has no group (the other languages only translate an existing heading).
        if (string.IsNullOrWhiteSpace(column.Group)) return result;
        foreach (string code in languages)
        {
            string text = code == defaultLanguage ? column.Group : column.GroupTranslations.TryGetValue(code, out var known) ? known : "";
            text = text?.Trim() ?? "";
            if (text.Length > 0) result[code] = text;
        }
        return result;
    }

    /// <summary>One row per distinct group heading: columns sharing a default-language heading share one set of translations.</summary>
    public static void AddSheetRows(List<EpmaLocalizationRow> rows, IReadOnlyList<CultureOption> languages, string defaultLanguage, IEnumerable<EpmaEditorColumn> columns)
    {
        var groups = ByTitle(columns);
        for (int i = 0; i < groups.Count; i++)
        {
            var row = new EpmaLocalizationRow(EpmaLocalizationRowKind.Group, EpmaLocalizer.Format("epma_loc_row_group", i + 1), languages.Count) { Tag = groups[i].Title };
            for (int column = 0; column < languages.Count; column++)
            {
                string code = languages[column].Code;
                row.Values[column] = code == defaultLanguage ? groups[i].Title
                    : groups[i].Columns.Select(item => item.GroupTranslations.TryGetValue(code, out var text) ? text : "").FirstOrDefault(text => text.Length > 0) ?? "";
            }
            rows.Add(row);
        }
    }

    /// <summary>
    /// Takes the group rows of the table back. A changed default-language title renames the group on all its columns;
    /// clearing it ungroups them in every language.
    /// </summary>
    public static void ApplySheetRows(EpmaLocalizationSheetViewModel sheet, IEnumerable<EpmaEditorColumn> columns)
    {
        // Collect the columns of every row first: renaming one group must not pull in the columns of another that now has the same title.
        var groups = ByTitle(columns);
        var changes = sheet.Rows.Where(row => row.Kind == EpmaLocalizationRowKind.Group && row.Tag is string)
            .Select(row => (Row: row, Columns: groups.FirstOrDefault(item => item.Title == (string)row.Tag!).Columns)).ToList();
        foreach (var (row, targets) in changes)
        {
            if (targets is null) continue;
            string title = row.Values[0].Trim();
            foreach (var column in targets)
            {
                if (title.Length == 0) { column.Group = ""; column.GroupTranslations.Clear(); continue; }
                column.Group = title;
                for (int language = 1; language < sheet.Languages.Count; language++)
                {
                    string code = sheet.Languages[language].Code, text = row.Values[language].Trim();
                    if (text.Length > 0) column.GroupTranslations[code] = text;
                    else column.GroupTranslations.Remove(code);
                }
            }
        }
    }

    /// <summary>True when some group heading has no wording at all in the language (a group counts as translated once any of its columns has it).</summary>
    public static bool IsMissing(IEnumerable<EpmaEditorColumn> columns, string language)
        => ByTitle(columns).Any(group => group.Columns.All(column => !column.GroupTranslations.TryGetValue(language, out string? text) || string.IsNullOrWhiteSpace(text)));
}
