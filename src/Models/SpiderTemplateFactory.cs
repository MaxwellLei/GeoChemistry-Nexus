using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models.SpiderDiagram;
using ScottPlot;
using System.Collections.Generic;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 蜘蛛图模板工厂类，用于生成内置的REE和微量元素蛛网图模板
    /// </summary>
    public static class SpiderTemplateFactory
    {
        /// <summary>
        /// 创建REE蛛网图模板
        /// </summary>
        public static GraphMapTemplate CreateReeSpiderTemplate()
        {
            var languages = new List<string> { "en-US" };
            var defaultLang = "en-US";

            var template = new GraphMapTemplate
            {
                Version = UpdateHelper.GetCurrentVersionFloat(),
                DefaultLanguage = defaultLang,
                TemplateType = "Spider",
                NodeList = new LocalizedString
                {
                    Translations = new Dictionary<string, string>
                    {
                        { "en-US", "REE Spider Diagram" },
                        { "zh-CN", "REE蛛网图" }
                    }
                },
                Info = new GraphMapInfo
                {
                    Title = new TitleDefinition
                    {
                        Label = new LocalizedString
                        {
                            Translations = new Dictionary<string, string>
                            {
                                { "en-US", "REE Spider Diagram" },
                                { "zh-CN", "REE蛛网图" }
                            }
                        },
                        Family = "Arial",
                        Size = 24,
                        Color = "#000000",
                        IsBold = true,
                        IsItalic = false
                    },
                    Axes = new List<BaseAxisDefinition>
                    {
                        CreateSpiderBottomAxis("REE", NormalizationData.ReeElementOrder),
                        CreateSpiderLeftAxis("REE", NormalizationData.ReeElementOrder)
                    },
                    Legend = new LegendDefinition
                    {
                        IsVisible = true,
                        Alignment = Alignment.UpperRight
                    },
                    Grid = new GridDefinition
                    {
                        MajorGridLineIsVisible = true,
                        MajorGridLineColor = "#E0E0E0",
                        MajorGridLineWidth = 1
                    }
                },
                Script = new ScriptDefinition
                {
                    RequiredDataSeries = "La,Ce,Pr,Nd,Sm,Eu,Gd,Tb,Dy,Ho,Er,Tm,Yb,Lu",
                    ScriptBody = "var elements = ['La','Ce','Pr','Nd','Sm','Eu','Gd','Tb','Dy','Ho','Er','Tm','Yb','Lu'];\nvar validElements = elements.filter(e => typeof data[e] !== 'undefined' && data[e] > 0);\nreturn validElements.map(e => ({element: e, value: Math.log10(data[e] / standard[e])}));"
                }
            };

            return template;
        }

        /// <summary>
        /// 创建微量元素蛛网图模板
        /// </summary>
        public static GraphMapTemplate CreateTraceElementSpiderTemplate()
        {
            var languages = new List<string> { "en-US" };
            var defaultLang = "en-US";

            var template = new GraphMapTemplate
            {
                Version = UpdateHelper.GetCurrentVersionFloat(),
                DefaultLanguage = defaultLang,
                TemplateType = "Spider",
                NodeList = new LocalizedString
                {
                    Translations = new Dictionary<string, string>
                    {
                        { "en-US", "Trace Element Spider Diagram" },
                        { "zh-CN", "微量元素蛛网图" }
                    }
                },
                Info = new GraphMapInfo
                {
                    Title = new TitleDefinition
                    {
                        Label = new LocalizedString
                        {
                            Translations = new Dictionary<string, string>
                            {
                                { "en-US", "Trace Element Spider Diagram" },
                                { "zh-CN", "微量元素蛛网图" }
                            }
                        },
                        Family = "Arial",
                        Size = 24,
                        Color = "#000000",
                        IsBold = true,
                        IsItalic = false
                    },
                    Axes = new List<BaseAxisDefinition>
                    {
                        CreateSpiderBottomAxis("TraceElement", NormalizationData.TraceElementOrder),
                        CreateSpiderLeftAxis("TraceElement", NormalizationData.TraceElementOrder)
                    },
                    Legend = new LegendDefinition
                    {
                        IsVisible = true,
                        Alignment = Alignment.UpperRight
                    },
                    Grid = new GridDefinition
                    {
                        MajorGridLineIsVisible = true,
                        MajorGridLineColor = "#E0E0E0",
                        MajorGridLineWidth = 1
                    }
                },
                Script = new ScriptDefinition
                {
                    RequiredDataSeries = "Rb,Ba,Th,U,Nb,Ta,La,Ce,Pb,Pr,Sr,P,Nd,Zr,Sm,Ti,Y,Yb",
                    ScriptBody = "var elements = ['Rb','Ba','Th','U','Nb','Ta','La','Ce','Pb','Pr','Sr','P','Nd','Zr','Sm','Ti','Y','Yb'];\nvar validElements = elements.filter(e => typeof data[e] !== 'undefined' && data[e] > 0);\nreturn validElements.map(e => ({element: e, value: Math.log10(data[e] / standard[e])}));"
                }
            };

            return template;
        }

        /// <summary>
        /// 创建蜘蛛图底部坐标轴
        /// </summary>
        private static SpiderAxisDefinition CreateSpiderBottomAxis(string spiderType, List<string> elementOrder)
        {
            var standardName = spiderType == "REE"
                ? NormalizationData.GetReeStandards()[0].Name
                : NormalizationData.GetTraceElementStandards()[0].Name;

            return new SpiderAxisDefinition
            {
                Type = "Bottom",
                SpiderType = spiderType,
                ElementOrder = string.Join(",", elementOrder),
                NormalizationStandard = standardName,
                IsNormalizationEnabled = true,
                ScaleType = AxisScaleType.Linear,
                Label = new LocalizedString
                {
                    Translations = new Dictionary<string, string>
                    {
                        { "en-US", "Elements" },
                        { "zh-CN", "元素" }
                    }
                },
                Family = "Arial",
                Size = 12,
                Color = "#000000",
                IsBold = false,
                IsItalic = false
            };
        }

        /// <summary>
        /// 创建蜘蛛图左侧Y轴坐标轴
        /// </summary>
        private static SpiderAxisDefinition CreateSpiderLeftAxis(string spiderType, List<string> elementOrder)
        {
            var standardName = spiderType == "REE"
                ? NormalizationData.GetReeStandards()[0].Name
                : NormalizationData.GetTraceElementStandards()[0].Name;

            return new SpiderAxisDefinition
            {
                Type = "Left",
                SpiderType = spiderType,
                ElementOrder = string.Join(",", elementOrder),
                NormalizationStandard = standardName,
                IsNormalizationEnabled = true,
                ScaleType = AxisScaleType.Linear,
                Label = new LocalizedString
                {
                    Translations = new Dictionary<string, string>
                    {
                        { "en-US", $"Sample / {standardName}" },
                        { "zh-CN", $"样品 / {standardName}" }
                    }
                },
                Family = "Arial",
                Size = 12,
                Color = "#000000",
                IsBold = false,
                IsItalic = false
            };
        }

        /// <summary>
        /// 根据类型获取蜘蛛图模板
        /// </summary>
        public static GraphMapTemplate GetSpiderTemplate(string spiderType)
        {
            return spiderType == "REE" 
                ? CreateReeSpiderTemplate() 
                : CreateTraceElementSpiderTemplate();
        }
    }
}
