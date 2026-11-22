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

        /// <summary>
        /// 是否已收藏
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// 安装状态：NOT_INSTALLED / UP_TO_DATE / OUTDATED / REQUIRES_APP_UPGRADE
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 服务器清单中的内容哈希（用于本地修改后判定 OUTDATED；未安装占位时与 FileHash 相同）
        /// </summary>
        public string ServerHash { get; set; } = string.Empty;

        /// <summary>
        /// 开发者本地修改了官方温压计、尚未发布（与图解的 PendingPublish 含义一致）。
        /// 发布成功后清除；目录同步时带有该标记的本地官方项不会被当作“已下架”删除。
        /// </summary>
        public bool PendingPublish { get; set; }

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
        /// 能力标签（P / T / fO2 等，标识可计算的结果类型；允许自定义简写）
        /// </summary>
        public List<string> Capabilities { get; set; } = new();

        /// <summary>
        /// 温压计名称（显示名称）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 温压计名称的多语言键名
        /// </summary>
        public string NameLangKey { get; set; } = string.Empty;

        /// <summary>
        /// 模板文本支持的语言（格式 1.2 起写入；第一项为默认语言，旧模板为空）
        /// </summary>
        public List<string> Languages { get; set; } = new();

        /// <summary>
        /// 各语言的模板名称（语言代码 → 文本；Name 保存默认语言文本）
        /// </summary>
        public Dictionary<string, string> NameLang { get; set; } = new();

        /// <summary>
        /// 各语言的参考文献（语言代码 → 文本；Reference 保存默认语言文本）
        /// </summary>
        public Dictionary<string, string> ReferenceLang { get; set; } = new();

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
        /// 各列单位（格式 1.3 新增，可选）：按位置与 <see cref="Headers"/> 一一对应，没有单位的列为空串；
        /// 旧模板或完全没有单位时为空列表。
        /// </summary>
        public List<string> Units { get; set; } = new();

        /// <summary>
        /// 各列的分组标题（格式 1.3 新增，可选）：按位置与 <see cref="Headers"/> 一一对应，
        /// 每项是“语言代码 → 标题”（语言取自 <see cref="Languages"/>，第一项为默认语言），没有分组的列为空字典；
        /// 相邻且标题相同的列在表头上合并成一条分组带。旧模板或完全没有分组时为空列表。
        /// </summary>
        public List<Dictionary<string, string>> Groups { get; set; } = new();

        /// <summary>
        /// 示例数据行
        /// </summary>
        public List<string> ExampleRow { get; set; } = new();

        /// <summary>
        /// 附加示例行（第 2 条示例起，格式 1.3 新增，可选）：每行与 <see cref="Headers"/> 等长，只填输入列的值，
        /// 输出列由 <see cref="ExampleRow"/> 中的公式按行推算；没有更多示例时为空列表。
        /// </summary>
        public List<List<string>> ExtraExampleRows { get; set; } = new();

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

        private bool? _isInstalledOverride;

        /// <summary>
        /// 本地是否已有脚本（已安装）。官方目录占位记录没有脚本。
        /// 完整实体按 ScriptContent 计算；摘要查询不加载脚本，由数据库服务在投影时写入。
        /// 不入库。
        /// </summary>
        [BsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsInstalled
        {
            get => _isInstalledOverride ?? !string.IsNullOrEmpty(ScriptContent);
            set => _isInstalledOverride = value;
        }

        // --- 附属资源区 ---

        /// <summary>
        /// 多语言帮助文档内容
        /// Key: 语言代码 (如 "zh-CN", "en-US")
        /// Value: RTF 文本内容
        /// </summary>
        public Dictionary<string, string> HelpDocuments { get; set; } = new();
    }
}
