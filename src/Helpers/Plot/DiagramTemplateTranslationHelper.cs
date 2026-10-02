using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 从图解模板提取/回写多语言翻译表格。
    /// </summary>
    public static class DiagramTemplateTranslationHelper
    {
        public const string TranslationKeyColumn = "TranslationKey";
        private const string ExportFormat = "GeoChemistryNexus.DiagramTranslation";
        private const int ExportFormatVersion = 1;

        private static readonly HashSet<string> MetadataColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "Context",
            "ObjectRef",
            TranslationKeyColumn
        };

        public static bool IsLanguageColumn(string columnName) =>
            !MetadataColumns.Contains(columnName);

        public static List<LocalizedString> CollectLocalizedStrings(GraphMapTemplate template)
        {
            var result = new List<LocalizedString>();
            if (template == null) return result;

            if (template.NodeList != null)
                result.Add(template.NodeList);

            if (template.Info?.Title?.Label != null)
                result.Add(template.Info.Title.Label);

            if (template.Info?.Texts != null)
            {
                foreach (var text in template.Info.Texts)
                {
                    if (text?.Content != null)
                        result.Add(text.Content);
                }
            }

            if (template.Info?.Axes != null)
            {
                foreach (var axis in template.Info.Axes)
                {
                    if (axis?.Label != null)
                        result.Add(axis.Label);

                    if (axis is CartesianAxisDefinition cartesian && cartesian.SubLabel != null)
                        result.Add(cartesian.SubLabel);
                }
            }

            if (template.Info?.Annotations != null)
            {
                for (int i = 0; i < template.Info.Annotations.Count; i++)
                {
                    var annotation = template.Info.Annotations[i];
                    if (annotation?.Content != null)
                        result.Add(annotation.Content);
                }
            }

            return result.Distinct().ToList();
        }

        public static DataTable BuildTranslationTable(GraphMapTemplate template, IEnumerable<string>? languageOrder = null)
        {
            var dt = new DataTable();
            dt.Columns.Add("Context", typeof(string));
            dt.Columns.Add(TranslationKeyColumn, typeof(string));
            dt.Columns.Add("ObjectRef", typeof(object));

            if (template == null)
                return dt;

            var allLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var loc in CollectLocalizedStrings(template))
            {
                if (loc?.Translations == null) continue;
                foreach (var key in loc.Translations.Keys)
                    allLanguages.Add(key);
            }

            if (!string.IsNullOrEmpty(template.DefaultLanguage))
                allLanguages.Add(template.DefaultLanguage);

            var orderedLanguages = (languageOrder ?? Enumerable.Empty<string>())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Concat(allLanguages.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var lang in orderedLanguages)
                dt.Columns.Add(lang, typeof(string));

            if (template.NodeList != null)
                AddRow(dt, LanguageService.Instance["translation_node_list"] ?? "Category Path", "categoryPath", template.NodeList, orderedLanguages);

            if (template.Info?.Title?.Label != null)
                AddRow(dt, LanguageService.Instance["translation_main_title"] ?? "Main Title", "mainTitle", template.Info.Title.Label, orderedLanguages);

            if (template.Info?.Texts != null)
            {
                for (int i = 0; i < template.Info.Texts.Count; i++)
                {
                    var textDef = template.Info.Texts[i];
                    if (textDef?.Content == null) continue;

                    string preview = GetPreviewText(textDef.Content, template.DefaultLanguage, $"Text #{i + 1}");
                    AddRow(dt, $"{LanguageService.Instance["translation_text_item"] ?? "Text"} #{i + 1} ({preview})", $"text.{i}", textDef.Content, orderedLanguages);
                }
            }

            if (template.Info?.Axes != null)
            {
                for (int i = 0; i < template.Info.Axes.Count; i++)
                {
                    var axis = template.Info.Axes[i];
                    if (axis?.Label != null)
                        AddRow(dt, $"{LanguageService.Instance["translation_axis_title"] ?? "Axis"} #{i + 1}", $"axis.{i}.label", axis.Label, orderedLanguages);

                    if (axis is CartesianAxisDefinition cartesian && cartesian.SubLabel != null)
                        AddRow(dt, $"{LanguageService.Instance["translation_axis_subtitle"] ?? "Axis Subtitle"} #{i + 1}", $"axis.{i}.sublabel", cartesian.SubLabel, orderedLanguages);
                }
            }

            if (template.Info?.Annotations != null)
            {
                for (int i = 0; i < template.Info.Annotations.Count; i++)
                {
                    var annotation = template.Info.Annotations[i];
                    if (annotation?.Content == null) continue;

                    string preview = GetPreviewText(annotation.Content, template.DefaultLanguage, $"Annotation #{i + 1}");
                    AddRow(dt, $"{LanguageService.Instance["translation_annotation"] ?? "Annotation"} #{i + 1} ({preview})", $"annotation.{i}", annotation.Content, orderedLanguages);
                }
            }

            return dt;
        }

        public static void ApplyTranslationTable(DataTable table, GraphMapTemplate template)
        {
            if (table == null || template == null) return;

            var currentLanguages = table.Columns.Cast<DataColumn>()
                .Where(c => IsLanguageColumn(c.ColumnName))
                .Select(c => c.ColumnName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in table.Rows)
            {
                if (row["ObjectRef"] is not LocalizedString locString) continue;
                locString.Translations ??= new Dictionary<string, string>();

                var keysToRemove = locString.Translations.Keys
                    .Where(k => !currentLanguages.Contains(k))
                    .ToList();
                foreach (var key in keysToRemove)
                    locString.Translations.Remove(key);

                foreach (string langKey in currentLanguages)
                {
                    string value = row[langKey] as string ?? string.Empty;
                    locString.Translations[langKey] = value;
                }
            }

            var languages = currentLanguages.ToList();
            if (languages.Count > 0)
            {
                template.DefaultLanguage = languages[0];
                foreach (var loc in CollectLocalizedStrings(template))
                {
                    if (loc != null)
                        loc.Default = template.DefaultLanguage;
                }
            }
        }

        public static void SyncTemplateLanguages(GraphMapTemplate template, IList<string> languages)
        {
            if (template == null || languages == null || languages.Count == 0) return;

            string defaultLang = languages[0];
            template.DefaultLanguage = defaultLang;

            foreach (var loc in CollectLocalizedStrings(template))
            {
                if (loc == null) continue;
                loc.Default = defaultLang;
                loc.Translations ??= new Dictionary<string, string>();

                foreach (var lang in languages)
                {
                    if (!loc.Translations.ContainsKey(lang))
                        loc.Translations[lang] = string.Empty;
                }

                var toRemove = loc.Translations.Keys
                    .Where(k => !languages.Contains(k, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                foreach (var key in toRemove)
                    loc.Translations.Remove(key);
            }
        }

        public static LocalizedString BuildCategoryNodeList(
            IList<string> languages,
            IList<CategoryPartModel> categoryParts)
        {
            var nodeList = new LocalizedString();
            foreach (var lang in languages)
            {
                var parts = new List<string>();
                foreach (var part in categoryParts)
                {
                    if (part.LocalizedNames != null && part.LocalizedNames.TryGetValue(lang, out var localized))
                        parts.Add(localized);
                    else
                        parts.Add(part.DisplayName);
                }
                nodeList.Translations[lang] = string.Join(" > ", parts);
            }

            if (languages.Count > 0)
                nodeList.Default = languages[0];

            return nodeList;
        }

        public static DataTable SyncTableLanguages(DataTable? existing, IList<string> languages, GraphMapTemplate template)
        {
            var rebuilt = BuildTranslationTable(template, languages);
            if (existing == null) return rebuilt;

            string defaultLanguage = languages.Count > 0 ? languages[0] : string.Empty;

            foreach (DataRow newRow in rebuilt.Rows)
            {
                var newRef = newRow["ObjectRef"];
                var translationKey = newRow[TranslationKeyColumn] as string;
                var isCategoryPath = string.Equals(translationKey, "categoryPath", StringComparison.OrdinalIgnoreCase);

                foreach (DataRow oldRow in existing.Rows)
                {
                    if (!ReferenceEquals(oldRow["ObjectRef"], newRef)) continue;

                    foreach (var lang in languages)
                    {
                        // 基本设置变更分类结构时，默认语言路径以重建结果为准；其他语言保留翻译表自定义内容。
                        if (isCategoryPath &&
                            !string.IsNullOrEmpty(defaultLanguage) &&
                            lang.Equals(defaultLanguage, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (existing.Columns.Contains(lang) && rebuilt.Columns.Contains(lang))
                            newRow[lang] = oldRow[lang];
                    }
                    break;
                }
            }

            return rebuilt;
        }

        public static string ExportToJson(DataTable table, IList<string> languages, string defaultLanguage)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (languages == null || languages.Count == 0)
                throw new InvalidOperationException(LanguageService.Instance["translation_export_no_languages"] ?? "No languages configured.");

            var payload = new DiagramTranslationExportPayload
            {
                Format = ExportFormat,
                FormatVersion = ExportFormatVersion,
                Languages = languages.ToList(),
                DefaultLanguage = defaultLanguage,
                Items = new List<DiagramTranslationExportItem>()
            };

            foreach (DataRow row in table.Rows)
            {
                string key = row[TranslationKeyColumn] as string ?? string.Empty;
                if (string.IsNullOrWhiteSpace(key)) continue;

                var item = new DiagramTranslationExportItem
                {
                    Key = key,
                    Context = row["Context"] as string ?? string.Empty,
                    Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                };

                foreach (var lang in languages)
                {
                    if (!table.Columns.Contains(lang)) continue;
                    item.Values[lang] = row[lang] as string ?? string.Empty;
                }

                payload.Items.Add(item);
            }

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }

        public static DiagramTranslationImportResult ImportFromJson(string json, DataTable table, IList<string> configuredLanguages)
        {
            var result = new DiagramTranslationImportResult();
            if (table == null)
            {
                result.ErrorMessage = LanguageService.Instance["translation_import_table_missing"] ?? "Translation table is not ready.";
                return result;
            }

            if (configuredLanguages == null || configuredLanguages.Count == 0)
            {
                result.ErrorMessage = LanguageService.Instance["translation_import_no_languages"] ?? "Configure languages in basic settings first.";
                return result;
            }

            DiagramTranslationExportPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<DiagramTranslationExportPayload>(json);
            }
            catch (JsonException ex)
            {
                result.ErrorMessage = $"{LanguageService.Instance["translation_import_invalid_json"] ?? "Invalid JSON file."} {ex.Message}";
                return result;
            }

            if (payload == null ||
                !string.Equals(payload.Format, ExportFormat, StringComparison.OrdinalIgnoreCase) ||
                payload.FormatVersion != ExportFormatVersion ||
                payload.Items == null)
            {
                result.ErrorMessage = LanguageService.Instance["translation_import_invalid_format"] ?? "Unsupported translation file format.";
                return result;
            }

            var configuredLangSet = configuredLanguages.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var rowsByKey = table.Rows.Cast<DataRow>()
                .Where(r => r[TranslationKeyColumn] is string key && !string.IsNullOrWhiteSpace(key))
                .ToDictionary(r => (string)r[TranslationKeyColumn], r => r, StringComparer.OrdinalIgnoreCase);

            foreach (var item in payload.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Key) || item.Values == null) continue;
                if (!rowsByKey.TryGetValue(item.Key, out var row)) continue;

                foreach (var kvp in item.Values)
                {
                    if (!configuredLangSet.Contains(kvp.Key)) continue;
                    if (!table.Columns.Contains(kvp.Key)) continue;

                    row[kvp.Key] = kvp.Value ?? string.Empty;
                    result.UpdatedCells++;
                }

                result.MatchedItems++;
            }

            result.IsSuccess = true;
            return result;
        }

        private static void AddRow(DataTable dt, string context, string translationKey, LocalizedString locString, IList<string> languages)
        {
            var row = dt.NewRow();
            row["Context"] = context;
            row[TranslationKeyColumn] = translationKey;
            row["ObjectRef"] = locString;

            foreach (var lang in languages)
            {
                if (locString.Translations != null && locString.Translations.TryGetValue(lang, out var value))
                    row[lang] = value;
                else
                    row[lang] = string.Empty;
            }

            dt.Rows.Add(row);
        }

        private static string GetPreviewText(LocalizedString content, string defaultLanguage, string fallback)
        {
            string text = AppCultureRegistry.GetLocalizedValue(
                content.Translations,
                defaultLanguage,
                content.Default);

            if (string.IsNullOrWhiteSpace(text))
                return fallback;

            return text.Length > 24 ? text.Substring(0, 24) + "…" : text;
        }
    }

    public sealed class DiagramTranslationExportPayload
    {
        public string Format { get; set; } = string.Empty;
        public int FormatVersion { get; set; }
        public List<string> Languages { get; set; } = new();
        public string DefaultLanguage { get; set; } = string.Empty;
        public List<DiagramTranslationExportItem> Items { get; set; } = new();
    }

    public sealed class DiagramTranslationExportItem
    {
        public string Key { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class DiagramTranslationImportResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int MatchedItems { get; set; }
        public int UpdatedCells { get; set; }
    }
}
