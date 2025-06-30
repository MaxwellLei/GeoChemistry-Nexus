using GeoChemistryNexus.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Models
{
    public class GraphMapTemplate
    {
        /// <summary>
        /// 底图版本
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 默认语言
        /// </summary>
        public string DefaultLanguage { get; set; } = "en-US";

        /// <summary>
        /// 底图类型：笛卡尔坐标系(Cartesian)，三元坐标系(Ternary)
        /// </summary>
        public string TemplateType { get; set; } = "Cartesian";

        /// <summary>
        /// 底图的分类
        /// </summary>
        public LocalizedString NodeList { get; set; } = new LocalizedString();

        /// <summary>
        /// 底图信息
        /// </summary>
        public GraphMapInfo Info { get; set; } = new GraphMapInfo();

        /// <summary>
        /// 脚本信息
        /// </summary>
        public ScriptDefinition Script { get; set; } = new ScriptDefinition();


        /// <summary>
        /// 静态工厂——生成最小化类对象
        /// </summary>
        /// <param name="languages">底图支持的语言列表，第一个为默认语言</param>
        /// <param name="type">底图类型</param>
        /// <param name="category">底图分类的占位符文本</param>
        /// <returns></returns>
        public static GraphMapTemplate CreateDefault(List<string> languages, string type, string category)
        {
            // 如果语言列表为空，则提供一个默认值
            if (languages == null || !languages.Any())
            {
                languages = new List<string> { "en-US" };
            }

            // 将列表的第一个语言作为默认语言
            string defaultLanguage = languages.First();

            // 定义一个本地辅助函数，用于根据语言列表和占位符文本创建 LocalizedString 对象
            LocalizedString CreateLocalized(string placeholderText)
            {
                // 使用 Linq 的 ToDictionary 方法快速创建翻译字典
                var translations = languages.ToDictionary(lang => lang, lang => placeholderText);
                return new LocalizedString
                {
                    Default = defaultLanguage,
                    Translations = translations
                };
            }

            // 根据默认语言选择合适的占位符
            string titlePlaceholder = defaultLanguage == "zh-CN" ? "新建图表" : "New Chart";
            string xAxisPlaceholder = defaultLanguage == "zh-CN" ? "X轴" : "X-Axis";
            string yAxisPlaceholder = defaultLanguage == "zh-CN" ? "Y轴" : "Y-Axis";

            var template = new GraphMapTemplate
            {
                DefaultLanguage = defaultLanguage,
                TemplateType = type,
                NodeList = CreateLocalized(category), // 使用传入的 category 作为占位符
                Script = new ScriptDefinition
                {
                    RequiredDataSeries = "Category,X,Y",
                    ScriptBody = "return [X, Y];"
                },
                Info = new GraphMapInfo
                {
                    Title = new TitleDefinition
                    {
                        Label = CreateLocalized(titlePlaceholder), // 使用占位符
                        Family = "Microsoft YaHei",
                        Size = 16,
                        Color = "#FF000000",
                        IsBold = true
                    },
                    Axes = new List<AxisDefinition>
          {
            new AxisDefinition { Type = "Bottom", Label = CreateLocalized(xAxisPlaceholder) },
            new AxisDefinition { Type = "Left", Label = CreateLocalized(yAxisPlaceholder) },
            new AxisDefinition { Type = "Top", Label = CreateLocalized("") }, // 空标签
                        new AxisDefinition { Type = "Right", Label = CreateLocalized("") } // 空标签
                    },
                    Legend = new LegendDefinition(),
                    Grid = new GridDefinition()
                }
            };
            return template;
        }
    }
}
