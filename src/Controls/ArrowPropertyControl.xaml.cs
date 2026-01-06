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
    public partial class ArrowPropertyControl : UserControl
    {
        // 防止在通过代码更新UI时，UI的ValueChanged事件又反过来更新属性
        private bool _isUpdatingFromSource = false;
        
        // 当前绑定的 ArrowDefinition
        private ArrowDefinition? _currentArrowDefinition;

        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(ArrowPropertyControl),
                new PropertyMetadata(null, OnSelectedObjectChanged));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public ArrowPropertyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 当 SelectedObject 依赖属性变化时调用
        /// </summary>
        private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ArrowPropertyControl)d;
            var oldArrow = e.OldValue as ArrowDefinition;
            var newArrow = e.NewValue as ArrowDefinition;

            // 解绑旧对象的事件
            if (oldArrow != null)
            {
                if (oldArrow.Start != null)
                    oldArrow.Start.PropertyChanged -= control.OnPointPropertyChanged;
                if (oldArrow.End != null)
                    oldArrow.End.PropertyChanged -= control.OnPointPropertyChanged;
            }

            // 绑定新对象的事件
            control._currentArrowDefinition = newArrow;
            if (newArrow != null)
            {
                if (newArrow.Start != null)
                    newArrow.Start.PropertyChanged += control.OnPointPropertyChanged;
                if (newArrow.End != null)
                    newArrow.End.PropertyChanged += control.OnPointPropertyChanged;
            }

            // 更新 UI
            control.UpdateUIFromArrowDefinition();
        }

        /// <summary>
        /// 当 PointDefinition 的属性变化时更新 UI
        /// </summary>
        private void OnPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PointDefinition.X) || e.PropertyName == nameof(PointDefinition.Y))
            {
                Dispatcher.Invoke(() => UpdateUIFromArrowDefinition());
            }
        }

        /// <summary>
        /// 控件加载时更新坐标标签和初始值
        /// </summary>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCoordinateLabels();
            UpdateUIFromArrowDefinition();
        }

        /// <summary>
        /// 根据三元图模式更新坐标标签
        /// </summary>
        private void UpdateCoordinateLabels()
        {
            string xLabel = TernaryCoordinateHelper.XAxisLabel;
            string yLabel = TernaryCoordinateHelper.YAxisLabel;

            // 更新起点坐标标签
            InfoElement.SetTitle(StartX, xLabel);
            InfoElement.SetTitle(StartY, yLabel);

            // 更新终点坐标标签
            InfoElement.SetTitle(EndX, xLabel);
            InfoElement.SetTitle(EndY, yLabel);
        }

        /// <summary>
        /// 从 ArrowDefinition 更新 UI 控件
        /// </summary>
        private void UpdateUIFromArrowDefinition()
        {
            _isUpdatingFromSource = true;

            if (_currentArrowDefinition != null)
            {
                // 转换起点坐标
                if (_currentArrowDefinition.Start != null)
                {
                    var (displayX, displayY) = TernaryCoordinateHelper.CartesianToDisplay(
                        _currentArrowDefinition.Start.X,
                        _currentArrowDefinition.Start.Y);
                    StartX.Value = Math.Round(displayX, 4);
                    StartY.Value = Math.Round(displayY, 4);
                }

                // 转换终点坐标
                if (_currentArrowDefinition.End != null)
                {
                    var (displayX, displayY) = TernaryCoordinateHelper.CartesianToDisplay(
                        _currentArrowDefinition.End.X,
                        _currentArrowDefinition.End.Y);
                    EndX.Value = Math.Round(displayX, 4);
                    EndY.Value = Math.Round(displayY, 4);
                }
            }
            else
            {
                StartX.Value = 0;
                StartY.Value = 0;
                EndX.Value = 0;
                EndY.Value = 0;
            }

            _isUpdatingFromSource = false;
        }

        /// <summary>
        /// 起点坐标值变化事件处理
        /// </summary>
        private void StartCoordinate_ValueChanged(object sender, FunctionEventArgs<double> e)
        {
            if (_isUpdatingFromSource || _currentArrowDefinition?.Start == null) return;

            // 将显示坐标转换为笛卡尔坐标存储
            var (cartesianX, cartesianY) = TernaryCoordinateHelper.DisplayToCartesian(
                StartX.Value, StartY.Value);

            _currentArrowDefinition.Start.X = cartesianX;
            _currentArrowDefinition.Start.Y = cartesianY;
        }

        /// <summary>
        /// 终点坐标值变化事件处理
        /// </summary>
        private void EndCoordinate_ValueChanged(object sender, FunctionEventArgs<double> e)
        {
            if (_isUpdatingFromSource || _currentArrowDefinition?.End == null) return;

            // 将显示坐标转换为笛卡尔坐标存储
            var (cartesianX, cartesianY) = TernaryCoordinateHelper.DisplayToCartesian(
                EndX.Value, EndY.Value);

            _currentArrowDefinition.End.X = cartesianX;
            _currentArrowDefinition.End.Y = cartesianY;
        }

        private void PickStartPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentArrowDefinition != null)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(_currentArrowDefinition.Start));
            }
        }

        private void PickEndPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentArrowDefinition != null)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(_currentArrowDefinition.End));
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