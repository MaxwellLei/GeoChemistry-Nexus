using GeoChemistryNexus.Models;
using GeoChemistryNexus.ViewModels;
using GeoChemistryNexus.Controls;
using GeoChemistryNexus.Helpers;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using WpfToolkit.Controls;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// MainPlot.xaml 的交互逻辑
    /// </summary>
    public partial class MainPlotPage : Page
    {
        // 单例本体
        private static MainPlotPage homePage = null!;

        private MainPlotViewModel viewModel;
        private bool _isUpdatingToolbarState = false;

        public MainPlotPage()
        {
            InitializeComponent();
            // 链接 ViewModel
            viewModel = new MainPlotViewModel(this.WpfPlot1, this.Drichtextbox, this.DataGrid);
            this.DataContext = viewModel;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.GetTemplateCardsScrollOffset = GetTemplateCardsScrollOffset;
            viewModel.RestoreTemplateCardsScrollAsync = RestoreTemplateCardsScrollAsync;
            viewModel.TemplateCardsPresentationChanged += OnTemplateCardsPresentationChanged;
            AttachTemplateCardsScrollBehavior();
            AttachBreadcrumbScrollBehavior();
            AttachTemplateCardLayoutBehavior();

            this.Loaded += (s, e) =>
            {
                // 页面加载后，获取窗口并添加键盘事件监听
                AttachKeyboardEvents();
                ScheduleTemplateCardItemSizeUpdate();
            };

            this.Unloaded += (s, e) =>
            {
                DetachKeyboardEvents();
                _stickyScrollCts?.Cancel();
                _stickyScrollCts?.Dispose();
                _stickyScrollCts = null;
                _hideTemplateCardsUntilScrollRestored = false;
                SetTemplateCardsVisibilityForScrollRestore(visible: true);
                CompleteScrollRestore();
                if (viewModel != null)
                    viewModel.TemplateCardsPresentationChanged -= OnTemplateCardsPresentationChanged;
            };

            var dpd = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(CustomColorPicker.SelectedColorProperty, typeof(CustomColorPicker));
            dpd.AddValueChanged(RichTextColorPicker, (s, e) =>
            {
                // Skip if we're just updating toolbar state to reflect current selection
                if (_isUpdatingToolbarState) return;
                
                if (Drichtextbox == null || Drichtextbox.IsReadOnly) return;
                var color = RichTextColorPicker.SelectedColor;

                Drichtextbox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            });
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

            RadioButton? checkedButton = null;
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

        // 绘图模式列宽比例：左 20% + 绘图 56% + 右 24% = 100%
        private const double PlotModeLeftStar = 20;
        private const double PlotModePlotStar = 56;
        private const double PlotModeRightStar = 24;

        // 记录切换前的宽度状态，用于恢复
        private GridLength _lastInnerLeftWidth = new GridLength(PlotModeLeftStar, GridUnitType.Star);
        private GridLength _lastPlotWidth = new GridLength(PlotModePlotStar, GridUnitType.Star);
        private GridLength _lastOuterRightWidth = new GridLength(PlotModeRightStar, GridUnitType.Star);
        
        // 标记当前是否处于折叠模式（数据表格模式）
        private bool _isCollapsedMode = false;

        // 标记当前是否处于稳定展开状态（非动画中且未折叠）
        private bool _isStableExpanded = true;

        // 标记动画是否正在进行中
        private bool _isAnimating = false;

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainPlotViewModel.Breadcrumbs))
            {
                AttachBreadcrumbScrollBehavior();
            }

            if (e.PropertyName == nameof(MainPlotViewModel.TemplateCardSizePreset))
            {
                ScheduleTemplateCardItemSizeUpdate(preserveScrollRatio: true);
            }

            if (e.PropertyName == nameof(MainPlotViewModel.GridScrollResetToken))
            {
                ResetDataGridScroll();
            }

            if (e.PropertyName == nameof(MainPlotViewModel.RibbonTabIndex))
            {
                if (viewModel.RibbonTabIndex == 1)
                {
                    // 切换到数据表格模式：先动画再切布局
                    AnimateToDataMode();
                }
                else
                {
                    // 切换回绘图模式：先切布局再动画
                    if (_isCollapsedMode)
                    {
                        AnimateToPlotMode();
                    }
                }
            }
        }

        /// <summary>
        /// 将数据表格的横向/纵向滚动条复位到起始位置
        /// </summary>
        private void ResetDataGridScroll()
        {
            DataGrid?.ScrollCurrentWorksheet(0, 0);

            // 延迟再次复位，避免选区变更等后续操作重新触发滚动
            DataGrid?.Dispatcher.BeginInvoke(() =>
            {
                DataGrid?.ScrollCurrentWorksheet(0, 0);
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 切换到数据模式：立即展开左侧面板（让ReoGrid有空间），固定Plot和右侧列宽，
        /// 用 TranslateTransform 补偿视觉位移并动画回新位置。
        /// </summary>
        private void AnimateToDataMode()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // 如果正在动画中，取消当前动画
                if (_isAnimating)
                {
                    CancelTransformAnimations();
                }

                // 仅在稳定展开状态下保存当前列宽比例
                if (_isStableExpanded)
                {
                    _lastInnerLeftWidth = InnerLeftCol.Width;
                    _lastPlotWidth = PlotColumn.Width;
                    _lastOuterRightWidth = OuterRightCol.Width;
                }

                _isStableExpanded = false;
                _isAnimating = true;

                double gridWidth = MainLayoutGrid.ActualWidth;
                if (gridWidth <= 0)
                {
                    // Grid 尚未加载，直接 snap
                    SnapToDataModeLayout();
                    _isAnimating = false;
                    return;
                }

                // 记录当前实际尺寸
                double oldLeftWidth = InnerLeftCol.ActualWidth;
                double rightWidth = OuterRightCol.ActualWidth;

                // 计算目标宽度：左侧 45%，Plot 55%
                double splitterWidth = 8;
                double availableWidth = Math.Max(0, gridWidth - splitterWidth);
                double targetLeftPx = availableWidth * 0.45;
                double targetPlotPx = availableWidth * 0.55;

                // 立即展开左侧面板，Plot 直接设为目标宽度（避免动画结束时 snap 产生跳变）
                // 右侧列保持当前宽度（超出部分被 ClipToBounds 裁切）
                OuterRightCol.MinWidth = 0;
                InnerLeftCol.MaxWidth = double.PositiveInfinity;
                InnerLeftCol.Width = new GridLength(targetLeftPx, GridUnitType.Pixel);
                PlotColumn.Width = new GridLength(targetPlotPx, GridUnitType.Pixel);
                OuterRightCol.Width = new GridLength(rightWidth, GridUnitType.Pixel);

                // 计算位移量：左面板扩大导致右侧内容全部右移
                double shift = targetLeftPx - oldLeftWidth; // 正值

                // 用 TranslateTransform 补偿：从旧视觉位置(-shift) 动画到新位置(0)
                // 在第一帧，布局把元素推到了新位置，-shift 把它们拉回旧位置，视觉无跳变
                var duration = TimeSpan.FromSeconds(0.4);
                var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

                var plotAnim = new DoubleAnimation(-shift, 0, duration) { EasingFunction = easing };
                var rightAnim = new DoubleAnimation(-shift, 0, duration) { EasingFunction = easing };
                var splitterAnim = new DoubleAnimation(-shift, 0, duration) { EasingFunction = easing };

                // 动画完成后 snap 到 Star 单位（响应式布局）
                plotAnim.Completed += (s, _) =>
                {
                    CancelTransformAnimations();
                    SnapToDataModeLayout();
                    _isAnimating = false;
                };

                // 启动动画（动画期间无布局计算，纯 GPU 合成）
                PlotContainerTranslate.BeginAnimation(TranslateTransform.XProperty, plotAnim);
                RightPanelTranslate.BeginAnimation(TranslateTransform.XProperty, rightAnim);
                RightSplitterTranslate.BeginAnimation(TranslateTransform.XProperty, splitterAnim);

                _isCollapsedMode = true;
            }));
        }

        /// <summary>
        /// 切换回绘图模式：先 snap 布局，用已知目标值计算偏移，
        /// 再用 RenderTransform 从旧位置平滑动画到新位置。
        /// 不调用 UpdateLayout()，避免打断图层列表的绑定/模板加载。
        /// </summary>
        private void AnimateToPlotMode()
        {
            // 如果正在动画中，取消当前动画
            if (_isAnimating)
            {
                CancelTransformAnimations();
            }

            _isAnimating = true;

            // 记录当前状态（数据模式下的左侧宽度）
            double oldLeftWidth = InnerLeftCol.ActualWidth;

            double gridWidth = MainLayoutGrid.ActualWidth;
            double splitterWidth = 8;
            double availableWidth = Math.Max(0, gridWidth - splitterWidth);
            double totalStars = GetPlotModeTotalStars();
            double targetLeftWidth = availableWidth * (_lastInnerLeftWidth.Value / totalStars);
            double targetRightWidth = availableWidth * (_lastOuterRightWidth.Value / totalStars);

            // 立即 snap 到绘图模式布局
            InnerLeftCol.BeginAnimation(ColumnDefinition.WidthProperty, null);
            OuterRightCol.BeginAnimation(ColumnDefinition.WidthProperty, null);
            PlotColumn.BeginAnimation(ColumnDefinition.WidthProperty, null);

            InnerLeftCol.Width = _lastInnerLeftWidth;
            InnerLeftCol.MaxWidth = double.PositiveInfinity;
            PlotColumn.Width = _lastPlotWidth;
            OuterRightCol.Width = _lastOuterRightWidth;
            OuterRightCol.MinWidth = 250;

            // 计算补偿偏移：布局 snap 使内容左移，正向补偿使其视觉上仍在旧位置
            double shift = oldLeftWidth - targetLeftWidth; // 正值 = 内容原本偏右

            var duration = TimeSpan.FromSeconds(0.4);
            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            // PlotContainer: 从旧位置(shift)平滑滑到新位置(0)
            var plotAnim = new DoubleAnimation(shift, 0, duration) { EasingFunction = easing };
            // 右侧面板: 从屏幕外(shift + 面板宽度)滑入到位(0)
            var rightAnim = new DoubleAnimation(shift + targetRightWidth, 0, duration) { EasingFunction = easing };
            // 右侧 Splitter: 和右侧面板一起
            var splitterAnim = new DoubleAnimation(shift + targetRightWidth, 0, duration) { EasingFunction = easing };

            // 动画完成后清理
            plotAnim.Completed += (s, _) =>
            {
                CancelTransformAnimations();
                _isStableExpanded = true;
                _isAnimating = false;
            };

            // 启动动画
            PlotContainerTranslate.BeginAnimation(TranslateTransform.XProperty, plotAnim);
            RightPanelTranslate.BeginAnimation(TranslateTransform.XProperty, rightAnim);
            RightSplitterTranslate.BeginAnimation(TranslateTransform.XProperty, splitterAnim);

            _isCollapsedMode = false;
        }

        /// <summary>
        /// Snap 到数据模式的最终布局
        /// </summary>
        private void SnapToDataModeLayout()
        {
            OuterRightCol.MinWidth = 0;
            InnerLeftCol.MaxWidth = double.PositiveInfinity;

            PlotColumn.Width = new GridLength(1, GridUnitType.Star);
            InnerLeftCol.Width = new GridLength(0.818182, GridUnitType.Star);
            OuterRightCol.Width = new GridLength(0);
        }

        private double GetPlotModeTotalStars()
        {
            return _lastInnerLeftWidth.Value + _lastPlotWidth.Value + _lastOuterRightWidth.Value;
        }

        /// <summary>
        /// 取消所有 TranslateTransform 动画，恢复到基准值
        /// </summary>
        private void CancelTransformAnimations()
        {
            PlotContainerTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            RightPanelTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            RightSplitterTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            PlotContainerTranslate.X = 0;
            RightPanelTranslate.X = 0;
            RightSplitterTranslate.X = 0;
        }

        public static MainPlotPage GetPage()
        {
            if (homePage == null)
            {
                homePage = new MainPlotPage();
            }
            return homePage;
        }

        public Task CheckUpdatesIfNeededAsync()
        {
            return viewModel.CheckUpdatesIfNeededAsync();
        }

        // 点击展开列表
        private void OnTreeViewItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 校验事件的发送者是否就是事件的原始来源所在的TreeViewItem。阻止冒泡事件
            if (sender is TreeViewItem item)
            {
                // 从真正被点击的元素向上查找父类
                var sourceTvi = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject ?? item);

                // 冒泡事件
                if (item != sourceTvi)
                {
                    return;
                }
            }

            // 检查父类状态
            var originalSource = e.OriginalSource as DependencyObject;
            if (originalSource != null && FindAncestor<ToggleButton>(originalSource) != null)
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
                var clickedItem = originalSource != null ? FindAncestor<TreeViewItem>(originalSource) : null;
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

                // 复选框（图层显隐按钮）需要保留自身点击行为，不能被树节点选择逻辑拦截
                if (FindAncestor<CheckBox>(originalSource) != null)
                {
                    return;
                }

                // 删除按钮需要保留自身点击行为
                if (FindAncestor<Button>(originalSource) != null)
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
        private T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
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

            _isUpdatingToolbarState = true;
            try
            {
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

                // Text Color - Update color picker to reflect current selection's color
                if (RichTextColorPicker != null)
                {
                    var foreground = Drichtextbox.Selection.GetPropertyValue(TextElement.ForegroundProperty);
                    if (foreground != DependencyProperty.UnsetValue && foreground is SolidColorBrush brush)
                    {
                        // Update the color picker to show the current selection's color
                        RichTextColorPicker.SelectedColor = brush.Color;
                    }
                    else
                    {
                        // Default to black if no color is set
                        RichTextColorPicker.SelectedColor = System.Windows.Media.Colors.Black;
                    }
                }
            }
            finally
            {
                _isUpdatingToolbarState = false;
            }
        }

        #endregion

        private Window? _popOutWindow;

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
        private Window? _parentWindow;

        /// <summary>
        /// 附加键盘事件到父窗口
        /// </summary>
        private void AttachKeyboardEvents()
        {
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _parentWindow.PreviewKeyDown += Window_PreviewKeyDown;
                _parentWindow.PreviewKeyUp += Window_PreviewKeyUp;
            }
        }

        /// <summary>
        /// 移除键盘事件
        /// </summary>
        private void DetachKeyboardEvents()
        {
            if (_parentWindow != null)
            {
                _parentWindow.PreviewKeyDown -= Window_PreviewKeyDown;
                _parentWindow.PreviewKeyUp -= Window_PreviewKeyUp;
                _parentWindow = null;
            }
        }

        /// <summary>
        /// Ctrl 键按下事件 - 显示卡片操作遮罩
        /// </summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 只在模板库模式下生效
            if (!viewModel.IsPlotMode && (e.Key == System.Windows.Input.Key.LeftCtrl || e.Key == System.Windows.Input.Key.RightCtrl))
            {
                var count = viewModel.TemplateCardsView?.Cast<object>()?.Count() ?? 0;
                System.Diagnostics.Debug.WriteLine($"Ctrl Key Down - IsPlotMode: {viewModel.IsPlotMode}, TemplateCardsView Count: {count}");
                UpdateCtrlOverlayState(true);
            }
        }

        /// <summary>
        /// Ctrl 键释放事件 - 隐藏卡片操作遮罩
        /// </summary>
        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.LeftCtrl || e.Key == System.Windows.Input.Key.RightCtrl)
            {
                System.Diagnostics.Debug.WriteLine($"Ctrl Key Up");
                UpdateCtrlOverlayState(false);
            }
        }

        /// <summary>
        /// 更新所有卡片的 Ctrl 遮罩状态
        /// </summary>
        private void UpdateCtrlOverlayState(bool isVisible)
        {
            if (viewModel?.TemplateCardsView == null)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCtrlOverlayState - TemplateCardsView is null");
                return;
            }

            int updatedCount = 0;
            foreach (var card in viewModel.TemplateCardsView)
            {
                if (card is TemplateCardViewModel cardVm)
                {
                    cardVm.IsCtrlOverlayVisible = isVisible;
                    updatedCount++;
                }
            }
            System.Diagnostics.Debug.WriteLine($"UpdateCtrlOverlayState - Updated {updatedCount} cards to IsVisible={isVisible}");
        }

        /// <summary>
        /// 处理 TreeViewItem 的 RequestBringIntoView 事件，阻止横向自动滚动
        /// </summary>
        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // 完全阻止自动滚动行为，保持滚动条在当前位置（默认最左侧）
            e.Handled = true;
        }

        /// <summary>
        /// 处理 TreeView 的鼠标滚轮事件，当 TreeView 滚动到边界时将事件传递给父级 ScrollViewer
        /// </summary>
        private void TreeView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is TreeView treeView)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(treeView);
                if (scrollViewer != null)
                {
                    // 检查是否已经滚动到边界
                    bool isAtTop = scrollViewer.VerticalOffset <= 0;
                    bool isAtBottom = scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight;

                    // 向上滚且已到顶部，或向下滚且已到底部时，将事件传递给父级
                    if ((e.Delta > 0 && isAtTop) || (e.Delta < 0 && isAtBottom))
                    {
                        e.Handled = true;
                        
                        // 找到父级 ScrollViewer 并手动触发滚动
                        var parentScrollViewer = FindVisualParent<ScrollViewer>(treeView);
                        if (parentScrollViewer != null)
                        {
                            // 创建新的鼠标滚轮事件并在父级 ScrollViewer 上触发
                            var newEventArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                            {
                                RoutedEvent = UIElement.MouseWheelEvent,
                                Source = parentScrollViewer
                            };
                            parentScrollViewer.RaiseEvent(newEventArgs);
                        }
                    }
                }
            }
        }

        private void OnTemplateCardsPresentationChanged()
        {
            RequestVisibleTemplateCardThumbnails();

            // 卡片重建后：若 pending 或 sticky 目标仍在，重新恢复（覆盖延迟 Clear 冲掉滚动的情况）
            if (_pendingTemplateCardsScrollOffset is > 0 || _stickyTemplateCardsScrollOffset is > 0)
            {
                if (_pendingTemplateCardsScrollOffset == null && _stickyTemplateCardsScrollOffset is double sticky)
                    _pendingTemplateCardsScrollOffset = sticky;

                // 再次隐藏，避免 Clear 后顶部内容在恢复前露出来
                if (_stickyTemplateCardsScrollOffset is > 0)
                {
                    _hideTemplateCardsUntilScrollRestored = true;
                    SetTemplateCardsVisibilityForScrollRestore(visible: false);
                }

                _ = TryRestoreTemplateCardsScrollAsync();
            }
        }

        private void AttachTemplateCardsScrollBehavior()
        {
            TemplateCardsControl.Loaded += (_, _) =>
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(TemplateCardsControl);
                if (scrollViewer == null)
                    return;

                scrollViewer.ScrollChanged -= TemplateCardsScrollViewer_ScrollChanged;
                scrollViewer.ScrollChanged += TemplateCardsScrollViewer_ScrollChanged;
                scrollViewer.SizeChanged -= TemplateCardsScrollViewer_SizeChanged;
                scrollViewer.SizeChanged += TemplateCardsScrollViewer_SizeChanged;
                ScheduleTemplateCardItemSizeUpdate();
                RequestVisibleTemplateCardThumbnails();
            };
        }

        private void AttachTemplateCardLayoutBehavior()
        {
            TemplateCardsControl.SizeChanged -= TemplateCardsControl_SizeChanged;
            TemplateCardsControl.SizeChanged += TemplateCardsControl_SizeChanged;
        }

        private void TemplateCardsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!e.WidthChanged)
                return;

            // 取消最大化等场景下，SizeChanged 可能早于 ViewportWidth 稳定，延后到布局完成再算
            ScheduleTemplateCardItemSizeUpdate(preserveScrollRatio: true);
        }

        private void TemplateCardsScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!e.WidthChanged)
                return;

            ScheduleTemplateCardItemSizeUpdate(preserveScrollRatio: true);
        }

        private Size _lastAppliedTemplateCardItemSize = new(TemplateCardLayoutHelper.DefaultCellSize, TemplateCardLayoutHelper.DefaultCellSize);
        private double _lastTemplateCardAvailableWidth;
        private bool _templateCardItemSizeRetryPending;
        private bool _templateCardItemSizeUpdateScheduled;
        private bool _pendingPreserveScrollRatio;

        private void ScheduleTemplateCardItemSizeUpdate(bool preserveScrollRatio = false)
        {
            _pendingPreserveScrollRatio |= preserveScrollRatio;
            if (_templateCardItemSizeUpdateScheduled)
                return;

            _templateCardItemSizeUpdateScheduled = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _templateCardItemSizeUpdateScheduled = false;
                bool preserve = _pendingPreserveScrollRatio;
                _pendingPreserveScrollRatio = false;
                UpdateTemplateCardItemSize(preserve);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void UpdateTemplateCardItemSize(bool preserveScrollRatio = false)
        {
            if (viewModel == null || TemplateCardsControl == null)
                return;

            var scrollViewer = FindVisualChild<ScrollViewer>(TemplateCardsControl);
            double availableWidth = scrollViewer?.ViewportWidth ?? 0;
            if (availableWidth <= 0)
                availableWidth = TemplateCardsControl.ActualWidth;

            // ListBox 右侧 Padding=10，与 TemplateCardListBoxStyle 一致
            if (availableWidth > 10)
                availableWidth -= 10;

            if (availableWidth <= 0)
                return;

            var newSize = TemplateCardLayoutHelper.ComputeItemSize(
                availableWidth,
                viewModel.TemplateCardSizePreset);

            bool widthUnchanged = Math.Abs(availableWidth - _lastTemplateCardAvailableWidth) < 0.5;
            bool sizeUnchanged = Math.Abs(newSize.Width - _lastAppliedTemplateCardItemSize.Width) < 0.5
                && Math.Abs(newSize.Height - _lastAppliedTemplateCardItemSize.Height) < 0.5;
            if (widthUnchanged && sizeUnchanged)
                return;

            double? scrollRatio = null;
            if (preserveScrollRatio && scrollViewer != null && scrollViewer.ExtentHeight > 0)
            {
                scrollRatio = scrollViewer.VerticalOffset / scrollViewer.ExtentHeight;
            }

            var panel = FindVisualChild<VirtualizingWrapPanel>(TemplateCardsControl);
            if (panel == null)
            {
                if (_templateCardItemSizeRetryPending)
                    return;

                _templateCardItemSizeRetryPending = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _templateCardItemSizeRetryPending = false;
                    UpdateTemplateCardItemSize(preserveScrollRatio);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            panel.ItemSize = newSize;
            _lastAppliedTemplateCardItemSize = newSize;
            _lastTemplateCardAvailableWidth = availableWidth;

            // 强制面板按新 ItemSize 重新测量（取消最大化后仅改属性可能不立刻重排）
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
            TemplateCardsControl.UpdateLayout();
            scrollViewer?.UpdateLayout();

            if (scrollRatio is double ratio && scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(ratio * scrollViewer.ExtentHeight);
            }

            RequestVisibleTemplateCardThumbnails();
        }

        private void TemplateCardsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 视口宽度变化（含取消最大化后滚动条显隐）时同步刷新列布局
            if (e.ViewportWidthChange != 0)
            {
                ScheduleTemplateCardItemSizeUpdate(preserveScrollRatio: true);
            }

            // 虚拟化面板 Extent 延后增高时，用 sticky 目标再次恢复，避免停在半截列表顶部
            if (e.ExtentHeightChange != 0 &&
                (_pendingTemplateCardsScrollOffset is > 0 || _stickyTemplateCardsScrollOffset is > 0))
            {
                if (_pendingTemplateCardsScrollOffset == null &&
                    _stickyTemplateCardsScrollOffset is double stickyExtent)
                {
                    _pendingTemplateCardsScrollOffset = stickyExtent;
                }

                _ = TryRestoreTemplateCardsScrollAsync();
            }

            if (e.VerticalChange == 0 && e.ViewportHeightChange == 0 && e.ViewportWidthChange == 0 && e.ExtentHeightChange == 0)
                return;

            RequestVisibleTemplateCardThumbnails();
        }

        private void RequestVisibleTemplateCardThumbnails()
        {
            if (viewModel == null)
                return;

            var visibleCards = GetVisibleTemplateCards().ToList();
            if (visibleCards.Count > 0)
                viewModel.RequestTemplateCardThumbnails(visibleCards);
        }

        private IEnumerable<TemplateCardViewModel> GetVisibleTemplateCards()
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(TemplateCardsControl);
            if (scrollViewer == null)
                yield break;

            var viewport = new Rect(0, scrollViewer.VerticalOffset, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
            foreach (var item in TemplateCardsControl.Items)
            {
                if (item is not TemplateCardViewModel card)
                    continue;

                var container = TemplateCardsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (container == null)
                    continue;

                if (IsTemplateCardVisibleInViewport(container, scrollViewer, viewport))
                    yield return card;
            }
        }

        private static bool IsTemplateCardVisibleInViewport(FrameworkElement container, ScrollViewer scrollViewer, Rect viewport)
        {
            try
            {
                var itemBounds = container.TransformToAncestor(scrollViewer)
                    .TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
                return viewport.IntersectsWith(itemBounds);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private double? _pendingTemplateCardsScrollOffset;
        /// <summary>
        /// 返回模板库后短暂保留的目标偏移：即使一次恢复成功，若随后卡片被 Clear 重建，仍可再次恢复。
        /// </summary>
        private double? _stickyTemplateCardsScrollOffset;
        private CancellationTokenSource? _stickyScrollCts;
        private TaskCompletionSource<bool>? _scrollRestoreTcs;
        private bool _hideTemplateCardsUntilScrollRestored;

        private double GetTemplateCardsScrollOffset()
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(TemplateCardsControl);
            return scrollViewer?.VerticalOffset ?? 0;
        }

        private Task RestoreTemplateCardsScrollAsync(double offset)
        {
            _pendingTemplateCardsScrollOffset = offset;
            if (offset > 0)
            {
                _stickyTemplateCardsScrollOffset = offset;
                _hideTemplateCardsUntilScrollRestored = true;
                SetTemplateCardsVisibilityForScrollRestore(visible: false);
                _stickyScrollCts?.Cancel();
                _stickyScrollCts = new CancellationTokenSource();
                var token = _stickyScrollCts.Token;
                _ = ClearStickyScrollAfterDelayAsync(token);
            }
            else
            {
                _stickyTemplateCardsScrollOffset = null;
                _hideTemplateCardsUntilScrollRestored = false;
                SetTemplateCardsVisibilityForScrollRestore(visible: true);
            }

            return TryRestoreTemplateCardsScrollAsync();
        }

        private void SetTemplateCardsVisibilityForScrollRestore(bool visible)
        {
            if (TemplateCardsControl == null)
                return;

            TemplateCardsControl.Opacity = visible ? 1 : 0;
        }

        private async Task ClearStickyScrollAfterDelayAsync(CancellationToken token)
        {
            try
            {
                // 全量重建时虚拟化 Extent 增高较慢，保留更久以便 ExtentHeightChange 继续纠正
                await Task.Delay(2500, token);
                if (!token.IsCancellationRequested)
                    _stickyTemplateCardsScrollOffset = null;
            }
            catch (TaskCanceledException)
            {
            }
        }

        private Task TryRestoreTemplateCardsScrollAsync()
        {
            var offset = _pendingTemplateCardsScrollOffset ?? _stickyTemplateCardsScrollOffset;
            if (offset is not double desiredOffset)
            {
                return Task.CompletedTask;
            }

            // 目标为顶部时无需重试
            if (desiredOffset <= 0)
            {
                var scrollViewerAtTop = FindVisualChild<ScrollViewer>(TemplateCardsControl);
                scrollViewerAtTop?.ScrollToVerticalOffset(0);
                _pendingTemplateCardsScrollOffset = null;
                FinishScrollRestoreAndShowCards();
                return Task.CompletedTask;
            }

            // 若已有进行中的恢复，复用同一个 Task，避免并发重试互相干扰
            if (_scrollRestoreTcs != null && !_scrollRestoreTcs.Task.IsCompleted)
            {
                return _scrollRestoreTcs.Task;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _scrollRestoreTcs = tcs;

            Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    double lastScrollableHeight = -1;
                    var stableScrollableCount = 0;

                    for (var attempt = 0; attempt < 40; attempt++)
                    {
                        var pending = _pendingTemplateCardsScrollOffset ?? _stickyTemplateCardsScrollOffset;
                        if (pending is not double pendingOffset || pendingOffset <= 0)
                        {
                            FinishScrollRestoreAndShowCards();
                            return;
                        }

                        desiredOffset = pendingOffset;
                        var scrollViewer = FindVisualChild<ScrollViewer>(TemplateCardsControl);
                        if (scrollViewer == null)
                        {
                            await Task.Delay(40);
                            continue;
                        }

                        // 强制一次布局，让批量加载的卡片尽快参与测量
                        TemplateCardsControl.UpdateLayout();
                        scrollViewer.UpdateLayout();

                        if (scrollViewer.ScrollableHeight <= 0)
                        {
                            await Task.Delay(40);
                            continue;
                        }

                        if (Math.Abs(scrollViewer.ScrollableHeight - lastScrollableHeight) < 0.5)
                            stableScrollableCount++;
                        else
                            stableScrollableCount = 0;
                        lastScrollableHeight = scrollViewer.ScrollableHeight;

                        // 先滚到当前可达位置，促使虚拟化面板继续增高 Extent
                        var targetOffset = Math.Min(desiredOffset, scrollViewer.ScrollableHeight);
                        scrollViewer.ScrollToVerticalOffset(targetOffset);
                        scrollViewer.UpdateLayout();

                        var heightEnough = scrollViewer.ScrollableHeight + 1 >= desiredOffset;
                        // 仅在高度足够，或多次重试后高度已稳定无法再增高时，才接受当前偏移
                        var extentExhausted = !heightEnough && stableScrollableCount >= 5 && attempt >= 15;
                        if (!heightEnough && !extentExhausted)
                        {
                            await Task.Delay(40);
                            continue;
                        }

                        if (Math.Abs(scrollViewer.VerticalOffset - targetOffset) >= 1)
                        {
                            await Task.Delay(40);
                            continue;
                        }

                        // 再等一帧 Render，确认合成侧也已是目标偏移
                        await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);

                        scrollViewer = FindVisualChild<ScrollViewer>(TemplateCardsControl);
                        if (scrollViewer == null)
                        {
                            await Task.Delay(40);
                            continue;
                        }

                        targetOffset = Math.Min(desiredOffset, scrollViewer.ScrollableHeight);
                        if (Math.Abs(scrollViewer.VerticalOffset - targetOffset) >= 1)
                        {
                            scrollViewer.ScrollToVerticalOffset(targetOffset);
                            await Task.Delay(40);
                            continue;
                        }

                        // 高度仍不足目标时保留 sticky，供后续 ExtentHeightChange 继续纠正
                        if (scrollViewer.ScrollableHeight + 1 >= desiredOffset
                            || Math.Abs(scrollViewer.VerticalOffset - desiredOffset) < 1)
                        {
                            _pendingTemplateCardsScrollOffset = null;
                        }

                        FinishScrollRestoreAndShowCards();
                        return;
                    }
                }
                finally
                {
                    // 重试耗尽也结束等待并显示列表，避免遮罩一直不关
                    FinishScrollRestoreAndShowCards();
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);

            return tcs.Task;
        }

        private void FinishScrollRestoreAndShowCards()
        {
            if (_hideTemplateCardsUntilScrollRestored)
            {
                _hideTemplateCardsUntilScrollRestored = false;
                SetTemplateCardsVisibilityForScrollRestore(visible: true);
            }

            CompleteScrollRestore();
        }

        private void CompleteScrollRestore()
        {
            var tcs = _scrollRestoreTcs;
            if (tcs != null && !tcs.Task.IsCompleted)
            {
                tcs.TrySetResult(true);
            }
        }

        /// <summary>
        /// 在可视化树中查找指定类型的子控件
        /// </summary>
        private T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
            return null;
        }

        /// <summary>
        /// 在可视化树中向上查找指定类型的父控件
        /// </summary>
        private T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;

            if (parent is T result)
            {
                return result;
            }

            return FindVisualParent<T>(parent);
        }

        /// <summary>
        /// 处理数据表格大小变化事件，动态计算验证区域的最大高度（为表格高度的 1/4）
        /// </summary>
        private void DataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is unvell.ReoGrid.ReoGridControl dataGrid && viewModel != null)
            {
                // 计算验证区域的最大高度为数据表格高度的 1/4
                double maxHeight = dataGrid.ActualHeight / 4.0;
                // 确保有一个最小高度，避免过小
                if (maxHeight < 100)
                {
                    maxHeight = 100;
                }
                viewModel.DataGridMaxVerificationHeight = maxHeight;
            }
        }
    }
}
