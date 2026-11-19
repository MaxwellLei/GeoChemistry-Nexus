using GeoChemistryNexus.Helpers;
using ScottPlot;
using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 工具图解模板工厂，提供无需依赖模板库文件的内置图解模板。
    /// </summary>
    public static class ToolDiagramTemplateFactory
    {
        private const string DefaultLanguage = "en-US";

        /// <summary>
        /// 创建二维散点图的默认笛卡尔模板。
        /// 坐标轴使用通用名称 X / Y，用户后续自行映射具体指标。
        /// </summary>
        public static GraphMapTemplate CreateScatter2DTemplate()
        {
            var template = CreateBaseTemplate(
                "2D_Plot",
                CreateLocalized("2D Scatter Plot", "二维散点图"));

            template.TemplateType = "Cartesian";

            template.Info.Axes = new List<BaseAxisDefinition>
            {
                new CartesianAxisDefinition
                {
                    Type = "Bottom",
                    Label = CreateLocalized("X", "X")
                },
                new CartesianAxisDefinition
                {
                    Type = "Left",
                    Label = CreateLocalized("Y", "Y")
                }
            };

            template.Info.Legend = new LegendDefinition
            {
                IsVisible = true,
                Alignment = Alignment.UpperRight
            };

            template.Info.Grid = new GridDefinition
            {
                MajorGridLineIsVisible = true,
                MajorGridLineColor = "#E0E0E0",
                MajorGridLineWidth = 1
            };

            template.Script = new ScriptDefinition
            {
                RequiredDataSeries = "X,Y",
                ScriptBody = "return [X, Y];"
            };

            return template;
        }

        /// <summary>
        /// 创建三元散点图的默认三元模板。
        /// 三个顶点组分使用通用名称 A / B / C，数据表按 A,B,C 三列录入，脚本直接返回三组分值。
        /// </summary>
        public static GraphMapTemplate CreateTernaryScatterTemplate()
        {
            var template = CreateBaseTemplate(
                "Ternary_Plot",
                CreateLocalized("Ternary Scatter Plot", "三元散点图"));

            template.TemplateType = "Ternary";
            template.Clockwise = true;

            template.Info.Axes = new List<BaseAxisDefinition>
            {
                new TernaryAxisDefinition
                {
                    Type = "Bottom",
                    Label = CreateLocalized("A", "A"),
                    LabelOffsetX = 0,
                    LabelOffsetY = 20
                },
                new TernaryAxisDefinition
                {
                    Type = "Left",
                    Label = CreateLocalized("B", "B"),
                    LabelOffsetX = -20,
                    LabelOffsetY = -10
                },
                new TernaryAxisDefinition
                {
                    Type = "Right",
                    Label = CreateLocalized("C", "C"),
                    LabelOffsetX = 20,
                    LabelOffsetY = -10
                }
            };

            template.Info.Legend = new LegendDefinition
            {
                IsVisible = true,
                Alignment = Alignment.UpperRight
            };

            template.Info.Grid = new GridDefinition
            {
                MajorGridLineIsVisible = true,
                MajorGridLineColor = "#E0E0E0",
                MajorGridLineWidth = 1
            };

            template.Script = new ScriptDefinition
            {
                RequiredDataSeries = "A,B,C",
                ScriptBody = "return [A, B, C];"
            };

            return template;
        }

        /// <summary>
        /// 创建带统一标题、语言和版本信息的基础模板。
        /// </summary>
        private static GraphMapTemplate CreateBaseTemplate(string plotType, LocalizedString title)
        {
            var languages = new List<string> { "en-US", "zh-CN" };

            var template = GraphMapTemplate.CreateDefault(languages, plotType, title);
            template.Version = ContentVersionHelper.GetDiagramFormatVersion();
            template.DefaultLanguage = DefaultLanguage;

            template.Info.Title = new TitleDefinition
            {
                Label = CreateLocalized(
                    title.Translations["en-US"],
                    title.Translations["zh-CN"]),
                Family = "Arial",
                Size = 24,
                Color = "#000000",
                IsBold = true,
                IsItalic = false
            };

            return template;
        }

        private static LocalizedString CreateLocalized(string enUs, string zhCn)
        {
            return new LocalizedString
            {
                Translations = new Dictionary<string, string>
                {
                    { "en-US", enUs },
                    { "zh-CN", zhCn }
                }
            };
        }
    }
}
