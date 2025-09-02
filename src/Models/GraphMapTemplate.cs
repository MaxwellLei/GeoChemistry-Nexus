using GeoChemistryNexus.Helpers;
using OpenTK.Graphics.OpenGL;
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
        /// <param name="type">底图类型</param>
        /// <param name="category">底图分类的占位符文本</param>
        /// <returns></returns>
        /// <summary>
        /// 静态工厂——生成最小化类对象
        /// </summary>
        /// <param name="languages">底图支持的语言列表，第一个为默认语言</param>
        /// <param name="type">底图类型: "2D_Plot" 或 "Ternary_Plot"</param>
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

            // 创建模板基础结构
            var template = new GraphMapTemplate
            {
                DefaultLanguage = defaultLanguage,
                // 根据传入的type设置模板类型
                TemplateType = type == "Ternary_Plot" ? "Ternary" : "Cartesian",
                NodeList = CreateLocalized(category),
                Script = new ScriptDefinition(),
                Info = new GraphMapInfo()
            };

            // 根据模板类型配置不同的默认值
            if (template.TemplateType == "Ternary")
            {
                // 三元相图的特定设置
                string titlePlaceholder = defaultLanguage == "zh-CN" ? "新建三元相图" : "New Ternary Plot";
                string componentAPlaceholder = "组分A";
                string componentBPlaceholder = "组分B";
                string componentCPlaceholder = "组分C";

                template.Info.Title = new TitleDefinition
                {
                    Label = CreateLocalized(titlePlaceholder)
                };
                template.Info.Axes = new List<AxisDefinition>
        {
                    // 为三元图的三个边定义坐标轴
                    new AxisDefinition { Type = "Bottom", Label = CreateLocalized(componentAPlaceholder) },
          new AxisDefinition { Type = "Left", Label = CreateLocalized(componentBPlaceholder) },
          new AxisDefinition { Type = "Right", Label = CreateLocalized(componentCPlaceholder) }
        };
                template.Script = new ScriptDefinition
                {
                    // 脚本需要A,B,C三个组分数据来计算二维坐标
                    RequiredDataSeries = "Category,A,B,C",
                    // 注意：此脚本为占位符，用户需要提供从(A,B,C)到(x,y)的正确转换逻辑
                    ScriptBody = "var y = B * Math.sin(Math.PI / 3);\nvar x = A + B * Math.cos(Math.PI / 3);\nreturn [x, y];"
                };
            }
            else // 默认处理笛卡尔坐标系 (2D_Plot)
            {
                string titlePlaceholder = defaultLanguage == "zh-CN" ? "新建图表" : "New Chart";
                string xAxisPlaceholder = defaultLanguage == "zh-CN" ? "X轴" : "X-Axis";
                string yAxisPlaceholder = defaultLanguage == "zh-CN" ? "Y轴" : "Y-Axis";

                template.Info.Title = new TitleDefinition
                {
                    Label = CreateLocalized(titlePlaceholder)
                };
                template.Info.Axes = new List<AxisDefinition>
                {
                  new AxisDefinition { Type = "Bottom", Label = CreateLocalized(xAxisPlaceholder) },
                  new AxisDefinition { Type = "Left", Label = CreateLocalized(yAxisPlaceholder) },
                  new AxisDefinition { Type = "Top", Label = CreateLocalized("") },
                  new AxisDefinition { Type = "Right", Label = CreateLocalized("") }
                };
                template.Script = new ScriptDefinition
                {
                    RequiredDataSeries = "Category,X,Y",
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
