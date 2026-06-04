using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 从图解模板提取/回写多语言翻译表格。
    /// </summary>
    public static class DiagramTemplateTranslationHelper
    {
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
                AddRow(dt, LanguageService.Instance["translation_node_list"] ?? "Category Path", template.NodeList, orderedLanguages);

            if (template.Info?.Title?.Label != null)
                AddRow(dt, LanguageService.Instance["translation_main_title"] ?? "Main Title", template.Info.Title.Label, orderedLanguages);

            if (template.Info?.Texts != null)
            {
                for (int i = 0; i < template.Info.Texts.Count; i++)
                {
                    var textDef = template.Info.Texts[i];
                    if (textDef?.Content == null) continue;

                    string preview = GetPreviewText(textDef.Content, template.DefaultLanguage, $"Text #{i + 1}");
                    AddRow(dt, $"{LanguageService.Instance["translation_text_item"] ?? "Text"} #{i + 1} ({preview})", textDef.Content, orderedLanguages);
                }
            }

            if (template.Info?.Axes != null)
            {
                for (int i = 0; i < template.Info.Axes.Count; i++)
                {
                    var axis = template.Info.Axes[i];
                    if (axis?.Label != null)
                        AddRow(dt, $"{LanguageService.Instance["translation_axis_title"] ?? "Axis"} #{i + 1}", axis.Label, orderedLanguages);

                    if (axis is CartesianAxisDefinition cartesian && cartesian.SubLabel != null)
                        AddRow(dt, $"{LanguageService.Instance["translation_axis_subtitle"] ?? "Axis Subtitle"} #{i + 1}", cartesian.SubLabel, orderedLanguages);
                }
            }

            if (template.Info?.Annotations != null)
            {
                for (int i = 0; i < template.Info.Annotations.Count; i++)
                {
                    var annotation = template.Info.Annotations[i];
                    if (annotation?.Content == null) continue;

                    string preview = GetPreviewText(annotation.Content, template.DefaultLanguage, $"Annotation #{i + 1}");
                    AddRow(dt, $"{LanguageService.Instance["translation_annotation"] ?? "Annotation"} #{i + 1} ({preview})", annotation.Content, orderedLanguages);
                }
            }

            return dt;
        }

        public static void ApplyTranslationTable(DataTable table, GraphMapTemplate template)
        {
            if (table == null || template == null) return;

            var currentLanguages = table.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName is not "Context" and not "ObjectRef")
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
            IList<GeoChemistryNexus.Controls.CategoryPartModel> categoryParts)
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

            foreach (DataRow newRow in rebuilt.Rows)
            {
                var newRef = newRow["ObjectRef"];
                foreach (DataRow oldRow in existing.Rows)
                {
                    if (!ReferenceEquals(oldRow["ObjectRef"], newRef)) continue;

                    foreach (var lang in languages)
                    {
                        if (existing.Columns.Contains(lang) && rebuilt.Columns.Contains(lang))
                            newRow[lang] = oldRow[lang];
                    }
                    break;
                }
            }

            return rebuilt;
        }

        private static void AddRow(DataTable dt, string context, LocalizedString locString, IList<string> languages)
        {
            var row = dt.NewRow();
            row["Context"] = context;
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
            string text = string.Empty;
            if (!string.IsNullOrEmpty(defaultLanguage) &&
                content.Translations != null &&
                content.Translations.TryGetValue(defaultLanguage, out var localized))
            {
                text = localized;
            }
            else if (content.Translations?.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) is string first)
            {
                text = first;
            }

            if (string.IsNullOrWhiteSpace(text))
                return fallback;

            return text.Length > 24 ? text.Substring(0, 24) + "…" : text;
        }
    }
}
