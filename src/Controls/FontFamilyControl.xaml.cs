using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeoChemistryNexus.Controls
{
    public partial class FontFamilyControl : UserControl
    {
        private bool _isUpdatingFromSource = false;
        private List<string> _fontNames;

        public static readonly DependencyProperty FontFamilyNameProperty =
            DependencyProperty.Register("FontFamilyName", typeof(string), typeof(FontFamilyControl),
                new FrameworkPropertyMetadata("Arial", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFontFamilyNameChanged));

        public string FontFamilyName
        {
            get { return (string)GetValue(FontFamilyNameProperty); }
            set { SetValue(FontFamilyNameProperty, value); }
        }

        public FontFamilyControl()
        {
            InitializeComponent();

            // 获取字体列表
            _fontNames = FontService.GetFontNames();

            FontComboBox.ItemsSource = _fontNames;
            FontComboBox.SelectionChanged += OnComboBoxSelectionChanged;

            // 订阅 Loaded 事件
            this.Loaded += FontFamilyControl_Loaded;
        }

        // Loaded 事件的处理程序
        private void FontFamilyControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 当控件完全加载后，将依赖属性的当前值设置给
            SetSelectedFont(this.FontFamilyName);

            // 取消订阅以节省资源
            this.Loaded -= FontFamilyControl_Loaded;
        }

        private static void OnFontFamilyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FontFamilyControl)d;

            // 确保只有在控件加载后才尝试更新UI
            if (!control.IsLoaded)
            {
                return;
            }

            var newFontName = e.NewValue as string;
            control.SetSelectedFont(newFontName);
        }

        private void SetSelectedFont(string fontName)
        {
            if (string.IsNullOrEmpty(fontName) || _fontNames == null)
                return;

            _isUpdatingFromSource = true;

            // 查找匹配的字体名称
            var matchedFont = _fontNames.FirstOrDefault(f =>
                string.Equals(f, fontName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(matchedFont))
            {
                FontComboBox.SelectedItem = matchedFont;
            }
            else
            {
                // 如果没有找到精确匹配，尝试部分匹配
                matchedFont = _fontNames.FirstOrDefault(f =>
                    f.IndexOf(fontName, StringComparison.OrdinalIgnoreCase) >= 0);

                if (!string.IsNullOrEmpty(matchedFont))
                {
                    FontComboBox.SelectedItem = matchedFont;
                }
                else
                {
                    // 如果仍然没有找到，设置为默认字体
                    var defaultFont = _fontNames.FirstOrDefault(f =>
                        string.Equals(f, "Arial", StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(defaultFont))
                    {
                        FontComboBox.SelectedItem = defaultFont;
                    }
                }
            }

            _isUpdatingFromSource = false;
        }

        private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingFromSource) return;

            if (FontComboBox.SelectedItem != null)
            {
                FontFamilyName = FontComboBox.SelectedItem.ToString();
            }
        }
    }
}