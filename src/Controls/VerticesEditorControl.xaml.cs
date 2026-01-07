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
using HandyControl.Controls;

namespace GeoChemistryNexus.Controls
{
    public partial class VerticesEditorControl : UserControl
    {
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

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
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