using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 地质温度计（GTM）定义模型
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
        /// 所属矿物分类
        /// </summary>
        public string Mineral { get; set; } = string.Empty;

        /// <summary>
        /// 矿物分类的多语言键名（对应语言资源文件中的 key）
        /// </summary>
        public string MineralLangKey { get; set; } = string.Empty;

        /// <summary>
        /// 温度计名称（显示名称）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 温度计名称的多语言键名
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

        /// <summary>
        /// 表格表头列表
        /// </summary>
        public List<string> Headers { get; set; } = new();

        /// <summary>
        /// 示例数据行
        /// </summary>
        public List<string> ExampleRow { get; set; } = new();

        /// <summary>
        /// 工作表名称
        /// </summary>
        public string WorksheetName { get; set; } = string.Empty;

        /// <summary>
        /// 已注册的公式函数名称（内置温度计使用）
        /// </summary>
        public string FormulaName { get; set; } = string.Empty;

        /// <summary>
        /// 帮助文档相对路径
        /// </summary>
        public string HelpDocPath { get; set; } = string.Empty;

        /// <summary>
        /// JavaScript 计算脚本（内联脚本，适用于简单公式）
        /// 当此字段不为空时，GTM 服务会通过 Jint 引擎注册该脚本为 FormulaExtension 自定义函数
        /// </summary>
        public string Script { get; set; } = string.Empty;

        /// <summary>
        /// 外部 JavaScript 脚本文件路径（相对于 GTM 目录，适用于复杂算法）
        /// 当 Script 为空但 ScriptFile 不为空时，从此文件加载脚本
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
        /// 简要描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否为内置 GTM
        /// </summary>
        [JsonIgnore]
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// GTM 来源（内置 / 本地 / 服务器）
        /// </summary>
        [JsonIgnore]
        public PluginSource Source { get; set; } = PluginSource.BuiltIn;

        /// <summary>
        /// 已加载的完整脚本内容（运行时填充，不序列化）
        /// </summary>
        [JsonIgnore]
        public string LoadedScript { get; set; } = string.Empty;
    }

    /// <summary>
    /// GTM 来源枚举
    /// </summary>
    public enum PluginSource
    {
        BuiltIn,
        Local,
        Server
    }

    /// <summary>
    /// 矿物分组模型（用于 UI 展示）
    /// </summary>
    public class MineralGroup
    {
        /// <summary>
        /// 矿物标识
        /// </summary>
        public string MineralKey { get; set; } = string.Empty;

        /// <summary>
        /// 矿物显示名称
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
        /// 该矿物下的温度计列表
        /// </summary>
        public List<Geothermometer> Plugins { get; set; } = new();

        /// <summary>
        /// 是否展开
        /// </summary>
        public bool IsExpanded { get; set; }
    }

    /// <summary>
    /// 服务器 GTM 索引条目模型
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
        /// 参考文献
        /// </summary>
        public string Reference { get; set; } = string.Empty;

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
        /// 索引版本
        /// </summary>
        public string IndexVersion { get; set; } = "1.0.0";
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
