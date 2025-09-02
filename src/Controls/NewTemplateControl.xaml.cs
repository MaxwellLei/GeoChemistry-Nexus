using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// <summary>
    /// NewTemplateControl.xaml 的交互逻辑
    /// </summary>
    public partial class NewTemplateControl : UserControl
    {
        // 为语言和分类层级分别创建集合
        private readonly ObservableCollection<string> _languageParts = new ObservableCollection<string>();
        private readonly ObservableCollection<string> _categoryParts = new ObservableCollection<string>();

        // 修改 Language 属性以从标签集合生成字符串
        public string Language => string.Join(" > ", _languageParts);
        public string CategoryHierarchy => string.Join(" > ", _categoryParts);
        public string PlotType => (PlotTypeComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? PlotTypeComboBox.Text;
        public string FilePath => FilePathTextBox.Text;

        #region ConfirmCommand Dependency Property

        public static readonly DependencyProperty ConfirmCommandProperty =
            DependencyProperty.Register("ConfirmCommand", typeof(ICommand), typeof(NewTemplateControl), new PropertyMetadata(null));

        public ICommand ConfirmCommand
        {
            get { return (ICommand)GetValue(ConfirmCommandProperty); }
            set { SetValue(ConfirmCommandProperty, value); }
        }

        #endregion

        #region CancelCommand Dependency Property

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register("CancelCommand", typeof(ICommand), typeof(NewTemplateControl), new PropertyMetadata(null));

        public ICommand CancelCommand
        {
            get { return (ICommand)GetValue(CancelCommandProperty); }
            set { SetValue(CancelCommandProperty, value); }
        }

        #endregion

        public NewTemplateControl()
        {
            InitializeComponent();
            // 分别为两个 ItemsControl 设置数据源
            LanguageItemsControl.ItemsSource = _languageParts;
            CategoryItemsControl.ItemsSource = _categoryParts;
        }

        public void EmptyData()
        {
            // 清空所有数据
            _languageParts.Clear();
            _categoryParts.Clear();
            FilePathTextBox.Text = string.Empty;
            PlotTypeComboBox.SelectedIndex = -1;
        }

        /// <summary>
        /// 添加语言标签
        /// </summary>
        private void NewLanguageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string text = NewLanguageTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    _languageParts.Add(text);
                    NewLanguageTextBox.Clear();
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// 删除语言标签
        /// </summary>
        private void RemoveLanguageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string languageToRemove)
            {
                _languageParts.Remove(languageToRemove);
            }
        }

        /// <summary>
        /// 添加分类层级标签
        /// </summary>
        private void NewCategoryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string text = NewCategoryTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    _categoryParts.Add(text);
                    NewCategoryTextBox.Clear();
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// 删除分类层级标签
        /// </summary>
        private void RemoveCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string categoryToRemove)
            {
                _categoryParts.Remove(categoryToRemove);
            }
        }

        /// <summary>
        /// 点击“浏览”按钮。
        /// </summary>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string? selectedPath = FileHelper.GetFolderPath();
            if (!string.IsNullOrEmpty(selectedPath))
            {
                FilePathTextBox.Text = selectedPath;
            }
        }

        /// <summary>
        /// 点击“确定”按钮
        /// </summary>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmCommand?.CanExecute(this) ?? false)
            {
                ConfirmCommand.Execute(this);
            }
        }

        /// <summary>
        /// 点击“取消”按钮
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (CancelCommand?.CanExecute(this) ?? false)
            {
                CancelCommand.Execute(this);
            }
        }
    }
}