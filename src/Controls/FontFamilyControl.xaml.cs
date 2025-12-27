using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GeoChemistryNexus.Controls
{
    public class FontItem
    {
        public string Name { get; set; }
        public string Category { get; set; }

        // 缓存 FontFamily 对象，避免绑定过程中因 TypeConverter 带来的开销
        private System.Windows.Media.FontFamily _fontFamily;
        public System.Windows.Media.FontFamily FontFamily
        {
            get
            {
                if (_fontFamily == null)
                {
                    _fontFamily = new System.Windows.Media.FontFamily(Name);
                }
                return _fontFamily;
            }
        }

        public override string ToString() => Name;
    }

    public partial class FontFamilyControl : UserControl
    {
        private bool _isUpdatingFromSource = false;
        
        // Static list to persist recent fonts
        private static List<string> _recentFonts = new List<string>();
        // Cache for all font items to avoid recreating them
        private static List<FontItem> _cachedAllFontItems = null;
        private const int MaxRecentFonts = 5;

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
            
            // Initial load
            LoadFonts();

            // Subscribe to Loaded to set initial selection
            this.Loaded += FontFamilyControl_Loaded;
        }

        private async void LoadFonts()
        {
            // Use cached items if available
            if (_cachedAllFontItems == null)
            {
                var fontNames = await FontService.GetFontNamesAsync();
                _cachedAllFontItems = fontNames.Select(font => new FontItem 
                { 
                    Name = font, 
                    Category = LanguageService.Instance["all_fonts"]        // 所有字体
                }).ToList();
            }

            UpdateFontList();
        }

        private void UpdateFontList()
        {
            if (_cachedAllFontItems == null) return;

            var items = new List<FontItem>();

            // Add Recent Fonts
            if (_recentFonts.Any())
            {
                foreach (var font in _recentFonts)
                {
                    items.Add(new FontItem 
                    { 
                        Name = font, 
                        Category = LanguageService.Instance["recently_used"]        // 最近使用
                    });
                }
            }

            items.AddRange(_cachedAllFontItems);

            FontComboBox.ItemsSource = items;

            // Setup Grouping
            var view = CollectionViewSource.GetDefaultView(FontComboBox.ItemsSource);
            if (view != null)
            {
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            }
        }

        private void FontFamilyControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetSelectedFont(this.FontFamilyName);
            this.Loaded -= FontFamilyControl_Loaded;
        }

        private static void OnFontFamilyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FontFamilyControl)d;
            if (!control.IsLoaded) return;

            var newFontName = e.NewValue as string;
            control.SetSelectedFont(newFontName);
        }

        private void SetSelectedFont(string fontName)
        {
            if (string.IsNullOrEmpty(fontName)) return;

            _isUpdatingFromSource = true;
            
            // Find the item in the ComboBox
            var items = FontComboBox.ItemsSource as IEnumerable<FontItem>;
            if (items != null)
            {
                // Try to find exact match
                var match = items.FirstOrDefault(x => x.Name.Equals(fontName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    FontComboBox.SelectedItem = match;
                }
                else
                {
                    FontComboBox.Text = fontName;
                }
            }

            _isUpdatingFromSource = false;
        }

        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingFromSource) return;

            if (FontComboBox.SelectedItem is FontItem selectedItem)
            {
                // Add to recent
                AddToRecent(selectedItem.Name);
                
                // Update Property
                FontFamilyName = selectedItem.Name;
            }
        }

        // 处理“最佳匹配”或手动输入的文本
        private void FontComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // 如果用户输入的内容不在列表里，或者刚刚输完
            if (FontComboBox.SelectedItem == null && !string.IsNullOrWhiteSpace(FontComboBox.Text))
             {
                 var text = FontComboBox.Text;
                 var items = FontComboBox.ItemsSource as IEnumerable<FontItem>;
                 if (items != null)
                 {
                     var match = items.FirstOrDefault(x => x.Name.Equals(text, StringComparison.OrdinalIgnoreCase));
                     if (match != null)
                     {
                         FontComboBox.SelectedItem = match;
                         AddToRecent(match.Name);
                         FontFamilyName = match.Name;
                     }
                     else
                     {
                        // 以输入开头的精确匹配
                        var partialMatch = items.FirstOrDefault(x => x.Name.StartsWith(text, StringComparison.OrdinalIgnoreCase));
                         if (partialMatch != null)
                         {
                             FontComboBox.SelectedItem = partialMatch;
                             AddToRecent(partialMatch.Name);
                             FontFamilyName = partialMatch.Name;
                         }
                     }
                 }
             }
        }

        private void AddToRecent(string fontName)
        {
            if (string.IsNullOrEmpty(fontName)) return;

            // Remove if exists (to move to top)
            if (_recentFonts.Contains(fontName))
            {
                _recentFonts.Remove(fontName);
            }

            _recentFonts.Insert(0, fontName);

            if (_recentFonts.Count > MaxRecentFonts)
            {
                _recentFonts.RemoveAt(_recentFonts.Count - 1);
            }

            _ = Dispatcher.InvokeAsync(() => 
            {
                UpdateFontList();
                // Restore selection
                SetSelectedFont(fontName);
            });
         }
    }
}