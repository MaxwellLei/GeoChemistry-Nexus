using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

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

        public LocalizedString Value
        {
            get => (LocalizedString)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
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
