using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    public static class PlotCategoryStructureHelper
    {
        /// <summary>
        /// 从已有图解模板构建可选择的父级分类树（不含模板最后一层，即元素/图解名层级）。
        /// </summary>
        public static IReadOnlyList<CategoryStructureSelectNode> BuildSelectableStructureTree(
            IEnumerable<GraphMapTemplateEntity> entities,
            Guid? excludeTemplateId = null)
        {
            var builder = new TreeBuilderNode();

            foreach (var entity in entities)
            {
                if (excludeTemplateId.HasValue && entity.Id == excludeTemplateId.Value)
                    continue;

                if (entity.NodeList?.Translations == null || entity.NodeList.Translations.Count == 0)
                    continue;

                string displayLang = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);
                string displayPath = AppCultureRegistry.GetLocalizedValue(
                    entity.NodeList.Translations,
                    displayLang,
                    entity.NodeList.Default);

                var parts = displayPath
                    .Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length < 2)
                    continue;

                // 允许选到倒数第二层（不含模板专属的最后一层，如元素名 F1-F2）
                int maxPrefixLen = parts.Length - 1;
                builder.AddPath(entity.NodeList, parts, maxPrefixLen);
            }

            return builder.ToSelectNodes();
        }

        /// <summary>
        /// 将选中的分类前缀转换为编辑器使用的 CategoryPartModel 列表（含多语言名称）。
        /// </summary>
        public static List<CategoryPartModel> BuildCategoryPartsFromSelection(
            CategoryStructureSelectNode selectedNode)
        {
            if (selectedNode.SourceNodeList?.Translations == null || selectedNode.PrefixLength <= 0)
                return new List<CategoryPartModel>();

            string displayLang = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);
            string displayPath = AppCultureRegistry.GetLocalizedValue(
                selectedNode.SourceNodeList.Translations,
                displayLang,
                selectedNode.SourceNodeList.Default);
            var displayPathParts = displayPath
                .Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var parts = new List<CategoryPartModel>(selectedNode.PrefixLength);

            for (int i = 0; i < selectedNode.PrefixLength; i++)
            {
                var localizedNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (lang, path) in selectedNode.SourceNodeList.Translations)
                {
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    var segments = path.Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (i < segments.Length)
                        localizedNames[lang] = segments[i];
                }

                string displayName = localizedNames.TryGetValue(displayLang, out var localized) && !string.IsNullOrWhiteSpace(localized)
                    ? localized
                    : (i < displayPathParts.Length ? displayPathParts[i] : selectedNode.Name);

                parts.Add(new CategoryPartModel
                {
                    DisplayName = displayName,
                    LocalizedNames = localizedNames.Count > 0 ? localizedNames : null
                });
            }

            return parts;
        }

        /// <summary>
        /// 构建选中节点在当前界面语言下的完整路径显示文本。
        /// </summary>
        public static string BuildDisplayPath(CategoryStructureSelectNode node)
        {
            if (node.SourceNodeList?.Translations == null || node.PrefixLength <= 0)
                return node.Name;

            string displayLang = AppCultureRegistry.ResolveAppLanguage(LanguageService.CurrentLanguage);
            string displayPath = AppCultureRegistry.GetLocalizedValue(
                node.SourceNodeList.Translations,
                displayLang,
                node.SourceNodeList.Default);

            var parts = displayPath
                .Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return node.Name;

            int take = Math.Min(node.PrefixLength, parts.Length);
            return string.Join(" > ", parts.Take(take));
        }

        private sealed class TreeBuilderNode
        {
            private readonly Dictionary<string, TreeBuilderNode> _children = new(StringComparer.Ordinal);

            public string Name { get; private set; } = string.Empty;
            public bool IsSelectable { get; private set; }
            public LocalizedString? SourceNodeList { get; private set; }
            public int PrefixLength { get; private set; }

            public void AddPath(LocalizedString nodeList, string[] displayParts, int maxPrefixLen)
            {
                var current = this;
                for (int i = 0; i < maxPrefixLen; i++)
                {
                    string partName = displayParts[i];
                    if (!current._children.TryGetValue(partName, out var child))
                    {
                        child = new TreeBuilderNode { Name = partName };
                        current._children[partName] = child;
                    }

                    child.IsSelectable = true;
                    child.SourceNodeList = nodeList;
                    child.PrefixLength = i + 1;
                    current = child;
                }
            }

            public List<CategoryStructureSelectNode> ToSelectNodes()
            {
                return _children.Values
                    .OrderBy(c => c.Name, StringComparer.CurrentCulture)
                    .Select(c => c.ToSelectNode())
                    .ToList();
            }

            private CategoryStructureSelectNode ToSelectNode()
            {
                var node = new CategoryStructureSelectNode
                {
                    Name = Name,
                    IsSelectable = IsSelectable,
                    SourceNodeList = SourceNodeList,
                    PrefixLength = PrefixLength
                };

                foreach (var child in _children.Values.OrderBy(c => c.Name, StringComparer.CurrentCulture))
                    node.Children.Add(child.ToSelectNode());

                return node;
            }
        }
    }
}
