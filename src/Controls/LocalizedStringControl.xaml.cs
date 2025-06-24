using GeoChemistryNexus.Helpers;
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
    public partial class LocalizedStringControl : UserControl
    {
        // 防止在通过代码更新UI时，UI的事件又反过来更新属性
        private bool _isUpdatingFromSource = false;

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(LocalizedString), typeof(LocalizedStringControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public LocalizedString Value
        {
            get { return (LocalizedString)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public LocalizedStringControl()
        {
            InitializeComponent();
            // 监听UI控件的Text变化事件，当用户输入时实时更新数据源
            DisplayTextBox.TextChanged += OnTextChanged;
        }

        /// <summary>
        /// 当Value依赖属性变化时，更新UI
        /// </summary>
        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LocalizedStringControl)d;
            var localizedString = e.NewValue as LocalizedString;

            // 标志位，表示本次UI更新来源于数据源，避免触发循环更新
            control._isUpdatingFromSource = true;

            if (localizedString != null)
            {
                string currentLanguage = LanguageService.CurrentLanguage;
                control.DisplayTextBox.Text = localizedString.Get();
            }
            else
            {
                control.DisplayTextBox.Text = string.Empty;
            }

            // 更新结束，重置标志位
            control._isUpdatingFromSource = false;
        }

        /// <summary>
        /// 当UI控件的值被用户修改时，实时更新Value依赖属性
        /// </summary>
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // 如果UI的更新是代码触发的，则直接返回
            if (_isUpdatingFromSource) return;
            if (Value == null) return;

            // 触发绑定更新
            var newText = DisplayTextBox.Text;
            var currentLanguage = LanguageService.CurrentLanguage;

            // 创建一个新实例
            var newLocalizedString = new LocalizedString
            {
                Default = Value.Default,
                // 创建字典的新副本，以防原始字典被意外修改
                Translations = new Dictionary<string, string>(Value.Translations)
            };
            newLocalizedString.Translations[currentLanguage] = newText;

            // 将新实例赋值给依赖属性
            this.Value = newLocalizedString;
        }
    }
}