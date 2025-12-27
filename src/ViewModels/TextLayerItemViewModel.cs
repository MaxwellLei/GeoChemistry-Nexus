using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;

namespace GeoChemistryNexus.ViewModels
{
    public class TextLayerItemViewModel : LayerItemViewModel, IPlotLayer
    {
        public TextDefinition TextDefinition { get; }
        private readonly int _index;

        public TextLayerItemViewModel(TextDefinition textDefinition, int index)
            : base(GetName(textDefinition, index))
        {
            TextDefinition = textDefinition;
            _index = index;
            PropertyChangedEventManager.AddHandler(TextDefinition, OnTextDefinitionChanged, string.Empty);
            
            // 监听 Model 变化触发刷新
            TextDefinition.PropertyChanged += (s, e) => OnRefreshRequired();
            if (TextDefinition.StartAndEnd != null) TextDefinition.StartAndEnd.PropertyChanged += (s, e) => OnRefreshRequired();
            
            TextDefinition.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(TextDefinition.StartAndEnd) && TextDefinition.StartAndEnd != null)
                    TextDefinition.StartAndEnd.PropertyChanged += (sender, args) => OnRefreshRequired();
            };
        }

        private static string GetName(TextDefinition textDefinition, int index)
        {
            var content = textDefinition.Content.Get();
            if (string.IsNullOrWhiteSpace(content))
            {
                return LanguageService.Instance["text"] + $" {index + 1}";
            }

            // Replace newlines with spaces to keep the name on a single line
            return content.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
        }

        private void OnTextDefinitionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TextDefinition.Content))
            {
                Name = GetName(TextDefinition, _index);
            }
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
            textPlot.LabelFontName = TextDefinition.Family;
            textPlot.LabelFontSize = TextDefinition.Size;
            textPlot.LabelRotation = TextDefinition.Rotation;

            // 颜色转换
            textPlot.LabelFontColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(TextDefinition.Color));

            // 背景色
            textPlot.LabelBackgroundColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(TextDefinition.BackgroundColor));

            // 边框色
            textPlot.LabelBorderColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(TextDefinition.BorderColor));

            textPlot.LabelBorderWidth = TextDefinition.BorderWidth;
            textPlot.LabelBorderRadius = TextDefinition.FilletRadius;
            textPlot.LabelBold = TextDefinition.IsBold;
            textPlot.LabelItalic = TextDefinition.IsItalic;

            // 对齐方式处理
            switch (TextDefinition.ContentHorizontalAlignment)
            {
                case TextAlignment.Left:
                    textPlot.LabelAlignment = Alignment.UpperLeft;
                    break;
                case TextAlignment.Center:
                    textPlot.LabelAlignment = Alignment.UpperCenter;
                    break;
                case TextAlignment.Right:
                    textPlot.LabelAlignment = Alignment.UpperRight;
                    break;
                default:
                    textPlot.LabelAlignment = Alignment.UpperLeft;
                    break;
            }

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
                textPlot.LabelFontColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(TextDefinition.Color));

                // 恢复背景
                textPlot.LabelBackgroundColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(TextDefinition.BackgroundColor));

                // 恢复边框
                textPlot.LabelBorderColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(TextDefinition.BorderColor));
                textPlot.LabelBorderWidth = TextDefinition.BorderWidth;

                // 恢复字体样式
                textPlot.LabelFontSize = TextDefinition.Size;
                textPlot.LabelRotation = TextDefinition.Rotation;
                textPlot.LabelBold = TextDefinition.IsBold;
                textPlot.LabelItalic = TextDefinition.IsItalic;
            }
        }
    }
}
