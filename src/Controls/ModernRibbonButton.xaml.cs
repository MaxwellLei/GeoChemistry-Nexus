using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// ModernRibbonButton.xaml 的交互逻辑
    /// </summary>
    public partial class ModernRibbonButton : UserControl
    {
        public ModernRibbonButton()
        {
            InitializeComponent();
        }

        // 图标代码
        public static readonly DependencyProperty IconCodeProperty =
            DependencyProperty.Register("IconCode", typeof(string), typeof(ModernRibbonButton), new PropertyMetadata(string.Empty));

        public string IconCode
        {
            get { return (string)GetValue(IconCodeProperty); }
            set { SetValue(IconCodeProperty, value); }
        }

        // 按钮下方的文字
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ModernRibbonButton), new PropertyMetadata(string.Empty));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // 图标颜色
        public static readonly DependencyProperty IconBrushProperty =
            DependencyProperty.Register("IconBrush", typeof(Brush), typeof(ModernRibbonButton), new PropertyMetadata(Brushes.Black));

        public Brush IconBrush
        {
            get { return (Brush)GetValue(IconBrushProperty); }
            set { SetValue(IconBrushProperty, value); }
        }

        // 下拉菜单的内容
        public static readonly DependencyProperty MenuContentProperty =
            DependencyProperty.Register("MenuContent", typeof(object), typeof(ModernRibbonButton), new PropertyMetadata(null));

        public object MenuContent
        {
            get { return GetValue(MenuContentProperty); }
            set { SetValue(MenuContentProperty, value); }
        }

        // 字体路径
        public static readonly DependencyProperty IconFontFamilyProperty =
            DependencyProperty.Register("IconFontFamily", typeof(FontFamily), typeof(ModernRibbonButton),
                new PropertyMetadata(new FontFamily(new Uri("pack://application:,,,/"), "./Data/Icon/#iconfont")));

        public FontFamily IconFontFamily
        {
            get { return (FontFamily)GetValue(IconFontFamilyProperty); }
            set { SetValue(IconFontFamilyProperty, value); }
        }

        // 点击菜单项关闭菜单栏
        private void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (MainToggle.IsChecked == true)
            {
                MainToggle.IsChecked = false;
            }
        }
    }
}
