using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Converter;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.ViewModels;
using HandyControl.Controls;
using HandyControl.Data;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    public partial class TextPropertyControl : UserControl
    {
        // 防止在通过代码更新UI时，UI的ValueChanged事件又反过来更新属性
        private bool _isUpdatingFromSource = false;
        
        // 当前绑定的 TextDefinition
        private TextDefinition? _currentTextDefinition;

        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(TextPropertyControl),
                new PropertyMetadata(null, OnSelectedObjectChanged));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public TextPropertyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 当 SelectedObject 依赖属性变化时调用
        /// </summary>
        private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TextPropertyControl)d;
            var oldText = e.OldValue as TextDefinition;
            var newText = e.NewValue as TextDefinition;

            // 解绑旧对象的事件
            if (oldText != null && oldText.StartAndEnd != null)
            {
                oldText.StartAndEnd.PropertyChanged -= control.OnPointPropertyChanged;
            }

            // 绑定新对象的事件
            control._currentTextDefinition = newText;
            if (newText != null && newText.StartAndEnd != null)
            {
                newText.StartAndEnd.PropertyChanged += control.OnPointPropertyChanged;
            }

            // 更新 UI
            control.UpdateUIFromTextDefinition();
        }

        /// <summary>
        /// 当 PointDefinition 的属性变化时更新 UI
        /// </summary>
        private void OnPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PointDefinition.X) || e.PropertyName == nameof(PointDefinition.Y))
            {
                Dispatcher.Invoke(() => UpdateUIFromTextDefinition());
            }
        }

        /// <summary>
        /// 控件加载时更新坐标标签和初始值
        /// </summary>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCoordinateLabels();
            UpdateUIFromTextDefinition();
        }

        /// <summary>
        /// 根据三元图模式更新坐标标签
        /// </summary>
        private void UpdateCoordinateLabels()
        {
            string xLabel = TernaryCoordinateHelper.XAxisLabel;
            string yLabel = TernaryCoordinateHelper.YAxisLabel;

            InfoElement.SetTitle(PositionX, xLabel);
            InfoElement.SetTitle(PositionY, yLabel);
            
            // 三元图模式下限制最大值为1
            if (TernaryCoordinateHelper.IsTernaryMode)
            {
                PositionX.Maximum = 1.0;
                PositionY.Maximum = 1.0;
            }
            else
            {
                PositionX.Maximum = double.MaxValue;
                PositionY.Maximum = double.MaxValue;
            }
        }

        /// <summary>
        /// 从 TextDefinition 更新 UI 控件
        /// </summary>
        private void UpdateUIFromTextDefinition()
        {
            _isUpdatingFromSource = true;

            if (_currentTextDefinition?.StartAndEnd != null)
            {
                var (displayX, displayY) = TernaryCoordinateHelper.CartesianToDisplay(
                    _currentTextDefinition.StartAndEnd.X,
                    _currentTextDefinition.StartAndEnd.Y);
                PositionX.Value = Math.Round(displayX, 4);
                PositionY.Value = Math.Round(displayY, 4);
            }
            else
            {
                PositionX.Value = 0;
                PositionY.Value = 0;
            }

            _isUpdatingFromSource = false;
        }

        /// <summary>
        /// 位置坐标值变化事件处理
        /// </summary>
        private void PositionCoordinate_ValueChanged(object sender, FunctionEventArgs<double> e)
        {
            if (_isUpdatingFromSource || _currentTextDefinition?.StartAndEnd == null) return;

            // 将显示坐标转换为笛卡尔坐标存储
            var (cartesianX, cartesianY) = TernaryCoordinateHelper.DisplayToCartesian(
                PositionX.Value, PositionY.Value);

            _currentTextDefinition.StartAndEnd.X = cartesianX;
            _currentTextDefinition.StartAndEnd.Y = cartesianY;
        }

        private void PickPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTextDefinition?.StartAndEnd != null)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(_currentTextDefinition.StartAndEnd));
            }
        }
    }
}
