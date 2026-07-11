using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 地质温压计（GTM）定义模型
    /// </summary>
    public class Geothermometer
    {
        /// <summary>
        /// GTM 唯一标识符
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// GTM 版本号
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 温压计类别键（single_mineral / mineral_pair / multi_equilibrium）
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 温压计标签（显示名称，已本地化）
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 标签存储键（未本地化，用于筛选匹配）
        /// </summary>
        [JsonIgnore]
        public List<string> StorageTags { get; set; } = new();

        /// <summary>
        /// 标签显示文本（用于列表等简单绑定）
        /// </summary>
        [JsonIgnore]
        public string TagsDisplayText => Tags == null || Tags.Count == 0
            ? string.Empty
            : string.Join(" · ", Tags);

        /// <summary>
        /// 温压计名称（显示名称）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 温压计名称的多语言键名
        /// </summary>
        public string NameLangKey { get; set; } = string.Empty;

        /// <summary>
        /// 作者信息
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// 发表年份
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// 参考文献
        /// </summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// 图标编码（iconfont unicode）
        /// </summary>
        public string IconCode { get; set; } = "\ue60d";

        /// <summary>
        /// 图标颜色（十六进制色值）
        /// </summary>
        public string IconColor { get; set; } = "#555555";

        /// <summary>
        /// 表格表头列表
        /// </summary>
        public List<string> Headers { get; set; } = new();

        /// <summary>
        /// 示例数据行
        /// </summary>
        public List<string> ExampleRow { get; set; } = new();

        /// <summary>
        /// 已注册的公式函数名称
        /// </summary>
        public string FormulaName { get; set; } = string.Empty;

        /// <summary>
        /// JavaScript 计算脚本（内联脚本，ZIP 导入时可能携带）
        /// </summary>
        public string Script { get; set; } = string.Empty;

        /// <summary>
        /// 外部 JavaScript 脚本文件名（ZIP 交换格式中使用）
        /// </summary>
        public string ScriptFile { get; set; } = string.Empty;

        /// <summary>
        /// 输入列名列表（用于标识哪些列是用户输入，便于提取参数调用 calculateDetailed）
        /// </summary>
        public List<string> InputColumns { get; set; } = new();

        /// <summary>
        /// 附加公式列表（一个 GTM 可注册多个 ReoGrid 自定义函数）
        /// 每个条目包含 FormulaName（ReoGrid 中的函数名）和 FunctionName（JS 脚本中的函数名）
        /// </summary>
        public List<AdditionalFormula> AdditionalFormulas { get; set; } = new();

        /// <summary>
        /// 是否为官方温压计
        /// </summary>
        [JsonIgnore]
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// 是否已收藏
        /// </summary>
        [JsonIgnore]
        public bool IsFavorite { get; set; }
    }

    /// <summary>
    /// 温压计类别分组模型（用于官方温压计 UI 展示）
    /// </summary>
    public class GeoTCategoryGroup
    {
        /// <summary>
        /// 类别键
        /// </summary>
        public string CategoryKey { get; set; } = string.Empty;

        /// <summary>
        /// 类别显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 图标编码
        /// </summary>
        public string IconCode { get; set; } = string.Empty;

        /// <summary>
        /// 图标颜色
        /// </summary>
        public string IconColor { get; set; } = "#555555";

        /// <summary>
        /// 该类别下的温压计列表
        /// </summary>
        public List<Geothermometer> Plugins { get; set; } = new();

        /// <summary>
        /// 是否展开
        /// </summary>
        public bool IsExpanded { get; set; }
    }

    /// <summary>
    /// 服务器 GTM 索引条目模型
    /// </summary>
    public class PluginIndexEntry
    {
        /// <summary>
        /// GTM ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 参考文献
        /// </summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// 下载地址（相对路径）
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// 文件哈希（用于校验完整性）
        /// </summary>
        public string Hash { get; set; } = string.Empty;
    }

    /// <summary>
    /// 服务器 GTM 索引
    /// </summary>
    public class PluginIndex
    {
        /// <summary>
        /// 索引版本
        /// </summary>
        public string IndexVersion { get; set; } = "1.0.0";

        /// <summary>
        /// GTM 列表
        /// </summary>
        public List<PluginIndexEntry> Plugins { get; set; } = new();
    }

    /// <summary>
    /// 温压计更新检查结果状态
    /// </summary>
    public enum GeothermometerUpdateCheckStatus
    {
        /// <summary>检查成功完成</summary>
        Success,
        /// <summary>检查过程中发生错误（网络、解析等）</summary>
        Failed
    }

    /// <summary>
    /// 温压计更新检查结果
    /// </summary>
    public class GeothermometerUpdateCheckResult
    {
        public GeothermometerUpdateCheckStatus Status { get; set; } = GeothermometerUpdateCheckStatus.Failed;

        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>需要从服务器下载或更新的条目</summary>
        public List<PluginIndexEntry> Updates { get; set; } = new();

        /// <summary>已从服务器清单下架、待删除的本地官方温压计实体 ID</summary>
        public List<Guid> Removals { get; set; } = new();

        /// <summary>本次检查是否同步了矿物分类翻译文件</summary>
        public bool MineralCategoriesSynced { get; set; }

        public bool HasChanges => Updates.Count > 0 || Removals.Count > 0;
    }

    /// <summary>
    /// 单个温压计下载结果
    /// </summary>
    public class GeothermometerDownloadItemResult
    {
        public bool Success { get; init; }

        public string PluginId { get; init; } = string.Empty;

        public string ErrorMessage { get; init; } = string.Empty;

        public static GeothermometerDownloadItemResult Succeeded(string pluginId)
            => new() { Success = true, PluginId = pluginId };

        public static GeothermometerDownloadItemResult Failed(string pluginId, string errorMessage)
            => new() { Success = false, PluginId = pluginId, ErrorMessage = errorMessage };
    }

    /// <summary>
    /// 温压计批量下载结果
    /// </summary>
    public class GeothermometerBatchDownloadResult
    {
        public int SuccessCount { get; init; }

        public int RemovalCount { get; init; }

        public IReadOnlyList<GeothermometerDownloadItemResult> Failures { get; init; } =
            Array.Empty<GeothermometerDownloadItemResult>();
    }

    /// <summary>
    /// ReoGrid 公式名冲突描述
    /// </summary>
    public class FormulaNameConflict
    {
        public string FormulaName { get; set; } = string.Empty;
        public string ExistingPluginId { get; set; } = string.Empty;
        public string ExistingName { get; set; } = string.Empty;
        public string CandidatePluginId { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
    }

    /// <summary>
    /// GeoT-index.json 模型，包含 GeoT-List.json 的哈希值
    /// 用于客户端快速判断是否需要下载完整的 GTM 列表
    /// </summary>
    public class GeoTIndex
    {
        /// <summary>
        /// GeoT-List.json 文件的 MD5 哈希值
        /// </summary>
        public string ListHash { get; set; } = string.Empty;

        /// <summary>
        /// GeoTMineralCategories.json 文件的 MD5 哈希值
        /// </summary>
        public string MineralCategoriesHash { get; set; } = string.Empty;

        /// <summary>
        /// 索引版本
        /// </summary>
        public string IndexVersion { get; set; } = "1.0.0";
    }

    /// <summary>
    /// 温压计 ZIP 导入失败原因
    /// </summary>
    public enum GeothermometerImportFailureReason
    {
        /// <summary>文件损坏或 JSON 格式不符合温压计规范</summary>
        InvalidOrCorrupted,
        /// <summary>温压计格式版本高于当前程序支持版本</summary>
        VersionIncompatible
    }

    /// <summary>
    /// 温压计 ZIP 导入失败异常
    /// </summary>
    public class GeothermometerImportException : Exception
    {
        public GeothermometerImportFailureReason Reason { get; }

        public GeothermometerImportException(GeothermometerImportFailureReason reason, Exception? innerException = null)
            : base(null, innerException)
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// 附加公式定义（用于一个 GTM 注册多个 ReoGrid 自定义函数）
    /// </summary>
    public class AdditionalFormula
    {
        /// <summary>
        /// ReoGrid 中注册的公式名称
        /// </summary>
        public string FormulaName { get; set; } = string.Empty;

        /// <summary>
        /// JS 脚本中对应的函数名称
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;
    }
}
