using System;
using System.Collections.Generic;
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
        public string Id { get; set; }

        [JsonPropertyName("graphMapPath")]
        public string GraphMapPath { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("fileHash")]
        public string FileHash { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("cosKey")]
        public string CosKey { get; set; }

        [JsonPropertyName("localPath")]
        public string LocalPath { get; set; }
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
        public string PreserveAnnouncement { get; set; }
    }

    public class PublishResult
    {
        public string OutputDirectory { get; set; }
        public int TotalOfficialCount { get; set; }
        public int ExportedZipCount { get; set; }
        public int SkippedZipCount { get; set; }
        public string GraphMapListPath { get; set; }
        public string HomeLinksCatalogPath { get; set; }
        public string HomeLinksHash { get; set; }
        public string ServerInfoPath { get; set; }
        public string CategoriesPath { get; set; }
        public string ManifestPath { get; set; }
        public string ListHash { get; set; }
        public List<PublishManifestEntry> ManifestEntries { get; set; } = new();

        public string Summary =>
            $"官方模板 {TotalOfficialCount} 个，导出 ZIP {ExportedZipCount} 个，跳过 {SkippedZipCount} 个。";
    }

    public class PublishPreviewItem
    {
        public Guid Id { get; set; }
        public string GraphMapPath { get; set; }
        public string Name { get; set; }
        public string LocalHash { get; set; }
        public string RemoteHash { get; set; }
        public PublishAction Action { get; set; }
    }

    public class PublishPreview
    {
        public List<PublishPreviewItem> NewItems { get; set; } = new();
        public List<PublishPreviewItem> UpdatedItems { get; set; } = new();
        public List<PublishPreviewItem> RemovedItems { get; set; } = new();
        public List<PublishPreviewItem> UnchangedItems { get; set; } = new();

        public int PendingChangeCount => NewItems.Count + UpdatedItems.Count + RemovedItems.Count;
    }

    public class GeothermometerPublishResult
    {
        public string OutputDirectory { get; set; }
        public int TotalOfficialCount { get; set; }
        public int ExportedZipCount { get; set; }
        public int SkippedZipCount { get; set; }
        public string ListPath { get; set; }
        public string IndexPath { get; set; }
        public string ManifestPath { get; set; }
        public string ListHash { get; set; }
        public List<PublishManifestEntry> ManifestEntries { get; set; } = new();

        public string Summary =>
            $"官方温压计 {TotalOfficialCount} 个，导出 ZIP {ExportedZipCount} 个，跳过 {SkippedZipCount} 个。";
    }

    public class HomeLinksPublishResult
    {
        public string OutputDirectory { get; set; }
        public string HomeLinksCatalogPath { get; set; }
        public string HomeLinksHash { get; set; }
        public string ServerInfoPath { get; set; }
        public int GroupCount { get; set; }
        public int LinkCount { get; set; }

        public string Summary =>
            $"主页链接 {GroupCount} 个分组，{LinkCount} 个链接。";
    }

    public class AnnouncementPublishResult
    {
        public string OutputDirectory { get; set; }
        public string ServerInfoPath { get; set; }
        public string Announcement { get; set; }

        public string Summary => "公告已写入 server_info.json。";
    }

    public class CosUploadResult
    {
        public bool Success { get; set; }
        public int UploadedFileCount { get; set; }
        public bool ServerInfoVerified { get; set; }
        public string Message { get; set; }
        public List<string> UploadedKeys { get; set; } = new();
    }
}
