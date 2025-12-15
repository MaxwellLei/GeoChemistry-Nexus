using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.ViewModels;
using GeoChemistryNexus.Controls;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

using System.Windows.Media.Animation;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// MainPlot.xaml 的交互逻辑
    /// </summary>
    public partial class MainPlotPage : Page
    {
        // 单例本体
        private static MainPlotPage homePage = null;

        private MainPlotViewModel viewModel;

        public MainPlotPage()
        {
            InitializeComponent();
            // 链接 ViewModel
            viewModel = new MainPlotViewModel(this.WpfPlot1, this.Drichtextbox, this.DataGrid);
            this.DataContext = viewModel;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            var dpd = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(CustomColorPicker.SelectedColorProperty, typeof(CustomColorPicker));
            dpd.AddValueChanged(RichTextColorPicker, (s, e) =>
            {
                if (Drichtextbox == null || Drichtextbox.IsReadOnly) return;
                var color = RichTextColorPicker.SelectedColor;

                // 更新按钮下划线颜色
                if (ColorUnderline != null)
                {
                    ColorUnderline.Background = new SolidColorBrush(color);
                }

                if (!Drichtextbox.Selection.IsEmpty)
                {
                    Drichtextbox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
                }
            });
        }

        // 记录切换前的宽度状态，用于恢复
        private GridLength _lastOuterRightWidth = new GridLength(300);
        private GridLength _lastInnerLeftWidth = new GridLength(0.3, GridUnitType.Star);

        private async void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainPlotViewModel.RibbonTabIndex))
            {
                if (viewModel.RibbonTabIndex == 1)
                {
                    // 切换到数据表格模式
                    
                    // 1. 保存当前状态 (如果当前有宽度)
                    if (OuterRightCol.Width.Value > 0)
                    {
                        _lastOuterRightWidth = OuterRightCol.Width;
                    }
                    if (InnerLeftCol.Width.Value > 0)
                    {
                        _lastInnerLeftWidth = InnerLeftCol.Width;
                    }

                    // 2. 准备动画目标
                    // 保持单位类型一致以确保动画平滑 (Star -> 0*, Pixel -> 0px)
                    var targetRight = OuterRightCol.Width.IsStar 
                        ? new GridLength(0, GridUnitType.Star) 
                        : new GridLength(0);
                    
                    // 左侧目标设为较大的比例 (0.923*)
                    // 如果当前是 Pixel，这里可能会有跳变，但通常左侧较少被手动调整为 Pixel
                    var targetLeft = new GridLength(0.923077, GridUnitType.Star);

                    // 3. 解除最小宽度限制以便折叠
                    OuterRightCol.MinWidth = 0;

                    // 4. 执行动画
                    AnimateColumn(OuterRightCol, targetRight);
                    AnimateColumn(InnerLeftCol, targetLeft);
                }
                else
                {
                    // 切换回绘图模式
                    
                    // 1. 恢复宽度
                    AnimateColumn(OuterRightCol, _lastOuterRightWidth);
                    AnimateColumn(InnerLeftCol, _lastInnerLeftWidth);

                    // 2. 等待动画完成后恢复最小宽度限制
                    // 动画时长为 0.5s，这里等待 0.5s
                    await Task.Delay(500);
                    OuterRightCol.MinWidth = 280;
                }
            }
        }

        private void AnimateColumn(ColumnDefinition column, GridLength toValue)
        {
            var animation = new GridLengthAnimation
            {
                From = column.Width, // 从当前宽度开始
                To = toValue,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            column.BeginAnimation(ColumnDefinition.WidthProperty, animation);
        }

        public static MainPlotPage GetPage()
        {
            if (homePage == null)
            {
                homePage = new MainPlotPage();
                //homePage.lg = ConfigHelper.GetConfig("language");
            }
            homePage.viewModel.InitTemplate();
            homePage.viewModel.LoadSettings();
            return homePage;
        }

        // 点击展开列表
        private void OnTreeViewItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 校验事件的发送者是否就是事件的原始来源所在的TreeViewItem。阻止冒泡事件
            if (sender is TreeViewItem item)
            {
                // 从真正被点击的元素向上查找父类
                var sourceTvi = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject);

                // 冒泡事件
                if (item != sourceTvi)
                {
                    return;
                }
            }

            // 检查父类状态
            var originalSource = e.OriginalSource as DependencyObject;
            if (FindAncestor<ToggleButton>(originalSource) != null)
            {
                return;
            }

            // 执行展开/折叠
            if (sender is TreeViewItem tvi && tvi.HasItems)
            {
                tvi.IsExpanded = !tvi.IsExpanded;
            }

            // 停止事件冒泡
            e.Handled = true;
        }

        private void OnTreeViewItemPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is LayerItemViewModel layerItem)
            {
                var originalSource = e.OriginalSource as DependencyObject;

                // 1. 确保点击的是当前 TreeViewItem，而不是其子项
                // FindAncestor 会从点击点向上找，找到最近的一个 TreeViewItem
                var clickedItem = FindAncestor<TreeViewItem>(originalSource);
                if (clickedItem != item)
                {
                    // 如果找到的最近 TreeViewItem 不是当前的 sender，说明点击的是 sender 的子项
                    // 此时应该忽略，让事件继续传递给子项去处理
                    return;
                }

                // 2. 如果点击的是折叠/展开按钮，不处理选择
                if (FindAncestor<ToggleButton>(originalSource) != null)
                {
                    return;
                }

                // 执行 ViewModel 的选择逻辑
                if (viewModel.SelectLayerCommand.CanExecute(layerItem))
                {
                    viewModel.SelectLayerCommand.Execute(layerItem);
                }

                // 阻止 TreeView 原生的单选逻辑
                e.Handled = true;
                
                // 尝试让 Item 获得焦点以支持键盘操作（可选）
                item.Focus();
            }
        }

        /// <summary>
        /// 辅助方法，用于在可视化树中向上查找指定类型的父控件。
        /// </summary>
        /// <typeparam name="T">要查找的父控件的类型。</typeparam>
        /// <param name="current">查找的起始依赖对象。</param>
        /// <returns>找到的第一个指定类型的父控件，如果找不到则返回 null。</returns>
        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        #region RichTextBox Toolbar Events

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Drichtextbox == null || Drichtextbox.IsReadOnly) return;
            if (!Drichtextbox.Selection.IsEmpty && sender is System.Windows.Controls.ComboBox comboBox && comboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                if (double.TryParse(item.Content.ToString(), out double size))
                {
                    Drichtextbox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
                }
            }
        }

        private void FontSizeComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ApplyFontSize(sender);
                // 移除焦点，让用户可以继续编辑文档
                Drichtextbox.Focus();
                e.Handled = true;
            }
        }

        private void FontSizeComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyFontSize(sender);
        }

        private void ApplyFontSize(object sender)
        {
            if (Drichtextbox == null || Drichtextbox.IsReadOnly) return;
            if (sender is System.Windows.Controls.ComboBox comboBox)
            {
                if (double.TryParse(comboBox.Text, out double size))
                {
                    if (!Drichtextbox.Selection.IsEmpty)
                    {
                        Drichtextbox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
                    }
                }
            }
        }


        private void SubscriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (Drichtextbox == null || Drichtextbox.IsReadOnly) return;
            ToggleBaselineAlignment(BaselineAlignment.Subscript);
        }

        private void SuperscriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (Drichtextbox == null || Drichtextbox.IsReadOnly) return;
            ToggleBaselineAlignment(BaselineAlignment.Superscript);
        }

        private void ToggleBaselineAlignment(BaselineAlignment alignment)
        {
            if (Drichtextbox == null || Drichtextbox.Selection.IsEmpty) return;

            var currentAlignment = Drichtextbox.Selection.GetPropertyValue(Inline.BaselineAlignmentProperty);

            // 如果已经是这个状态，就取消（恢复到 Baseline）
            if (currentAlignment != DependencyProperty.UnsetValue && currentAlignment is BaselineAlignment ba && ba == alignment)
            {
                Drichtextbox.Selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Baseline);
            }
            else
            {
                Drichtextbox.Selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, alignment);
            }
            
            UpdateToolbarState();
        }

        private void Drichtextbox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateToolbarState();
        }

        private void UpdateToolbarState()
        {
            if (Drichtextbox == null) return;

            // Bold
            var weight = Drichtextbox.Selection.GetPropertyValue(TextElement.FontWeightProperty);
            if (BoldButton != null)
                BoldButton.IsChecked = (weight != DependencyProperty.UnsetValue) && (weight.Equals(FontWeights.Bold));

            // Italic
            var style = Drichtextbox.Selection.GetPropertyValue(TextElement.FontStyleProperty);
            if (ItalicButton != null)
                ItalicButton.IsChecked = (style != DependencyProperty.UnsetValue) && (style.Equals(FontStyles.Italic));

            // Underline
            var decoration = Drichtextbox.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            if (UnderlineButton != null)
                UnderlineButton.IsChecked = (decoration != DependencyProperty.UnsetValue) && (decoration == TextDecorations.Underline);

            // Sub/Superscript
            var alignment = Drichtextbox.Selection.GetPropertyValue(Inline.BaselineAlignmentProperty);
            if (SuperscriptButton != null)
                SuperscriptButton.IsChecked = (alignment != DependencyProperty.UnsetValue) && ((BaselineAlignment)alignment == BaselineAlignment.Superscript);
            if (SubscriptButton != null)
                SubscriptButton.IsChecked = (alignment != DependencyProperty.UnsetValue) && ((BaselineAlignment)alignment == BaselineAlignment.Subscript);
        }

        #endregion
    }
}
