using LiteDB;
using GeoChemistryNexus.Helpers;
using System;
using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    public class GraphMapTemplateEntity
    {
        // --- 索引与身份区 ---
        
        /// <summary>
        /// 主键 ID，使用 GUID
        /// 生成规则：使用 GraphMapPath 生成确定性 UUID (UUID v5)，确保同一模板路径生成的 Guid 始终一致
        /// </summary>
        [BsonId]
        public Guid Id { get; set; }

        /// <summary>
        /// 原有的路径标识符，例如 "Igneous/TAS_Diagram"
        /// 作为生成 Id 的种子
        /// </summary>
        public string GraphMapPath { get; set; }

        /// <summary>
        /// 文件哈希，用于版本比对 (MD5)
        /// 计算源为 Content 序列化后的 JSON 字符串
        /// </summary>
        public string FileHash { get; set; }

        /// <summary>
        /// 是否为用户自定义模板 (true: 用户创建/另存; false: 系统内置)
        /// </summary>
        public bool IsCustom { get; set; }

        /// <summary>
        /// 是否为新模板 (true: 刚刚转换为内置模板; false: 旧模板)
        /// </summary>
        public bool IsNewTemplate { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModified { get; set; }

        // --- 轻量元数据区 (用于列表展示) ---

        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 模板分类层级信息
        /// 从 Content.NodeList 映射而来，用于快速构建分类树
        /// </summary>
        public LocalizedString NodeList { get; set; }
        
        public string TemplateType { get; set; } // "Cartesian" or "Ternary"
        public float Version { get; set; }

        // --- 重量级数据区 (Payload) ---

        /// <summary>
        /// 完整的绘图模板数据 (包含点、线、坐标轴、脚本等所有细节)
        /// 对应原 .json 文件内容
        /// 仅在打开模板时加载
        /// </summary>
        public GraphMapTemplate Content { get; set; }

        // --- 附属资源区 ---

        /// <summary>
        /// 多语言帮助文档内容
        /// Key: 语言代码 (如 "zh-CN", "en-US")
        /// Value: RTF 文本内容
        /// </summary>
        public Dictionary<string, string> HelpDocuments { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 模板状态：NOT_INSTALLED, UP_TO_DATE, OUTDATED
        /// </summary>
        public string Status { get; set; }
    }
}
