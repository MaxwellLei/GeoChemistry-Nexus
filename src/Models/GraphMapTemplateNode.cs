using GeoChemistryNexus.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    public class GraphMapTemplateNode
    {
        /// <summary>
        /// 当节点名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 记录当前节点的父亲
        /// </summary>
        [JsonIgnore]
        public GraphMapTemplateNode Parent { get; set; }

        /// <summary>
        /// 子节点
        /// </summary>
        public ObservableCollection<GraphMapTemplateNode> Children { get; } = new();

        /// <summary>
        /// 模板文件和缩略图路径，不含文件后缀
        /// </summary>
        public string GraphMapPath { get; set; }

        // 模板列表文件哈希值
        // 更新逻辑核心依赖
        [JsonPropertyName("file_hash")]
        public string FileHash { get; set; }

    }
}
