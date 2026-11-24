using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    public class ServerInfo
    {
        /// <summary>
        /// 服务器端模板列表文件的哈希值
        /// </summary>
        [JsonPropertyName("list_hash")]
        public string ListHash { get; set; } = string.Empty;

        /// <summary>
        /// 服务器端主页链接目录文件的哈希值
        /// </summary>
        [JsonPropertyName("home_links_hash")]
        public string HomeLinksHash { get; set; } = string.Empty;

        /// <summary>
        /// 服务器公告信息（旧版纯文本字段，保留用于兼容回退）
        /// </summary>
        [JsonPropertyName("announcement")]
        public string Announcement { get; set; } = string.Empty;

        /// <summary>
        /// 服务器端公告目录文件（Announcements.json）的哈希值
        /// </summary>
        [JsonPropertyName("announcements_hash")]
        public string AnnouncementsHash { get; set; } = string.Empty;

        /// <summary>
        /// 当前服务器允许继续使用的软件最低版本号
        /// </summary>
        [JsonPropertyName("minimum_supported_version")]
        public string MinimumSupportedVersion { get; set; } = string.Empty;

        /// <summary>
        /// 当前服务器发布的最新软件版本号
        /// </summary>
        [JsonPropertyName("latest_app_version")]
        public string LatestAppVersion { get; set; } = string.Empty;

        /// <summary>
        /// 最新版安装包的 SHA256（十六进制）。填写后客户端会在安装前校验；留空则只做完整性（长度）校验。
        /// </summary>
        [JsonPropertyName("latest_app_installer_sha256")]
        public string LatestAppInstallerSha256 { get; set; } = string.Empty;

        /// <summary>
        /// 最新版精简安装包（不内置 .NET 运行时，文件名后缀 -x64-lite.exe）的 SHA256（十六进制）。
        /// 留空表示本次发布没有精简版，客户端始终使用完整版安装包。
        /// </summary>
        [JsonPropertyName("latest_app_installer_lite_sha256")]
        public string LatestAppInstallerLiteSha256 { get; set; } = string.Empty;

        /// <summary>
        /// 最新版软件需要的 .NET 运行时主版本号（如 10）。
        /// 仅当本机已安装该主版本的 Desktop 运行时，客户端才会选用精简版安装包；0 或缺省表示不启用精简版。
        /// </summary>
        [JsonPropertyName("required_runtime_major")]
        public int RequiredRuntimeMajor { get; set; }

        /// <summary>
        /// 官方内容对象存储公共根地址。客户端连上任意一台后，把这里多出来的地址并入本次会话的尝试列表。
        /// </summary>
        [JsonPropertyName("content_mirrors")]
        public List<string> ContentMirrors { get; set; } = new();
    }
}
