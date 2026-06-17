using LiteDB;
using System;
using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 温压计（GTM）数据库实体
    /// 存储在 LiteDB 中，包含元数据、JS 脚本和帮助文档
    /// </summary>
    public class GeothermometerEntity
    {
        // --- 索引与身份区 ---

        /// <summary>
        /// 主键 ID，使用确定性 UUID v5（由 GTM Id 字符串生成）
        /// </summary>
        [BsonId]
        public Guid Id { get; set; }

        /// <summary>
        /// GTM 标识符（如 "zircon_ti_loucks_2020"），作为生成 Id 的种子
        /// </summary>
        public string PluginId { get; set; } = string.Empty;

        /// <summary>
        /// GTM 版本号
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 发布内容哈希（元数据 + 脚本 + 帮助文档，用于版本比对）
        /// </summary>
        public string FileHash { get; set; } = string.Empty;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否为官方温压计（迁移自内置 JSON 的为官方，用户创建或导入的为自定义）
        /// </summary>
        public bool IsOfficial { get; set; }

        // --- 轻量元数据区 (用于列表展示) ---

        /// <summary>
        /// 温压计类别键（single_mineral / mineral_pair / multi_equilibrium）
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 温压计标签（存储 zh-CN 或用户输入的原始名称）
        /// </summary>
        public List<string> Tags { get; set; } = new();

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

        // --- 表格配置区 ---

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
        /// 输入列名列表
        /// </summary>
        public List<string> InputColumns { get; set; } = new();

        /// <summary>
        /// 附加公式列表
        /// </summary>
        public List<AdditionalFormula> AdditionalFormulas { get; set; } = new();

        // --- 重量级数据区 (脚本) ---

        /// <summary>
        /// JavaScript 计算脚本内容（完整的 .js 文件内容）
        /// 包含 calculate(args) 和 calculateDetailed(inputs) 两个函数
        /// </summary>
        public string ScriptContent { get; set; } = string.Empty;

        // --- 附属资源区 ---

        /// <summary>
        /// 多语言帮助文档内容
        /// Key: 语言代码 (如 "zh-CN", "en-US")
        /// Value: RTF 文本内容
        /// </summary>
        public Dictionary<string, string> HelpDocuments { get; set; } = new();
    }
}
