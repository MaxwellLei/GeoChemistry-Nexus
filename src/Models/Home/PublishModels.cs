using GeoChemistryNexus.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    public enum PublishAction
    {
        New,
        Update,
        Skip,
        Remove
    }

    public class PublishManifestEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("graphMapPath")]
        public string GraphMapPath { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("fileHash")]
        public string FileHash { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("cosKey")]
        public string CosKey { get; set; } = string.Empty;

        [JsonPropertyName("localPath")]
        public string? LocalPath { get; set; }
    }

    public class PublishOptions
    {
        /// <summary>
        /// 强制导出全部 ZIP，忽略 Hash 对比
        /// </summary>
        public bool ForceExportAll { get; set; }

        /// <summary>
        /// 保留 server_info.json 中的现有公告
        /// </summary>
        public string? PreserveAnnouncement { get; set; }

        /// <summary>
        /// 保留 server_info.json 中的最低支持软件版本
        /// </summary>
        public string? PreserveMinimumSupportedVersion { get; set; }

        /// <summary>
        /// 保留 server_info.json 中的最新软件版本
        /// </summary>
        public string? PreserveLatestAppVersion { get; set; }
    }

    /// <summary>
    /// 随软件新版本一起写入 server_info.json 的安装包信息：完整版/精简版的 SHA256 和所需运行时主版本。
    /// </summary>
    public class AppInstallerReleaseInfo
    {
        /// <summary>完整版安装包 SHA256，空串表示不写校验值。</summary>
        public string FullSha256 { get; set; } = string.Empty;

        /// <summary>精简版安装包 SHA256，空串表示本次没有精简版。</summary>
        public string LiteSha256 { get; set; } = string.Empty;

        /// <summary>所需 .NET 运行时主版本，0 表示不启用精简版。</summary>
        public int RequiredRuntimeMajor { get; set; }

        public void ApplyTo(ServerInfo serverInfo)
        {
            serverInfo.LatestAppInstallerSha256 = FullSha256 ?? string.Empty;
            serverInfo.LatestAppInstallerLiteSha256 = LiteSha256 ?? string.Empty;
            serverInfo.RequiredRuntimeMajor = RequiredRuntimeMajor;
        }
    }

    public class PublishResult
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public int TotalOfficialCount { get; set; }
        public int ExportedZipCount { get; set; }
        public int SkippedZipCount { get; set; }
        public string GraphMapListPath { get; set; } = string.Empty;
        public string HomeLinksCatalogPath { get; set; } = string.Empty;
        public string HomeLinksHash { get; set; } = string.Empty;
        public string ServerInfoPath { get; set; } = string.Empty;
        public string ManifestPath { get; set; } = string.Empty;
        public string ListHash { get; set; } = string.Empty;
        public string MinimumSupportedVersion { get; set; } = string.Empty;
        public string LatestAppVersion { get; set; } = string.Empty;
        public List<PublishManifestEntry> ManifestEntries { get; set; } = new();

        public string Summary =>
            $"官方模板 {TotalOfficialCount} 个，导出 ZIP {ExportedZipCount} 个，跳过 {SkippedZipCount} 个。";
    }

    public class PublishPreviewItem
    {
        public Guid Id { get; set; }
        public string GraphMapPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LocalHash { get; set; } = string.Empty;
        public string RemoteHash { get; set; } = string.Empty;
        public PublishAction Action { get; set; }

        /// <summary>本地图解的多级路径（NodeList）；仅图解预览填充。</summary>
        public LocalizedString? LocalNodeList { get; set; }

        /// <summary>线上 GraphMapList 里记录的多级路径（NodeList）；新增项为空。</summary>
        public LocalizedString? RemoteNodeList { get; set; }

        /// <summary>本地与线上的路径不一致（改过文件夹名或移动过位置）。</summary>
        public bool PathChanged { get; set; }
    }

    public class PublishPreview
    {
        public List<PublishPreviewItem> NewItems { get; set; } = new();
        public List<PublishPreviewItem> UpdatedItems { get; set; } = new();
        public List<PublishPreviewItem> RemovedItems { get; set; } = new();
        public List<PublishPreviewItem> UnchangedItems { get; set; } = new();

        public int PendingChangeCount => NewItems.Count + UpdatedItems.Count + RemovedItems.Count;

        /// <summary>更新项中路径发生变化的数量（是 UpdatedItems 的子集）。</summary>
        public int PathChangedCount => UpdatedItems.Count(item => item.PathChanged);
    }

    public class GeothermometerPublishResult
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public int TotalOfficialCount { get; set; }
        public int ExportedZipCount { get; set; }
        public int SkippedZipCount { get; set; }
        public string ListPath { get; set; } = string.Empty;
        public string IndexPath { get; set; } = string.Empty;
        public string CategoriesPath { get; set; } = string.Empty;
        public string ManifestPath { get; set; } = string.Empty;
        public string ListHash { get; set; } = string.Empty;
        public string MineralCategoriesHash { get; set; } = string.Empty;
        public List<PublishManifestEntry> ManifestEntries { get; set; } = new();

        public string Summary =>
            $"官方温压计 {TotalOfficialCount} 个，导出 ZIP {ExportedZipCount} 个，跳过 {SkippedZipCount} 个。";
    }

    public class HomeLinksPublishResult
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public string HomeLinksCatalogPath { get; set; } = string.Empty;
        public string HomeLinksHash { get; set; } = string.Empty;
        public string ServerInfoPath { get; set; } = string.Empty;
        public string Announcement { get; set; } = string.Empty;
        public string MinimumSupportedVersion { get; set; } = string.Empty;
        public string LatestAppVersion { get; set; } = string.Empty;
        public int GroupCount { get; set; }
        public int LinkCount { get; set; }

        public string Summary =>
            $"主页链接 {GroupCount} 个分组，{LinkCount} 个链接。";
    }

    public class AnnouncementPublishResult
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public string ServerInfoPath { get; set; } = string.Empty;
        public string Announcement { get; set; } = string.Empty;
        public string MinimumSupportedVersion { get; set; } = string.Empty;
        public string LatestAppVersion { get; set; } = string.Empty;

        /// <summary>多语言公告目录（Announcements.json）导出路径，为空表示本次未发布公告目录</summary>
        public string AnnouncementsCatalogPath { get; set; } = string.Empty;
        public string AnnouncementsHash { get; set; } = string.Empty;
        public int AnnouncementsEntryCount { get; set; }

        public string Summary => string.IsNullOrEmpty(AnnouncementsCatalogPath)
            ? "公告已写入 server_info.json。"
            : $"公告已写入 server_info.json，多语言公告目录 {AnnouncementsEntryCount} 条。";
    }

    public class CosUploadResult
    {
        public bool Success { get; set; }
        public int UploadedFileCount { get; set; }
        public bool ServerInfoVerified { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> UploadedKeys { get; set; } = new();
    }
}
