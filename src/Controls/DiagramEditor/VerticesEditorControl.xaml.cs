using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel; // 引入此命名空间
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Converter;

namespace GeoChemistryNexus.Controls
{
    /// <summary>顶点编辑器视图模式</summary>
    public enum VerticesViewMode
    {
        Table,
        Text
    }

    public partial class VerticesEditorControl : UserControl
    {
        /// <summary>
        /// 视图模式（表格 / 文本）。切换按钮由外部放在分组标题栏，通过绑定本属性控制。
        /// </summary>
        public static readonly DependencyProperty ViewModeProperty =
            DependencyProperty.Register("ViewMode", typeof(VerticesViewMode), typeof(VerticesEditorControl),
                new FrameworkPropertyMetadata(VerticesViewMode.Table, OnViewModeChanged));

        public VerticesViewMode ViewMode
        {
            get { return (VerticesViewMode)GetValue(ViewModeProperty); }
            set { SetValue(ViewModeProperty, value); }
        }

        private static void OnViewModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VerticesEditorControl control && (VerticesViewMode)e.NewValue == VerticesViewMode.Text)
            {
                control.RefreshVertexText();
            }
        }

        public static readonly DependencyProperty VerticesValueProperty =
            DependencyProperty.Register("VerticesValue", typeof(ObservableCollection<PointDefinition>), typeof(VerticesEditorControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public ObservableCollection<PointDefinition> VerticesValue
        {
            get { return (ObservableCollection<PointDefinition>)GetValue(VerticesValueProperty); }
            set { SetValue(VerticesValueProperty, value); }
        }

        public VerticesEditorControl()
        {
            InitializeComponent();
            Loaded += VerticesEditorControl_Loaded;
        }
        
        /// <summary>
        /// 控件加载时设置数值输入框的最大值限制
        /// </summary>
        private void VerticesEditorControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateNumericUpDownMaximum();
        }
        
        /// <summary>
        /// 更新数值输入框的最大值限制
        /// </summary>
        private void UpdateNumericUpDownMaximum()
        {
            // 获取ItemsControl中的所有NumericUpDown控件
            var itemsControl = this.FindName("VerticesItemsControl") as ItemsControl;
            if (itemsControl == null) return;
            
            double maxValue = TernaryCoordinateHelper.IsTernaryMode ? 1.0 : double.MaxValue;
            
            // 遍历每个项目容器
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container != null)
                {
                    // 查找NumericUpDown控件
                    var numericUpDowns = FindVisualChildren<NumericUpDown>(container);
                    foreach (var numericUpDown in numericUpDowns)
                    {
                        numericUpDown.Maximum = maxValue;
                    }
                }
            }
        }
        
        /// <summary>
        /// 查找可视化树中的子元素
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    if (child != null)
                    {
                        foreach (T childOfChild in FindVisualChildren<T>(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// “添加新顶点”按钮
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // 如果列表为null，则先创建一个新的
            if (VerticesValue == null)
            {
                VerticesValue = new ObservableCollection<PointDefinition>();
            }
            // 添加顶点
            VerticesValue.Add(new PointDefinition { X = 0, Y = 0 });

            if (ViewMode == VerticesViewMode.Text)
            {
                RefreshVertexText();
            }
        }

        private void RefreshVertexText()
        {
            var vertices = VerticesValue;
            VertexTextBox.Text = vertices == null
                ? string.Empty
                : string.Join(System.Environment.NewLine,
                    vertices.Select(p => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}", p.X, p.Y)));
        }

        /// <summary>
        /// 文本视图失去焦点时，把每行 “X, Y” 解析并应用到顶点列表（无法解析的行会被忽略）
        /// </summary>
        private void VertexTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var parsed = new List<(double X, double Y)>();
            var separators = new[] { ',', ';', '\t', ' ' };
            foreach (var line in VertexTextBox.Text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2
                    && double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)
                    && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y))
                {
                    parsed.Add((x, y));
                }
            }

            if (VerticesValue == null)
            {
                VerticesValue = new ObservableCollection<PointDefinition>();
            }

            // 先复用已有顶点（保持对象引用与高亮状态），多退少补
            for (int i = 0; i < parsed.Count; i++)
            {
                if (i < VerticesValue.Count)
                {
                    VerticesValue[i].X = parsed[i].X;
                    VerticesValue[i].Y = parsed[i].Y;
                }
                else
                {
                    VerticesValue.Add(new PointDefinition { X = parsed[i].X, Y = parsed[i].Y });
                }
            }
            while (VerticesValue.Count > parsed.Count)
            {
                VerticesValue.RemoveAt(VerticesValue.Count - 1);
            }

            RefreshVertexText();
        }

        /// <summary>
        /// 拾取坐标按钮
        /// </summary>
        private void PickPointButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PointDefinition pointDef)
            {
                // 发送拾取请求消息
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(pointDef));
            }
        }

        /// <summary>
        /// 单个顶点后的“删除”按钮
        /// </summary>
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PointDefinition pointToRemove)
            {
                // 检查是否按下了 Ctrl 键
                bool isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

                if (!isCtrlPressed)
                {
                    // 弹窗
                    bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                        LanguageService.Instance["DeleteingBasemapConfirm2_Message"],
                        LanguageService.Instance["Cancel"],
                        LanguageService.Instance["Confirm"]);

                    if (!isConfirmed)
                    {
                        return;
                    }
                }

                // 删除顶点
                VerticesValue?.Remove(pointToRemove);
            }
        }

        private void Coordinate_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is PointDefinition point)
            {
                point.IsHighlighted = true;
            }
        }

        private void Coordinate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is PointDefinition point)
            {
                point.IsHighlighted = false;
            }
        }
    }
}