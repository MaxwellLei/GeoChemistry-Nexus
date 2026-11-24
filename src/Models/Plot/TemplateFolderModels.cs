using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 本地图解文件夹清单（Config/PlotTemplateFolders.json）。
    /// 文件夹本身由模板的 NodeList 推导；清单只负责保存空文件夹及其多语言名称，不随导入导出包或发布推送。
    /// </summary>
    public sealed class TemplateFolderManifest
    {
        public List<TemplateFolderEntry> Custom { get; set; } = new();

        public List<TemplateFolderEntry> Official { get; set; } = new();

        public List<TemplateFolderEntry> GetEntries(bool isCustom) => isCustom ? Custom : Official;
    }

    public sealed class TemplateFolderEntry
    {
        /// <summary>从根到该文件夹的每一级多语言名称（语言代码 → 名称）。</summary>
        public List<Dictionary<string, string>> Path { get; set; } = new();
    }

    /// <summary>
    /// 文件夹多语言名称的导入导出文件。每个文件夹用 key（英文路径，层级以 &gt; 分隔）标识，
    /// 导入时只按 key 匹配，不看名称，所以名称可以随意翻译、修改。
    /// </summary>
    public sealed class TemplateFolderNameFile
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("note")]
        public string Note { get; set; } = string.Empty;

        /// <summary>软件支持的全部语言码，翻译时按这个列表补全 names。</summary>
        [JsonPropertyName("languages")]
        public List<string> Languages { get; set; } = new();

        [JsonPropertyName("folders")]
        public List<TemplateFolderNameEntry> Folders { get; set; } = new();
    }

    public sealed class TemplateFolderNameEntry
    {
        /// <summary>文件夹标识：从根到该文件夹的英文名称，以 &gt; 分隔。导入时靠它匹配，不要修改。</summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>语言码 → 文件夹名称；空字符串表示还没有该语言的名称。</summary>
        [JsonPropertyName("names")]
        public Dictionary<string, string> Names { get; set; } = new();
    }

    /// <summary>
    /// 合并模板路径与本地清单后的文件夹节点，供文件夹选择器和文件夹操作使用。
    /// </summary>
    public sealed class TemplateFolderInfo
    {
        public string Name { get; init; } = string.Empty;

        public bool IsRoot { get; init; }

        public bool IsCustom { get; init; }

        public TemplateFolderInfo? Parent { get; init; }

        /// <summary>各语言名称；模板路径中的名称优先，清单中的名称补齐缺失语言。</summary>
        public Dictionary<string, string> LocalizedNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<TemplateFolderInfo> Children { get; } = new();

        /// <summary>直接位于该文件夹中的模板（摘要）。</summary>
        public List<GraphMapTemplateEntity> Templates { get; } = new();

        /// <summary>当前界面语言下的路径（不含根）。</summary>
        public IReadOnlyList<string> DisplayPath { get; init; } = Array.Empty<string>();

        public int Depth => DisplayPath.Count;

        public IEnumerable<GraphMapTemplateEntity> AllTemplates()
            => Templates.Concat(Children.SelectMany(c => c.AllTemplates()));

        public IEnumerable<TemplateFolderInfo> SelfAndAncestors()
        {
            for (var node = this; node != null && !node.IsRoot; node = node.Parent)
                yield return node;
        }

        public bool IsSameOrDescendantOf(TemplateFolderInfo other)
        {
            for (var node = this; node != null; node = node.Parent)
            {
                if (ReferenceEquals(node, other))
                    return true;
            }
            return false;
        }
    }
}
