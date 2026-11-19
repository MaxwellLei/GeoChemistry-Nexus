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
        /// 能力标签（已规范化：内置 P/T/fO2 在前，自定义简写在后）
        /// </summary>
        public List<string> Capabilities { get; set; } = new();

        /// <summary>
        /// 能力标签显示文本（用于搜索等）
        /// </summary>
        [JsonIgnore]
        public string CapabilitiesDisplayText => Capabilities == null || Capabilities.Count == 0
            ? string.Empty
            : string.Join(" · ", Capabilities);

        /// <summary>
        /// 温压计名称（显示名称）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 温压计名称的多语言键名
        /// </summary>
        public string NameLangKey { get; set; } = string.Empty;

        /// <summary>
        /// 模板文本支持的语言（格式 1.2 起；第一项为默认语言，旧模板为空）
        /// </summary>
        public List<string> Languages { get; set; } = new();

        /// <summary>
        /// 各语言的模板名称（Name 保存默认语言文本）
        /// </summary>
        public Dictionary<string, string> NameLang { get; set; } = new();

        /// <summary>
        /// 各语言的参考文献（Reference 保存默认语言文本）
        /// </summary>
        public Dictionary<string, string> ReferenceLang { get; set; } = new();

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
        /// 各列单位（格式 1.3 新增，可选）：按位置与 <see cref="Headers"/> 一一对应，没有单位的列为空串；
        /// 旧模板或完全没有单位时为空列表。
        /// </summary>
        public List<string> Units { get; set; } = new();

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

        /// <summary>
        /// 安装状态：NOT_INSTALLED / UP_TO_DATE / OUTDATED / REQUIRES_APP_UPGRADE
        /// </summary>
        [JsonIgnore]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 未安装（需下载）
        /// </summary>
        [JsonIgnore]
        public bool IsNotInstalled =>
            string.Equals(Status, GeothermometerInstallStatus.NotInstalled, StringComparison.Ordinal);

        /// <summary>
        /// 有可更新版本或本地相对官方有改动
        /// </summary>
        [JsonIgnore]
        public bool IsUpdateAvailable =>
            string.Equals(Status, GeothermometerInstallStatus.Outdated, StringComparison.Ordinal);

        /// <summary>
        /// 服务器版本高于当前软件支持的格式：不能下载/更新；本地没有安装时也不能使用，已有旧版时仍可使用旧版
        /// </summary>
        [JsonIgnore]
        public bool IsRequiresAppUpgrade =>
            string.Equals(Status, GeothermometerInstallStatus.RequiresAppUpgrade, StringComparison.Ordinal);

        /// <summary>
        /// 本地是否已有脚本（已安装），来自数据库摘要投影，无需再读库
        /// </summary>
        [JsonIgnore]
        public bool IsInstalledLocally { get; set; }
    }

    /// <summary>
    /// 温压计官方目录安装状态常量（与图解模板 Status 对齐）
    /// </summary>
    public static class GeothermometerInstallStatus
    {
        public const string NotInstalled = "NOT_INSTALLED";
        public const string UpToDate = "UP_TO_DATE";
        public const string Outdated = "OUTDATED";
        public const string RequiresAppUpgrade = "REQUIRES_APP_UPGRADE";
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
    /// 服务器 GTM 索引条目模型（GeoT-List.json 目录项，含足够元数据供未安装占位展示）
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
        /// 显示名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 名称多语言键
        /// </summary>
        public string NameLangKey { get; set; } = string.Empty;

        /// <summary>
        /// 模板文本支持的语言（第一项为默认语言；旧目录无此字段）
        /// </summary>
        public List<string> Languages { get; set; } = new();

        /// <summary>
        /// 各语言的模板名称（旧目录无此字段）
        /// </summary>
        public Dictionary<string, string> NameLang { get; set; } = new();

        /// <summary>
        /// 各语言的参考文献（旧目录无此字段）
        /// </summary>
        public Dictionary<string, string> ReferenceLang { get; set; } = new();

        /// <summary>
        /// 类别键
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 能力标签
        /// </summary>
        public List<string> Capabilities { get; set; } = new();

        /// <summary>
        /// 作者
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
        /// 图标编码
        /// </summary>
        public string IconCode { get; set; } = "\ue60d";

        /// <summary>
        /// 图标颜色
        /// </summary>
        public string IconColor { get; set; } = "#555555";

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
    /// 温压计更新检查结果（仅同步目录与状态，不自动下载 ZIP）
    /// </summary>
    public class GeothermometerUpdateCheckResult
    {
        public GeothermometerUpdateCheckStatus Status { get; set; } = GeothermometerUpdateCheckStatus.Failed;

        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>是否下载了新的 GeoT-List.json</summary>
        public bool ListDownloaded { get; set; }

        /// <summary>本地数据库目录是否因同步发生变更（含占位/状态/下架）</summary>
        public bool CatalogChanged { get; set; }

        /// <summary>当前未安装的官方项数量</summary>
        public int NotInstalledCount { get; set; }

        /// <summary>当前可更新（OUTDATED）的官方项数量</summary>
        public int OutdatedCount { get; set; }

        /// <summary>本次同步删除的下架官方项数量</summary>
        public int RemovalCount { get; set; }

        /// <summary>服务器版本高于当前程序格式、无法下载更新的条目</summary>
        public List<PluginIndexEntry> RequiresAppUpgrade { get; set; } = new();

        /// <summary>本次检查是否同步了矿物分类翻译文件</summary>
        public bool MineralCategoriesSynced { get; set; }

        /// <summary>远端清单哈希与本地缓存不一致（以 downloadListIfOutdated=false 检查且未下载时返回，调用方可据此征求确认）</summary>
        public bool ListOutdated { get; set; }

        public bool HasPendingActions => NotInstalledCount > 0 || OutdatedCount > 0;

        public int RequiresAppUpgradeCount => RequiresAppUpgrade.Count;
    }

    /// <summary>
    /// 单个温压计下载结果
    /// </summary>
    public class GeothermometerDownloadItemResult
    {
        public bool Success { get; init; }

        public string PluginId { get; init; } = string.Empty;

        public string ErrorMessage { get; init; } = string.Empty;

        /// <summary>失败原因是下载内容与目录哈希不一致（本地目录可能已过期，可刷新目录后重试）</summary>
        public bool IsHashMismatch { get; init; }

        public static GeothermometerDownloadItemResult Succeeded(string pluginId)
            => new() { Success = true, PluginId = pluginId };

        public static GeothermometerDownloadItemResult Failed(string pluginId, string errorMessage, bool isHashMismatch = false)
            => new() { Success = false, PluginId = pluginId, ErrorMessage = errorMessage, IsHashMismatch = isHashMismatch };
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
