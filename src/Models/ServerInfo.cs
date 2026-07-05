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
        /// 服务器端类别结构文件的哈希值
        /// </summary>
        [JsonPropertyName("list_PlotCategories_hash")]
        public string ListPlotCategoriesHash { get; set; } = string.Empty;

        /// <summary>
        /// 服务器端主页链接目录文件的哈希值
        /// </summary>
        [JsonPropertyName("home_links_hash")]
        public string HomeLinksHash { get; set; } = string.Empty;

        /// <summary>
        /// 服务器公告信息
        /// </summary>
        [JsonPropertyName("announcement")]
        public string Announcement { get; set; } = string.Empty;

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
    }
}
