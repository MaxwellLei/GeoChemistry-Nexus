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
                    // 使用右侧边栏弹窗 (Growl.Ask)
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