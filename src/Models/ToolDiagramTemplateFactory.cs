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
        /// <summary>
        /// 创建哈克图解的默认笛卡尔模板。
        /// 默认使用 SiO2 作为 X 轴，Y 轴使用通用名称 Y，用户后续自行映射具体指标。
        /// </summary>
        public static GraphMapTemplate CreateHarkerTemplate()
        {
            var languages = new List<string> { "en-US", "zh-CN" };
            const string defaultLanguage = "en-US";

            var template = GraphMapTemplate.CreateDefault(
                languages,
                "2D_Plot",
                new LocalizedString
                {
                    Translations = new Dictionary<string, string>
                    {
                        { "en-US", "Harker Diagram" },
                        { "zh-CN", "哈克图解" }
                    }
                });

            template.Version = UpdateHelper.GetCurrentVersionFloat();
            template.DefaultLanguage = defaultLanguage;
            template.TemplateType = "Cartesian";

            template.Info.Title = new TitleDefinition
            {
                Label = new LocalizedString
                {
                    Translations = new Dictionary<string, string>
                    {
                        { "en-US", "Harker Diagram" },
                        { "zh-CN", "哈克图解" }
                    }
                },
                Family = "Arial",
                Size = 24,
                Color = "#000000",
                IsBold = true,
                IsItalic = false
            };

            template.Info.Axes = new List<BaseAxisDefinition>
            {
                new CartesianAxisDefinition
                {
                    Type = "Bottom",
                    Label = new LocalizedString
                    {
                        Translations = new Dictionary<string, string>
                        {
                            { "en-US", "SiO2 (wt.%)" },
                            { "zh-CN", "SiO2 (wt.%)" }
                        }
                    }
                },
                new CartesianAxisDefinition
                {
                    Type = "Left",
                    Label = new LocalizedString
                    {
                        Translations = new Dictionary<string, string>
                        {
                            { "en-US", "Y" },
                            { "zh-CN", "Y" }
                        }
                    }
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
                RequiredDataSeries = "SiO2,Y",
                ScriptBody = "return [SiO2, Y];"
            };

            return template;
        }
    }
}
