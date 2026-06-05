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
        private bool _isUpdatingToolbarState = false;

        public MainPlotPage()
        {
            InitializeComponent();
            // 链接 ViewModel
            viewModel = new MainPlotViewModel(this.WpfPlot1, this.Drichtextbox, this.DataGrid);
            this.DataContext = viewModel;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.GetTemplateCardsScrollOffset = GetTemplateCardsScrollOffset;
            viewModel.RestoreTemplateCardsScrollRequested += RestoreTemplateCardsScroll;
            AttachBreadcrumbScrollBehavior();

            // 页面加载时检查更新（等待首次模板库加载完成）
            this.Loaded += async (s, e) =>
            {
                await viewModel.CheckUpdatesIfNeededAsync();
                // 页面加载后，获取窗口并添加键盘事件监听
                AttachKeyboardEvents();
            };

            this.Unloaded += (s, e) =>
            {
                DetachKeyboardEvents();
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
        private Window _parentWindow;

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

        private double GetTemplateCardsScrollOffset()
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(TemplateCardsControl);
            return scrollViewer?.VerticalOffset ?? 0;
        }

        private void RestoreTemplateCardsScroll(double offset)
        {
            Dispatcher.InvokeAsync(async () =>
            {
                for (var attempt = 0; attempt < 6; attempt++)
                {
                    var scrollViewer = FindVisualChild<ScrollViewer>(TemplateCardsControl);
                    if (scrollViewer == null)
                    {
                        return;
                    }

                    var targetOffset = Math.Min(offset, scrollViewer.ScrollableHeight);
                    scrollViewer.ScrollToVerticalOffset(targetOffset);

                    if (Math.Abs(scrollViewer.VerticalOffset - targetOffset) < 1)
                    {
                        return;
                    }

                    await Task.Delay(50);
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 在可视化树中查找指定类型的子控件
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
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
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
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
