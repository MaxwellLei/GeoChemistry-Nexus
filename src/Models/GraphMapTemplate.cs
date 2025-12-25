using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Converter;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    public class GraphMapTemplate
    {
        /// <summary>
        /// 底图版本
        /// </summary>
        [JsonConverter(typeof(StringToFloatConverter))]
        public float Version { get; set; } = 1.0f;

        /// <summary>
        /// 默认语言
        /// </summary>
        public string DefaultLanguage { get; set; } = "en-US";

        /// <summary>
        /// 底图类型：笛卡尔坐标系(Cartesian)，三元坐标系(Ternary)
        /// 后续可能增加其他类型
        /// </summary>
        public string TemplateType { get; set; } = "Cartesian";

        /// <summary>
        /// 仅对三元坐标系有效，是否顺时针方向
        /// </summary>
        public bool Clockwise { get; set; } = true;

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
        /// <param name="type">底图类型: "2D_Plot" 或 "Ternary_Plot"</param>
        /// <param name="categoryNodeList">底图分类的本地化对象</param>
        /// <returns></returns>
        public static GraphMapTemplate CreateDefault(List<string> languages, string type, LocalizedString categoryNodeList)
        {
            // 如果语言列表为空，则提供一个默认值
            if (languages == null || !languages.Any())
            {
                languages = new List<string> { "en-US" };
            }

            // 将列表的第一个语言作为默认语言
            string defaultLanguage = languages.First();

            // 创建模板基础结构
            var template = new GraphMapTemplate
            {
                Version = UpdateHelper.GetCurrentVersionFloat(),
                DefaultLanguage = defaultLanguage,
                // 根据传入的type设置模板类型
                TemplateType = type == "Ternary_Plot" ? "Ternary" : "Cartesian",
                NodeList = categoryNodeList,
                Script = new ScriptDefinition(),
                Info = new GraphMapInfo()
            };

            // 根据模板类型配置不同的默认值
            if (template.TemplateType == "Ternary")
            {
                template.Info.Title = new TitleDefinition
                {
                    Label = LocalizedPlaceholderFactory.Create("Placeholder_Ternary_Title", defaultLanguage, languages)
                };
                template.Info.Axes = new List<BaseAxisDefinition>
                {
                    // 为三元图的三个边定义坐标轴
                    new TernaryAxisDefinition { Type = "Bottom", Label = LocalizedPlaceholderFactory.Create("Placeholder_Component_A", defaultLanguage, languages), LabelOffsetX = 0, LabelOffsetY = 20 },
                    new TernaryAxisDefinition { Type = "Left", Label = LocalizedPlaceholderFactory.Create("Placeholder_Component_B", defaultLanguage, languages), LabelOffsetX = -20, LabelOffsetY = -10 },
                    new TernaryAxisDefinition { Type = "Right", Label = LocalizedPlaceholderFactory.Create("Placeholder_Component_C", defaultLanguage, languages), LabelOffsetX = 20, LabelOffsetY = -10 }
                };
                template.Script = new ScriptDefinition
                {
                    // 脚本需要A,B,C三个组分数据来计算二维坐标
                    RequiredDataSeries = "A,B,C",
                    // 注意：此脚本为占位符
                    ScriptBody = "var y = B * Math.sin(Math.PI / 3);\nvar x = A + B * Math.cos(Math.PI / 3);\nreturn [A, B, C];"
                };
            }
            else // 默认处理笛卡尔坐标系 (2D_Plot)
            {
                template.Info.Title = new TitleDefinition
                {
                    Label = LocalizedPlaceholderFactory.Create("Placeholder_Chart_Title_Default", defaultLanguage, languages)
                };
                template.Info.Axes = new List<BaseAxisDefinition>
                {
                    new CartesianAxisDefinition { Type = "Bottom", Label = LocalizedPlaceholderFactory.Create("Placeholder_Axis_X", defaultLanguage, languages) },
                    new CartesianAxisDefinition { Type = "Left", Label = LocalizedPlaceholderFactory.Create("Placeholder_Axis_Y", defaultLanguage, languages) },
                    //new CartesianAxisDefinition { Type = "Top", Label = CreateLocalized("") },
                    //new CartesianAxisDefinition { Type = "Right", Label = CreateLocalized("") }
                };
                template.Script = new ScriptDefinition
                {
                    RequiredDataSeries = "X,Y",
                    ScriptBody = "return [X, Y];"
                };
            }

            // 通用设置
            template.Info.Legend = new LegendDefinition();
            template.Info.Grid = new GridDefinition();

            return template;
        }
    }
}
