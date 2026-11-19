using LiteDB;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// EPMA 计算模板安装状态常量（与图解 / 温压计模板的 Status 取值保持一致）
    /// </summary>
    public static class EpmaInstallStatus
    {
        public const string NotInstalled = "NOT_INSTALLED";
        public const string UpToDate = "UP_TO_DATE";
        public const string Outdated = "OUTDATED";
        public const string RequiresAppUpgrade = "REQUIRES_APP_UPGRADE";
    }

    /// <summary>
    /// EPMA 计算模板数据库实体。
    /// 存储在 LiteDB 中：列表展示所需的轻量元数据 + 完整的 .epma.zip 字节（PackageBytes）。
    /// 官方模板与个人模板各自占一条记录，同一 PackageId 的官方与个人模板可以并存。
    /// </summary>
    public class EpmaTemplateEntity
    {
        /// <summary>主键：由（官方标记 + PackageId）生成的确定性 UUID v5</summary>
        [BsonId]
        public Guid Id { get; set; }

        /// <summary>计算包标识（manifest.packageId）</summary>
        public string PackageId { get; set; } = string.Empty;

        /// <summary>计算包版本（manifest.version）</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>计算包格式版本（manifest.formatVersion）</summary>
        public int FormatVersion { get; set; }

        /// <summary>发布内容哈希：整个 .epma.zip 字节的 SHA-256（小写十六进制）</summary>
        public string FileHash { get; set; } = string.Empty;

        /// <summary>最后修改时间</summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>是否为官方计算模板</summary>
        public bool IsOfficial { get; set; }

        /// <summary>是否已收藏</summary>
        public bool IsFavorite { get; set; }

        /// <summary>安装状态：官方模板为 NOT_INSTALLED / UP_TO_DATE / OUTDATED / REQUIRES_APP_UPGRADE，个人模板为空</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 服务器清单中的内容哈希。为空表示该官方模板尚未发布（本地新建 / 内置种子），
        /// 同步时不会因为服务器清单里没有它而被删除。
        /// </summary>
        public string ServerHash { get; set; } = string.Empty;

        /// <summary>manifest.name 的原始 JSON（字符串或语言字典）</summary>
        public string NameJson { get; set; } = "null";

        /// <summary>manifest.description 的原始 JSON</summary>
        public string DescriptionJson { get; set; } = "null";

        /// <summary>manifest.tags 的原始 JSON 数组</summary>
        public string TagsJson { get; set; } = "[]";

        public string Author { get; set; } = string.Empty;

        public string UpdatedAt { get; set; } = string.Empty;

        public string Icon { get; set; } = "Σ";

        /// <summary>完整 .epma.zip 字节；未下载的官方占位为空数组（列表查询不会读取此字段）</summary>
        public byte[] PackageBytes { get; set; } = Array.Empty<byte>();

        [BsonIgnore]
        public bool IsCustom => !IsOfficial;

        [BsonIgnore]
        public bool HasPackage => PackageBytes is { Length: > 0 };

        /// <summary>
        /// 摘要查询不读取字节，因此用 FileHash 判断是否已安装：
        /// 未下载的官方占位 FileHash 为空，已保存的包始终带有 SHA-256。
        /// </summary>
        [BsonIgnore]
        public bool IsInstalled => !string.IsNullOrEmpty(FileHash);

        [BsonIgnore]
        public bool IsNotInstalled =>
            IsOfficial && string.Equals(Status, EpmaInstallStatus.NotInstalled, StringComparison.Ordinal);

        [BsonIgnore]
        public bool IsUpdateAvailable =>
            IsOfficial && string.Equals(Status, EpmaInstallStatus.Outdated, StringComparison.Ordinal);

        [BsonIgnore]
        public bool RequiresAppUpgrade =>
            IsOfficial && string.Equals(Status, EpmaInstallStatus.RequiresAppUpgrade, StringComparison.Ordinal);
    }

    /// <summary>
    /// 服务器 EPMA-List.json 目录项：携带足够元数据，供未下载占位直接展示。
    /// </summary>
    public class EpmaIndexEntry
    {
        public static readonly JsonElement NullElement = System.Text.Json.JsonSerializer.SerializeToElement<object?>(null);
        public static readonly JsonElement EmptyArrayElement = System.Text.Json.JsonSerializer.SerializeToElement(Array.Empty<string>());

        public string Id { get; set; } = string.Empty;

        public int FormatVersion { get; set; }

        public string Version { get; set; } = "1.0.0";

        public JsonElement Name { get; set; } = NullElement;

        public JsonElement Description { get; set; } = NullElement;

        public JsonElement Tags { get; set; } = EmptyArrayElement;

        public string Author { get; set; } = string.Empty;

        public string UpdatedAt { get; set; } = string.Empty;

        public string Icon { get; set; } = "Σ";

        /// <summary>下载地址（相对 Epma 目录的文件名）</summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>.epma.zip 字节的 SHA-256</summary>
        public string Hash { get; set; } = string.Empty;
    }

    /// <summary>服务器 EPMA 官方模板清单（EPMA-List.json）</summary>
    public class EpmaIndex
    {
        public string IndexVersion { get; set; } = "1.0.0";

        public List<EpmaIndexEntry> Templates { get; set; } = new();
    }

    /// <summary>EPMA-index.json：保存 EPMA-List.json 的 MD5，客户端据此快速判断清单是否变化</summary>
    public class EpmaListIndex
    {
        public string ListHash { get; set; } = string.Empty;

        public string IndexVersion { get; set; } = "1.0.0";
    }

    public enum EpmaUpdateCheckStatus
    {
        Success,
        Failed
    }

    /// <summary>EPMA 更新检查结果（只同步清单与状态，不自动下载模板包）</summary>
    public class EpmaUpdateCheckResult
    {
        public EpmaUpdateCheckStatus Status { get; set; } = EpmaUpdateCheckStatus.Failed;

        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>是否下载了新的 EPMA-List.json</summary>
        public bool ListDownloaded { get; set; }

        /// <summary>本地数据库是否因同步发生变化（占位 / 状态 / 下架）</summary>
        public bool CatalogChanged { get; set; }

        public int NotInstalledCount { get; set; }

        public int OutdatedCount { get; set; }

        public int RemovalCount { get; set; }

        /// <summary>服务器格式高于当前程序、无法下载的条目</summary>
        public List<EpmaIndexEntry> RequiresAppUpgrade { get; set; } = new();

        /// <summary>远端清单哈希与本地缓存不一致（以 downloadListIfOutdated=false 检查且未下载时返回，调用方可据此征求确认）</summary>
        public bool ListOutdated { get; set; }

        public bool HasPendingActions => NotInstalledCount > 0 || OutdatedCount > 0;

        public int RequiresAppUpgradeCount => RequiresAppUpgrade.Count;
    }

    public class EpmaDownloadItemResult
    {
        public bool Success { get; init; }

        public string PackageId { get; init; } = string.Empty;

        public string ErrorMessage { get; init; } = string.Empty;

        /// <summary>失败原因是下载内容与目录哈希不一致（本地目录可能已过期，可刷新目录后重试）</summary>
        public bool IsHashMismatch { get; init; }

        public static EpmaDownloadItemResult Succeeded(string packageId)
            => new() { Success = true, PackageId = packageId };

        public static EpmaDownloadItemResult Failed(string packageId, string errorMessage, bool isHashMismatch = false)
            => new() { Success = false, PackageId = packageId, ErrorMessage = errorMessage, IsHashMismatch = isHashMismatch };
    }

    public class EpmaBatchDownloadResult
    {
        public int SuccessCount { get; init; }

        public IReadOnlyList<EpmaDownloadItemResult> Failures { get; init; } = Array.Empty<EpmaDownloadItemResult>();
    }

    /// <summary>EPMA 官方模板发布导出结果</summary>
    public class EpmaPublishResult
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public int TotalOfficialCount { get; set; }
        public int ExportedZipCount { get; set; }
        public int SkippedZipCount { get; set; }
        public string ListPath { get; set; } = string.Empty;
        public string IndexPath { get; set; } = string.Empty;
        public string ManifestPath { get; set; } = string.Empty;
        public string ListHash { get; set; } = string.Empty;
        public List<PublishManifestEntry> ManifestEntries { get; set; } = new();

        public string Summary =>
            $"官方 EPMA 计算模板 {TotalOfficialCount} 个，导出 ZIP {ExportedZipCount} 个，跳过 {SkippedZipCount} 个。";
    }
}
