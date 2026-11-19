using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 应用级 UI 缩放：以固定倍率（默认 100%）整体放大界面内容。
    /// </summary>
    public static class UiScaleHelper
    {
        public const string ConfigKey = ConfigKeys.UiScaleMode;

        /// <summary>
        /// 全局文字排版模式资源键（Application 资源，XAML 中以 DynamicResource 引用）。
        /// 100% 时使用 Display（贴合像素网格，最清晰）；非 100%（放大或缩小）时使用 Ideal，
        /// 避免按 1x 像素网格对齐（hinting）的文字经缩放变换后发虚。
        /// </summary>
        public const string TextFormattingModeResourceKey = "AppTextFormattingMode";

        private sealed record ChromeMetrics(double CaptionHeight, Thickness ResizeBorderThickness);

        private static readonly DependencyProperty DesignChromeMetricsProperty =
            DependencyProperty.RegisterAttached(
                "DesignChromeMetrics",
                typeof(ChromeMetrics),
                typeof(UiScaleHelper),
                new PropertyMetadata(null));

        private static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached(
                "IsUiScaleAttached",
                typeof(bool),
                typeof(UiScaleHelper),
                new PropertyMetadata(false));

        private static readonly DependencyProperty LastScaleProperty =
            DependencyProperty.RegisterAttached(
                "LastUiScale",
                typeof(double),
                typeof(UiScaleHelper),
                new PropertyMetadata(1.0));

        private static readonly List<WeakReference<Window>> AttachedWindows = new();

        public static UiScaleMode GetMode()
        {
            return ParseMode(ConfigHelper.GetConfig(ConfigKey));
        }

        public static void SetMode(UiScaleMode mode)
        {
            ConfigHelper.SetConfig(ConfigKey, ModeToConfigValue(mode));
            UpdateTextFormattingMode(ModeToFixedScale(mode));
            RefreshAllAttachedWindows();
            WeakReferenceMessenger.Default.Send(new UiScaleModeChangedMessage(mode));
        }

        /// <summary>
        /// 解析配置值；空值或无法识别的值（含旧版本的 "Auto"）一律视为 100%。
        /// </summary>
        public static UiScaleMode ParseMode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return UiScaleMode.Percent100;

            if (int.TryParse(value, out int percent))
            {
                return percent switch
                {
                    90 => UiScaleMode.Percent90,
                    125 => UiScaleMode.Percent125,
                    150 => UiScaleMode.Percent150,
                    _ => UiScaleMode.Percent100
                };
            }

            return value.ToLowerInvariant() switch
            {
                "p90" or "percent90" => UiScaleMode.Percent90,
                "p125" or "percent125" => UiScaleMode.Percent125,
                "p150" or "percent150" => UiScaleMode.Percent150,
                _ => UiScaleMode.Percent100
            };
        }

        public static string ModeToConfigValue(UiScaleMode mode) => mode switch
        {
            UiScaleMode.Percent90 => "90",
            UiScaleMode.Percent125 => "125",
            UiScaleMode.Percent150 => "150",
            _ => "100"
        };

        public static double ModeToFixedScale(UiScaleMode mode) => mode switch
        {
            UiScaleMode.Percent90 => 0.9,
            UiScaleMode.Percent125 => 1.25,
            UiScaleMode.Percent150 => 1.5,
            _ => 1.0
        };

        /// <summary>
        /// 当前应使用的 UI 缩放倍率（全局固定）。
        /// </summary>
        public static double ComputeScale(Window window)
        {
            return ModeToFixedScale(GetMode());
        }

        /// <summary>
        /// 将窗口接入 UI 缩放（内容 LayoutTransform + WindowChrome 同步）。
        /// </summary>
        public static void Attach(Window window)
        {
            if (window == null || (bool)window.GetValue(IsAttachedProperty))
                return;

            window.SetValue(IsAttachedProperty, true);
            AttachedWindows.Add(new WeakReference<Window>(window));

            UpdateTextFormattingMode(ComputeScale(window));

            window.SourceInitialized += (_, _) => Apply(window);
            window.Loaded += (_, _) => Apply(window);
            window.DpiChanged += (_, _) => Apply(window);
            window.StateChanged += (_, _) => Apply(window);

            if (window.IsLoaded || PresentationSource.FromVisual(window) != null)
                Apply(window);
        }

        public static void RefreshAllAttachedWindows()
        {
            for (int i = AttachedWindows.Count - 1; i >= 0; i--)
            {
                if (!AttachedWindows[i].TryGetTarget(out var window) || window == null)
                {
                    AttachedWindows.RemoveAt(i);
                    continue;
                }

                Apply(window);
            }
        }

        public static void Apply(Window window)
        {
            if (window?.Content is not FrameworkElement content)
                return;

            var scale = ComputeScale(window);
            var lastScale = (double)window.GetValue(LastScaleProperty);

            if (Math.Abs(lastScale - scale) < 0.001 && content.LayoutTransform is ScaleTransform)
            {
                SyncWindowChrome(window, scale);
                return;
            }

            if (content.LayoutTransform is ScaleTransform existing)
            {
                existing.ScaleX = scale;
                existing.ScaleY = scale;
            }
            else
            {
                content.LayoutTransform = new ScaleTransform(scale, scale);
            }

            // 非 100% 时图片使用高质量重采样，避免位图被拉伸 / 压缩后发糊
            if (Math.Abs(scale - 1.0) > 0.001)
                RenderOptions.SetBitmapScalingMode(content, BitmapScalingMode.HighQuality);
            else
                content.ClearValue(RenderOptions.BitmapScalingModeProperty);

            window.SetValue(LastScaleProperty, scale);
            SyncWindowChrome(window, scale);
        }

        /// <summary>
        /// 根据缩放倍率切换全局文字排版模式（见 <see cref="TextFormattingModeResourceKey"/>）。
        /// </summary>
        private static void UpdateTextFormattingMode(double scale)
        {
            var app = Application.Current;
            if (app == null)
                return;

            var mode = Math.Abs(scale - 1.0) > 0.001 ? TextFormattingMode.Ideal : TextFormattingMode.Display;
            if (app.Resources[TextFormattingModeResourceKey] is TextFormattingMode current && current == mode)
                return;

            app.Resources[TextFormattingModeResourceKey] = mode;
        }

        private static void SyncWindowChrome(Window window, double scale)
        {
            var chrome = WindowChrome.GetWindowChrome(window);
            if (chrome == null)
                return;

            // 保留各窗口自己的设计值：主窗口使用页面拖动条，编辑窗口使用原生 Caption。
            // 只记录一次，避免切换缩放或最大化/还原时在已缩放的值上继续累乘。
            if (window.GetValue(DesignChromeMetricsProperty) is not ChromeMetrics metrics)
            {
                metrics = new ChromeMetrics(chrome.CaptionHeight, chrome.ResizeBorderThickness);
                window.SetValue(DesignChromeMetricsProperty, metrics);
            }

            chrome.CaptionHeight = metrics.CaptionHeight * scale;
            var border = metrics.ResizeBorderThickness;
            chrome.ResizeBorderThickness = new Thickness(
                border.Left * scale, border.Top * scale, border.Right * scale, border.Bottom * scale);
        }
    }
}
