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
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Models;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using System.IO;

namespace GeoChemistryNexus.Controls
{
    public class CategoryPartModel
    {
        public string DisplayName { get; set; }
        public Dictionary<string, string> LocalizedNames { get; set; } // Null if manual input
    }

    public class LanguageTagModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private bool _isDefault;
        public string Text { get; set; }
        public bool IsDefault 
        { 
            get => _isDefault;
            set => SetProperty(ref _isDefault, value);
        }
    }

    /// <summary>
    /// NewTemplateControl.xaml 的交互逻辑
    /// </summary>
    public partial class NewTemplateControl : UserControl
    {
        // 为语言和分类层级分别创建集合
        private readonly ObservableCollection<LanguageTagModel> _languageParts = new ObservableCollection<LanguageTagModel>();
        // Change from string to CategoryPartModel
        private readonly ObservableCollection<CategoryPartModel> _categoryParts = new ObservableCollection<CategoryPartModel>();
        
        // 存储加载的分类数据
        private PlotTemplateCategoryConfig _categoryConfig;
        
        // 修改 Language 属性以从标签集合生成字符串
        public string SelectedLanguages => string.Join(" > ", _languageParts.Select(x => x.Text));
        
        // Use DisplayName for the string representation
        public string CategoryHierarchy => string.Join(" > ", _categoryParts.Select(p => p.DisplayName));

        public string PlotType => (PlotTypeComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? PlotTypeComboBox.Text;

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
            
            // 监听分类集合变化，更新下拉框
            _categoryParts.CollectionChanged += (s, e) => UpdateCategoryComboBoxSource();
            
            InitializeBuiltInLanguages();
            LoadCategories();

            // 注册消息接收
            WeakReferenceMessenger.Default.Register<CategoryConfigUpdatedMessage>(this, (r, m) =>
            {
                // 在 UI 线程上重新加载配置
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadCategories();
                });
            });

            // 默认选中第一个绘图类型
            PlotTypeComboBox.SelectedIndex = 0;
        }

        private void LoadCategories()
        {
            _categoryConfig = PlotCategoryHelper.LoadConfig();
            UpdateCategoryComboBoxSource();
        }
        
        // Expose method to get the rich category parts
        public IEnumerable<CategoryPartModel> GetCategoryParts()
        {
            return _categoryParts;
        }

        private void UpdateCategoryComboBoxSource()
        {
            if (_categoryConfig == null) return;
            
            List<Dictionary<string, string>> sourceList = null;
            
            // 动态确定当前是第几级
            int currentLevelIndex = _categoryParts.Count + 1; // 0个已选 -> Level 1, 1个已选 -> Level 2
            string levelKey = $"Level{currentLevelIndex}";

            // 尝试从字典中获取对应的层级列表
            if (_categoryConfig.ContainsKey(levelKey))
            {
                sourceList = _categoryConfig[levelKey];
            }
            
            if (sourceList != null)
            {
                var displayItems = sourceList.Select(c => new CategoryDisplayItem 
                { 
                    DisplayName = PlotCategoryHelper.GetName(c),
                    OriginalObject = c 
                }).ToList();
                CategoryInputComboBox.ItemsSource = displayItems;
            }
            else
            {
                CategoryInputComboBox.ItemsSource = null;
            }
        }


        public void EmptyData()
        {
            // 清空所有数据
            _languageParts.Clear();
            _categoryParts.Clear();
            
            CategoryInputComboBox.SelectedIndex = -1;
            CategoryInputComboBox.Text = string.Empty;

            LanguageInputComboBox.SelectedIndex = -1;
            LanguageInputComboBox.Text = string.Empty;

            PlotTypeComboBox.SelectedIndex = 0;
        }

        private void UpdateLanguageDefaultStatus()
        {
            if (_languageParts.Count == 0) return;
            
            // First item is default, others are not
            for (int i = 0; i < _languageParts.Count; i++)
            {
                _languageParts[i].IsDefault = (i == 0);
            }
        }

        /// <summary>
        /// 添加语言标签 (Selection or Enter)
        /// </summary>
        private void LanguageInputComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var comboBox = sender as ComboBox;
                string text = comboBox?.Text?.Trim();
                
                if (!string.IsNullOrEmpty(text))
                {
                    // Check if it's already added
                    if (!_languageParts.Any(p => p.Text.Equals(text, StringComparison.OrdinalIgnoreCase)))
                    {
                        _languageParts.Add(new LanguageTagModel { Text = text });
                        UpdateLanguageDefaultStatus();
                    }
                    comboBox.Text = string.Empty;
                }
                e.Handled = true;
            }
        }

        private void LanguageInputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var selectedCode = comboBox?.SelectedValue as string;
            
            if (!string.IsNullOrWhiteSpace(selectedCode))
            {
                if (!_languageParts.Any(p => p.Text.Equals(selectedCode, StringComparison.OrdinalIgnoreCase)))
                {
                    _languageParts.Add(new LanguageTagModel { Text = selectedCode });
                    UpdateLanguageDefaultStatus();
                }
                
                // Clear selection and text
                comboBox.SelectedIndex = -1;
                comboBox.Text = string.Empty;
            }
        }

        /// <summary>
        /// 删除语言标签
        /// </summary>
        private void RemoveLanguageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is LanguageTagModel itemToRemove)
            {
                _languageParts.Remove(itemToRemove);
                UpdateLanguageDefaultStatus();
            }
        }
        
        /// <summary>
        /// 添加分类标签 (Selection or Enter)
        /// </summary>
        private void CategoryInputComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var comboBox = sender as ComboBox;
                string text = comboBox?.Text?.Trim();

                if (!string.IsNullOrEmpty(text))
                {
                    // Check for invalid path chars
                    var invalidChars = System.IO.Path.GetInvalidFileNameChars();
                    if (text.IndexOfAny(invalidChars) >= 0)
                    {
                        HandyControl.Controls.MessageBox.Show(
                            LanguageService.Instance["invalid_filename_char"], 
                            LanguageService.Instance["error"], 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                        e.Handled = true;
                        return;
                    }

                    // Manual input (no localized object)
                    _categoryParts.Add(new CategoryPartModel 
                    { 
                        DisplayName = text,
                        LocalizedNames = null
                    });
                    
                    comboBox.Text = string.Empty;
                }
                e.Handled = true;
            }
        }
        
        private void CategoryInputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            
            if (comboBox?.SelectedItem is CategoryDisplayItem selectedItem)
            {
                 string name = selectedItem.DisplayName;
                 if (!string.IsNullOrWhiteSpace(name))
                 {
                     // Add CategoryPartModel with localized data
                     _categoryParts.Add(new CategoryPartModel 
                     { 
                         DisplayName = name,
                         LocalizedNames = selectedItem.OriginalObject as Dictionary<string, string>
                     });
                 }
                 // 清空选择
                 comboBox.SelectedIndex = -1;
                 comboBox.Text = string.Empty;
            }
        }

        /// <summary>
        /// 删除分类标签
        /// </summary>
        private void RemoveCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Update to handle CategoryPartModel
            if (sender is Button button && button.Tag is CategoryPartModel categoryToRemove)
            {
                _categoryParts.Remove(categoryToRemove);
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

        private class LanguageOption
        {
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }
        
        private class CategoryDisplayItem
        {
            public string DisplayName { get; set; }
            public object OriginalObject { get; set; }
        }

        private void InitializeBuiltInLanguages()
        {
            var builtIns = new List<LanguageOption>
            {
                new LanguageOption { Name = "简体中文 (zh-CN)", Code = "zh-CN" },
                new LanguageOption { Name = "繁体中文 (zh-TW)", Code = "zh-TW" },
                new LanguageOption { Name = "美式英文 (en-US)", Code = "en-US" },
                new LanguageOption { Name = "日语 (ja-JP)", Code = "ja-JP" },
                new LanguageOption { Name = "俄语 (ru-RU)", Code = "ru-RU" },
                new LanguageOption { Name = "韩语 (ko-KR)", Code = "ko-KR" },
                new LanguageOption { Name = "德语 (de-DE)", Code = "de-DE" },
                new LanguageOption { Name = "西班牙语 (es-ES)", Code = "es-ES" }
            };

            LanguageInputComboBox.ItemsSource = builtIns;
        }

    }
}
