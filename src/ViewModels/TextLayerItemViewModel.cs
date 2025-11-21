using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    public class TextLayerItemViewModel : LayerItemViewModel, IPlotLayer
    {
        public TextDefinition TextDefinition { get; }

        public TextLayerItemViewModel(TextDefinition textDefinition, int index)
            : base(LanguageService.Instance["text"] + $" {index + 1}")
        {
            TextDefinition = textDefinition;
        }

        public void Render(Plot plot)
        {
            // 校验
            if (TextDefinition?.StartAndEnd == null) return;

            // 获取文本内容（多语言）
            string content = TextDefinition.Content.Get();

            // 将存储的真实数据坐标转换为绘图坐标（处理 Log 逻辑）
            var renderLocation = PlotTransformHelper.ToRenderCoordinates(
                plot,
                TextDefinition.StartAndEnd.X,
                TextDefinition.StartAndEnd.Y
            );

            // 使用转换后的坐标添加文本对象
            var textPlot = plot.Add.Text(content, renderLocation);

            // --- 应用样式 ---
            textPlot.LabelText = content;

            // 字体自适应检测
            textPlot.LabelFontName = Fonts.Detect(content);
            textPlot.LabelFontSize = TextDefinition.Size;
            textPlot.LabelRotation = TextDefinition.Rotation;

            // 颜色转换
            textPlot.LabelFontColor = ScottPlot.Color.FromHex(
                GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(TextDefinition.Color));

            // 背景色
            textPlot.LabelBackgroundColor = ScottPlot.Color.FromHex(
                GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(TextDefinition.BackgroundColor));

            // 边框色
            textPlot.LabelBorderColor = ScottPlot.Color.FromHex(
                GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(TextDefinition.BorderColor));

            textPlot.LabelBorderWidth = TextDefinition.BorderWidth;
            textPlot.LabelBorderRadius = TextDefinition.FilletRadius;
            textPlot.LabelBold = TextDefinition.IsBold;
            textPlot.LabelItalic = TextDefinition.IsItalic;

            // 对齐方式处理
            // 有个小 BUG 需要处理 todo
            //switch (TextDefinition.ContentHorizontalAlignment)
            //{
            //    case System.Windows.HorizontalAlignment.Left:
            //        textPlot.LabelAlignment = Alignment.LowerRight;
            //        break;
            //    case System.Windows.HorizontalAlignment.Center:
            //        textPlot.LabelAlignment = Alignment.LowerCenter;
            //        break;
            //    case System.Windows.HorizontalAlignment.Right:
            //        textPlot.LabelAlignment = Alignment.LowerLeft;
            //        break;
            //    default:
            //        textPlot.LabelAlignment = Alignment.MiddleCenter;
            //        break;
            //}

            // 高级渲染：抗锯齿
            textPlot.LabelStyle.AntiAliasText = TextDefinition.AntiAliasEnable;

            // 赋值给基类 Plottable
            this.Plottable = textPlot;
        }

        public void Highlight()
        {
            if (Plottable is ScottPlot.Plottables.Text textPlot)
            {
                textPlot.LabelBorderColor = ScottPlot.Colors.Red;
                textPlot.LabelBorderWidth = 2;
            }
        }

        public void Dim()
        {
            if (Plottable is ScottPlot.Plottables.Text textPlot)
            {
                byte dimAlpha = 60;
                // 文字颜色、背景、边框全部变暗
                textPlot.LabelFontColor = textPlot.LabelFontColor.WithAlpha(dimAlpha);

                if (textPlot.LabelBackgroundColor != ScottPlot.Colors.Transparent)
                    textPlot.LabelBackgroundColor = textPlot.LabelBackgroundColor.WithAlpha(dimAlpha);

                if (textPlot.LabelBorderColor != ScottPlot.Colors.Transparent)
                    textPlot.LabelBorderColor = textPlot.LabelBorderColor.WithAlpha(dimAlpha);
            }
        }

        public void Restore()
        {
            if (Plottable is ScottPlot.Plottables.Text textPlot)
            {
                // 恢复颜色
                textPlot.LabelFontColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(TextDefinition.Color));

                // 恢复背景
                textPlot.LabelBackgroundColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(TextDefinition.BackgroundColor));

                // 恢复边框
                textPlot.LabelBorderColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(TextDefinition.BorderColor));
                textPlot.LabelBorderWidth = TextDefinition.BorderWidth;
            }
        }
    }
}
