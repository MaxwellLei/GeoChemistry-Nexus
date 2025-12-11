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
        public string ListHash { get; set; }

        /// <summary>
        /// 服务器端类别结构文件的哈希值
        /// </summary>
        [JsonPropertyName("list_PlotCategories_hash")]
        public string ListPlotCategoriesHash { get; set; }

        /// <summary>
        /// 服务器公告信息
        /// </summary>
        [JsonPropertyName("announcement")]
        public string Announcement { get; set; }
    }
}
