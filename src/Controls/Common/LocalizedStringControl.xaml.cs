using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    public partial class LocalizedStringControl : UserControl
    {
        private bool _isUpdatingFromSource = false;

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(LocalizedString), typeof(LocalizedStringControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty ContentLanguageContextProperty =
            DependencyProperty.Register(
                nameof(ContentLanguageContext),
                typeof(ContentLanguageContext),
                typeof(LocalizedStringControl),
                new PropertyMetadata(null, OnContentLanguageContextChanged));

        /// <summary>内部文本框使用的样式（如属性面板的 InspectorTextBoxStyle）；为空时沿用默认样式。</summary>
        public static readonly DependencyProperty TextBoxStyleProperty =
            DependencyProperty.Register(nameof(TextBoxStyle), typeof(Style), typeof(LocalizedStringControl),
                new PropertyMetadata(null, OnTextBoxStyleChanged));

        /// <summary>是否在文本框右下角显示可拖动调整高度的角标。</summary>
        public static readonly DependencyProperty AllowVerticalResizeProperty =
            DependencyProperty.Register(nameof(AllowVerticalResize), typeof(bool), typeof(LocalizedStringControl),
                new PropertyMetadata(false, OnAllowVerticalResizeChanged));

        /// <summary>是否在文本框右上角显示当前内容语言角标。</summary>
        public static readonly DependencyProperty ShowLanguageChipProperty =
            DependencyProperty.Register(nameof(ShowLanguageChip), typeof(bool), typeof(LocalizedStringControl),
                new PropertyMetadata(false, OnLanguageChipLayoutChanged));

        /// <summary>单行模式：不接受回车换行、不自动换行（标题、坐标轴名称等一行内容使用）。</summary>
        public static readonly DependencyProperty IsSingleLineProperty =
            DependencyProperty.Register(nameof(IsSingleLine), typeof(bool), typeof(LocalizedStringControl),
                new PropertyMetadata(false, OnIsSingleLineChanged));

        public bool IsSingleLine
        {
            get => (bool)GetValue(IsSingleLineProperty);
            set => SetValue(IsSingleLineProperty, value);
        }

        private static void OnIsSingleLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LocalizedStringControl control)
            {
                bool single = (bool)e.NewValue;
                control.DisplayTextBox.AcceptsReturn = !single;
                control.DisplayTextBox.TextWrapping = single ? TextWrapping.NoWrap : TextWrapping.Wrap;
                control.UpdateLanguageChipLayout();
            }
        }

        private static readonly DependencyPropertyKey LanguageChipTextPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(LanguageChipText), typeof(string), typeof(LocalizedStringControl),
                new PropertyMetadata(string.Empty));

        /// <summary>语言角标文本（如 简体中文 / English）。</summary>
        public static readonly DependencyProperty LanguageChipTextProperty = LanguageChipTextPropertyKey.DependencyProperty;

        public bool ShowLanguageChip
        {
            get => (bool)GetValue(ShowLanguageChipProperty);
            set => SetValue(ShowLanguageChipProperty, value);
        }

        public string LanguageChipText => (string)GetValue(LanguageChipTextProperty);

        private static void OnLanguageChipLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LocalizedStringControl control) control.UpdateLanguageChipLayout();
        }

        /// <summary>
        /// 语言角标位于输入框内部，但通过 Grid 行列布局“占位”，而不是给文本框加大内边距：
        /// 多行（可拖动调整高度）：文本在上，框内底部一行左侧为语言角标、右侧为拖动调高角标（共用同一行）；
        /// 单行：角标位于框内右侧、垂直居中，文本框占据其左侧。
        /// 显示角标时，外层 FieldHost 负责底色 / 圆角 / 聚焦描边，文本框本身去掉底色与边框。
        /// </summary>
        private void UpdateLanguageChipLayout()
        {
            if (LanguageChip == null) return;

            bool resize = AllowVerticalResize;
            bool single = IsSingleLine;

            // 可拖动调高时：内容始终在左上角（否则调高后文字会垂直居中）
            if (resize) DisplayTextBox.VerticalContentAlignment = VerticalAlignment.Top;
            else DisplayTextBox.ClearValue(Control.VerticalContentAlignmentProperty);

            if (!ShowLanguageChip)
            {
                LanguageChip.Visibility = Visibility.Collapsed;
                // 恢复为单纯的文本框
                FieldHost.ClearValue(Border.BackgroundProperty);
                FieldHost.ClearValue(Border.CornerRadiusProperty);
                FieldHost.ClearValue(Border.BorderThicknessProperty);
                DisplayTextBox.ClearValue(Control.BackgroundProperty);
                DisplayTextBox.ClearValue(Control.BorderThicknessProperty);
                if (resize) DisplayTextBox.Padding = new Thickness(10, 7, 10, 7);
                else DisplayTextBox.ClearValue(Control.PaddingProperty);
                Grid.SetColumnSpan(DisplayTextBox, 3);
                Grid.SetRow(ResizeGrip, 0);
                Grid.SetColumn(ResizeGrip, 0);
                Grid.SetColumnSpan(ResizeGrip, 3);
                ResizeGrip.VerticalAlignment = VerticalAlignment.Bottom;
                ResizeGrip.Margin = new Thickness(0, 0, 3, 3);
                return;
            }

            // 外层作为输入框外观
            FieldHost.Background = TryFindResource("InspectorFieldBrush") as Brush ?? Brushes.Transparent;
            FieldHost.CornerRadius = new CornerRadius(7);
            FieldHost.BorderThickness = new Thickness(1);
            DisplayTextBox.Background = Brushes.Transparent;
            DisplayTextBox.BorderThickness = new Thickness(0);

            LanguageChip.Visibility = Visibility.Visible;
            UpdateLanguageChipText();
            if (resize && !single)
            {
                // 多行：文本在上，底部一行左侧为语言角标、右侧为拖动角标
                DisplayTextBox.Padding = new Thickness(10, 7, 10, 2);
                Grid.SetColumnSpan(DisplayTextBox, 3);
                Grid.SetRow(LanguageChip, 1);
                Grid.SetColumn(LanguageChip, 0);
                LanguageChip.HorizontalAlignment = HorizontalAlignment.Left;
                LanguageChip.VerticalAlignment = VerticalAlignment.Center;
                LanguageChip.Margin = new Thickness(6, 0, 0, 4);
                Grid.SetRow(ResizeGrip, 1);
                Grid.SetColumn(ResizeGrip, 2);
                Grid.SetColumnSpan(ResizeGrip, 1);
                ResizeGrip.VerticalAlignment = VerticalAlignment.Bottom;
                ResizeGrip.Margin = new Thickness(6, 0, 3, 3);
            }
            else
            {
                // 单行：文本框在左，语言角标紧贴其右侧；右侧不留内边距，避免角标左侧出现空白
                DisplayTextBox.Padding = resize ? new Thickness(10, 7, 0, 7) : new Thickness(10, 0, 0, 0);
                Grid.SetColumnSpan(DisplayTextBox, 1);
                Grid.SetRow(LanguageChip, 0);
                Grid.SetColumn(LanguageChip, 1);
                LanguageChip.HorizontalAlignment = HorizontalAlignment.Right;
                LanguageChip.VerticalAlignment = resize ? VerticalAlignment.Top : VerticalAlignment.Center;
                LanguageChip.Margin = resize ? new Thickness(4, 5.5, 6, 0) : new Thickness(4, 0, 6, 0);
                // 可调高的单行框：拖动角标位于角标右侧的单独一列、贴底部
                Grid.SetRow(ResizeGrip, 0);
                Grid.SetColumn(ResizeGrip, 2);
                Grid.SetColumnSpan(ResizeGrip, 1);
                ResizeGrip.VerticalAlignment = VerticalAlignment.Bottom;
                ResizeGrip.Margin = new Thickness(0, 0, 3, 3);
            }
        }

        private void UpdateLanguageChipText()
        {
            string code = ResolveContext()?.ContentLanguage ?? LanguageService.CurrentLanguage ?? string.Empty;
            SetValue(LanguageChipTextPropertyKey, AppLanguageOptionFactory.GetNativeName(code));
        }

        public LocalizedString Value
        {
            get => (LocalizedString)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public Style? TextBoxStyle
        {
            get => (Style?)GetValue(TextBoxStyleProperty);
            set => SetValue(TextBoxStyleProperty, value);
        }

        public bool AllowVerticalResize
        {
            get => (bool)GetValue(AllowVerticalResizeProperty);
            set => SetValue(AllowVerticalResizeProperty, value);
        }

        private static void OnTextBoxStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LocalizedStringControl control && e.NewValue is Style style)
                control.DisplayTextBox.Style = style;
        }

        private static void OnAllowVerticalResizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LocalizedStringControl control)
            {
                control.ResizeGrip.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
                control.UpdateLanguageChipLayout();
            }
        }

        private void ResizeGrip_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double current = double.IsNaN(DisplayTextBox.Height) ? DisplayTextBox.ActualHeight : DisplayTextBox.Height;
            double min = Math.Max(DisplayTextBox.MinHeight, 30);
            DisplayTextBox.Height = Math.Max(min, current + e.VerticalChange);
        }

        public ContentLanguageContext? ContentLanguageContext
        {
            get => (ContentLanguageContext?)GetValue(ContentLanguageContextProperty);
            set => SetValue(ContentLanguageContextProperty, value);
        }

        public LocalizedStringControl()
        {
            InitializeComponent();
            DisplayTextBox.TextChanged += OnTextChanged;
            Loaded += LocalizedStringControl_Loaded;
            Unloaded += LocalizedStringControl_Unloaded;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LocalizedStringControl)d;
            control.UpdateDisplayedText();
        }

        private static void OnContentLanguageContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LocalizedStringControl control && e.OldValue is ContentLanguageContext oldContext)
                oldContext.PropertyChanged -= control.ContentLanguageContext_PropertyChanged;

            if (d is LocalizedStringControl control2 && e.NewValue is ContentLanguageContext newContext)
                newContext.PropertyChanged += control2.ContentLanguageContext_PropertyChanged;

            ((LocalizedStringControl)d).UpdateDisplayedText();
        }

        private void LocalizedStringControl_Loaded(object sender, RoutedEventArgs e)
        {
            LanguageService.Instance.PropertyChanged -= LanguageService_PropertyChanged;
            LanguageService.Instance.PropertyChanged += LanguageService_PropertyChanged;

            if (ContentLanguageContext != null)
                ContentLanguageContext.PropertyChanged += ContentLanguageContext_PropertyChanged;

            UpdateDisplayedText();
        }

        private void LocalizedStringControl_Unloaded(object sender, RoutedEventArgs e)
        {
            LanguageService.Instance.PropertyChanged -= LanguageService_PropertyChanged;

            if (ContentLanguageContext != null)
                ContentLanguageContext.PropertyChanged -= ContentLanguageContext_PropertyChanged;
        }

        private void LanguageService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
                UpdateDisplayedText();
        }

        private void ContentLanguageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ContentLanguage")
                UpdateDisplayedText();
        }

        private ContentLanguageContext? ResolveContext()
        {
            return ContentLanguageContext ?? ContentLanguageScope.Instance.Active;
        }

        private void UpdateDisplayedText()
        {
            _isUpdatingFromSource = true;
            DisplayTextBox.Text = Value?.Get(ResolveContext()) ?? string.Empty;
            _isUpdatingFromSource = false;
            UpdateLanguageChipLayout();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFromSource)
                return;

            Value = BuildValueFromText(DisplayTextBox.Text);
        }

        /// <summary>
        /// 将文本框当前内容同步到 <see cref="Value"/>（用于对话框确认前确保未丢失输入）。
        /// </summary>
        public void CommitPendingText()
        {
            if (_isUpdatingFromSource)
                return;

            Value = BuildValueFromText(DisplayTextBox.Text);
        }

        private LocalizedString BuildValueFromText(string text)
        {
            var current = Value ?? new LocalizedString();
            var updated = new LocalizedString
            {
                Default = current.Default,
                Translations = current.Translations == null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(current.Translations)
            };

            updated.Set(text, ResolveContext());
            return updated;
        }
    }
}
