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
using GeoChemistryNexus.Services;
using System.Collections.Specialized;

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
            AttachBreadcrumbScrollBehavior();

            // 页面加载时检查更新
            this.Loaded += (s, e) => viewModel.CheckUpdatesIfNeeded();

            /*
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
            */
        }

        private void OnSwitchRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            UpdateSwitchSlider();
        }

        private void OnSwitchRadioButtonLoaded(object sender, RoutedEventArgs e)
        {
            // Use Dispatcher to ensure layout is ready
            Dispatcher.BeginInvoke(new Action(() => UpdateSwitchSlider()), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void OnSwitchContainerIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool isVisible && isVisible)
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateSwitchSlider()), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }

        private void UpdateSwitchSlider()
        {
            if (SwitchContainer == null || SwitchSlider == null) return;

            RadioButton checkedButton = null;
            foreach (var child in SwitchContainer.Children)
            {
                if (child is RadioButton rb && rb.IsChecked == true)
                {
                    checkedButton = rb;
                    break;
                }
            }

            if (checkedButton != null)
            {
                try
                {
                    // Calculate position relative to the container
                    var transform = checkedButton.TransformToAncestor(SwitchContainer);
                    var point = transform.Transform(new Point(0, 0));

                    var targetMargin = new Thickness(point.X, 0, 0, 0);
                    var targetWidth = checkedButton.ActualWidth;
                    
                    if (targetWidth <= 0) return;

                    // Animate Margin
                    var marginAnim = new ThicknessAnimation(targetMargin, TimeSpan.FromMilliseconds(250));
                    marginAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                    SwitchSlider.BeginAnimation(Border.MarginProperty, marginAnim);

                    // Animate Width
                    var widthAnim = new DoubleAnimation(targetWidth, TimeSpan.FromMilliseconds(250));
                    widthAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                    SwitchSlider.BeginAnimation(Border.WidthProperty, widthAnim);
                }
                catch (Exception)
                {
                    // Ignore visual tree issues
                }
            }
        }

        // 记录切换前的宽度状态，用于恢复
        private GridLength _lastOuterRightWidth = new GridLength(320);
        private GridLength _lastInnerLeftWidth = new GridLength(280);
        
        // 标记当前是否处于折叠模式（数据表格模式）
        private bool _isCollapsedMode = false;

        // 标记当前是否处于稳定展开状态（非动画中且未折叠）
        private bool _isStableExpanded = true;

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainPlotViewModel.Breadcrumbs))
            {
                AttachBreadcrumbScrollBehavior();
            }

            if (e.PropertyName == nameof(MainPlotViewModel.RibbonTabIndex))
            {
                if (viewModel.RibbonTabIndex == 1)
                {
                    // 切换到数据表格模式
                    
                    // 使用 Dispatcher 确保布局计算准确
                    Dispatcher.BeginInvoke(new Action(() => 
                    {
                        // 1. 仅在稳定展开状态下保存当前状态，避免在动画过程中保存中间值导致状态损坏
                        if (_isStableExpanded)
                        {
                            if (OuterRightCol.Width.GridUnitType == GridUnitType.Pixel)
                            {
                                _lastOuterRightWidth = OuterRightCol.Width;
                            }
                            if (InnerLeftCol.Width.GridUnitType == GridUnitType.Pixel)
                            {
                                _lastInnerLeftWidth = InnerLeftCol.Width;
                            }
                        }

                        // 标记不再稳定展开
                        _isStableExpanded = false;

                        // 2. 解除限制以便动画
                        OuterRightCol.MinWidth = 0;
                        InnerLeftCol.MaxWidth = double.PositiveInfinity;

                        // 3. 计算目标像素宽度 (4.8 / 10 的比例)
                        double gridWidth = MainLayoutGrid.ActualWidth;

                        // 检查 Grid 是否已加载且有宽度
                        if (gridWidth <= 0)
                        {
                             if (PlotColumn != null) PlotColumn.Width = new GridLength(1, GridUnitType.Star);
                             InnerLeftCol.Width = new GridLength(0.923077, GridUnitType.Star);
                             OuterRightCol.Width = new GridLength(0);
                             _isCollapsedMode = true;
                             return;
                        }

                        double splitterWidth = 8; // 两个Splitter各4px
                        double availableWidth = Math.Max(0, gridWidth - splitterWidth);
                        double targetLeftPx = availableWidth * 0.48;

                        // 4. 创建像素到像素的动画
                        // 左侧：当前像素 -> 目标像素
                        var animLeft = new GridLengthAnimation
                        {
                            From = new GridLength(InnerLeftCol.ActualWidth, GridUnitType.Pixel),
                            To = new GridLength(targetLeftPx, GridUnitType.Pixel),
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                        };

                        // 右侧：当前像素 -> 0像素
                        var animRight = new GridLengthAnimation
                        {
                            From = new GridLength(OuterRightCol.ActualWidth, GridUnitType.Pixel),
                            To = new GridLength(0, GridUnitType.Pixel),
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                        };

                        // 5. 动画完成后切换到 Star 单位以保持响应式布局
                        animLeft.Completed += (s, _) =>
                        {
                            // 清除动画绑定，使本地值生效
                            InnerLeftCol.BeginAnimation(ColumnDefinition.WidthProperty, null);
                            
                            // 修复：重置中间列宽度为 1*，确保比例正确
                            if (PlotColumn != null)
                            {
                                PlotColumn.Width = new GridLength(1, GridUnitType.Star);
                            }

                            // 设置为 Star 单位 (4.8/5.2 = 0.923077)
                            InnerLeftCol.Width = new GridLength(0.923077, GridUnitType.Star);
                        };

                        animRight.Completed += (s, _) =>
                        {
                            OuterRightCol.BeginAnimation(ColumnDefinition.WidthProperty, null);
                            OuterRightCol.Width = new GridLength(0);
                        };

                        // 6. 开始动画
                        InnerLeftCol.BeginAnimation(ColumnDefinition.WidthProperty, animLeft);
                        OuterRightCol.BeginAnimation(ColumnDefinition.WidthProperty, animRight);
                        
                        _isCollapsedMode = true;
                    }));
                }
                else
                {
                    // 切换回绘图模式或其他模式
                    if (_isCollapsedMode)
                    {
                        // 1. 创建像素到像素的动画 (从当前状态恢复到之前的固定宽度)
                        
                        // 左侧：当前实际像素 -> 之前的固定像素
                        var animLeft = new GridLengthAnimation
                        {
                            From = new GridLength(InnerLeftCol.ActualWidth, GridUnitType.Pixel),
                            To = _lastInnerLeftWidth,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                        };

                        // 右侧：当前实际像素(0) -> 之前的固定像素
                        var animRight = new GridLengthAnimation
                        {
                            From = new GridLength(OuterRightCol.ActualWidth, GridUnitType.Pixel),
                            To = _lastOuterRightWidth,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                        };

                        // 2. 动画完成后恢复约束
                        animLeft.Completed += (s, _) =>
                        {
                            InnerLeftCol.BeginAnimation(ColumnDefinition.WidthProperty, null);
                            InnerLeftCol.Width = _lastInnerLeftWidth;
                            InnerLeftCol.MaxWidth = 500;
                        };

                        animRight.Completed += (s, _) =>
                        {
                            OuterRightCol.BeginAnimation(ColumnDefinition.WidthProperty, null);
                            OuterRightCol.Width = _lastOuterRightWidth;
                            OuterRightCol.MinWidth = 250;
                            
                            // 标记为稳定展开
                            _isStableExpanded = true;
                        };

                        // 3. 开始动画
                        InnerLeftCol.BeginAnimation(ColumnDefinition.WidthProperty, animLeft);
                        OuterRightCol.BeginAnimation(ColumnDefinition.WidthProperty, animRight);
                        
                        _isCollapsedMode = false;
                    }
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
            }
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
                var clickedItem = FindAncestor<TreeViewItem>(originalSource);
                if (clickedItem != item)
                {
                    // 忽略
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
                
                // 尝试让 Item 获得焦点以支持键盘操作
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
            
            // Font Size
            var size = Drichtextbox.Selection.GetPropertyValue(TextElement.FontSizeProperty);
            if (FontSizeComboBox != null)
            {
                if (size != DependencyProperty.UnsetValue && size is double dSize)
                {
                    FontSizeComboBox.Text = dSize.ToString("0.#");
                }
                else
                {
                    FontSizeComboBox.Text = "";
                }
            }
        }

        #endregion

        private Window _popOutWindow = null;

        private void OnPopOutPlotClick(object sender, RoutedEventArgs e)
        {
            // 如果窗口已经存在，则激活它
            if (_popOutWindow != null)
            {
                _popOutWindow.Activate();
                return;
            }

            // 1. 从当前父容器移除 WpfPlot1
            if (PlotContainer.Children.Contains(WpfPlot1))
            {
                PlotContainer.Children.Remove(WpfPlot1);
            }

            // 2. 创建新窗口
            _popOutWindow = new Window
            {
                Title = LanguageService.Instance["independent_diagram_window"],     // 独立图解窗口
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            // 尝试设置 Owner
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null && mainWindow.IsVisible)
            {
                _popOutWindow.Owner = mainWindow;
            }

            // 3. 将 WpfPlot1 放入新窗口
            // 为了保持样式，最好包一层 Grid
            var grid = new Grid();
            grid.Children.Add(WpfPlot1);
            _popOutWindow.Content = grid;

            // 4. 处理关闭事件
            _popOutWindow.Closed += (s, args) =>
            {
                // 移除 Content 防止引用
                if (_popOutWindow.Content is Grid g)
                {
                    g.Children.Remove(WpfPlot1);
                }
                _popOutWindow.Content = null;

                // 恢复到主界面
                if (!PlotContainer.Children.Contains(WpfPlot1))
                {
                    PlotContainer.Children.Insert(0, WpfPlot1);
                    Grid.SetRow(WpfPlot1, 0);
                }

                _popOutWindow = null;
            };

            _popOutWindow.Show();
        }

        private void AttachBreadcrumbScrollBehavior()
        {
             if (viewModel.Breadcrumbs is INotifyCollectionChanged collection)
             {
                 collection.CollectionChanged -= Breadcrumbs_CollectionChanged;
                 collection.CollectionChanged += Breadcrumbs_CollectionChanged;
             }
             ScrollBreadcrumbToEnd();
        }

        private void Breadcrumbs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ScrollBreadcrumbToEnd();
        }

        private void BreadcrumbScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScrollBreadcrumbToEnd();
        }

        private void ScrollBreadcrumbToEnd()
        {
            Dispatcher.InvokeAsync(() => {
                BreadcrumbScrollViewer?.ScrollToRightEnd();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}
