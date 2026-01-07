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
    public partial class LinePropertyControl : UserControl
    {
        // 防止在通过代码更新UI时，UI的ValueChanged事件又反过来更新属性
        private bool _isUpdatingFromSource = false;
        
        // 当前绑定的 LineDefinition
        private LineDefinition? _currentLineDefinition;

        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(LinePropertyControl),
                new PropertyMetadata(null, OnSelectedObjectChanged));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public LinePropertyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 当 SelectedObject 依赖属性变化时调用
        /// </summary>
        private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LinePropertyControl)d;
            var oldLine = e.OldValue as LineDefinition;
            var newLine = e.NewValue as LineDefinition;

            // 解绑旧对象的事件
            if (oldLine != null)
            {
                if (oldLine.Start != null)
                    oldLine.Start.PropertyChanged -= control.OnPointPropertyChanged;
                if (oldLine.End != null)
                    oldLine.End.PropertyChanged -= control.OnPointPropertyChanged;
            }

            // 绑定新对象的事件
            control._currentLineDefinition = newLine;
            if (newLine != null)
            {
                if (newLine.Start != null)
                    newLine.Start.PropertyChanged += control.OnPointPropertyChanged;
                if (newLine.End != null)
                    newLine.End.PropertyChanged += control.OnPointPropertyChanged;
            }

            // 更新 UI
            control.UpdateUIFromLineDefinition();
        }

        /// <summary>
        /// 当 PointDefinition 的属性变化时更新 UI
        /// </summary>
        private void OnPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PointDefinition.X) || e.PropertyName == nameof(PointDefinition.Y))
            {
                Dispatcher.Invoke(() => UpdateUIFromLineDefinition());
            }
        }

        /// <summary>
        /// 控件加载时更新坐标标签和初始值
        /// </summary>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCoordinateLabels();
            UpdateUIFromLineDefinition();
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
            
            // 三元图模式下限制最大值为1
            if (TernaryCoordinateHelper.IsTernaryMode)
            {
                StartX.Maximum = 1.0;
                StartY.Maximum = 1.0;
                EndX.Maximum = 1.0;
                EndY.Maximum = 1.0;
            }
            else
            {
                StartX.Maximum = double.MaxValue;
                StartY.Maximum = double.MaxValue;
                EndX.Maximum = double.MaxValue;
                EndY.Maximum = double.MaxValue;
            }
        }

        /// <summary>
        /// 从 LineDefinition 更新 UI 控件
        /// </summary>
        private void UpdateUIFromLineDefinition()
        {
            _isUpdatingFromSource = true;

            if (_currentLineDefinition != null)
            {
                // 转换起点坐标
                if (_currentLineDefinition.Start != null)
                {
                    var (displayX, displayY) = TernaryCoordinateHelper.CartesianToDisplay(
                        _currentLineDefinition.Start.X,
                        _currentLineDefinition.Start.Y);
                    StartX.Value = Math.Round(displayX, 4);
                    StartY.Value = Math.Round(displayY, 4);
                }

                // 转换终点坐标
                if (_currentLineDefinition.End != null)
                {
                    var (displayX, displayY) = TernaryCoordinateHelper.CartesianToDisplay(
                        _currentLineDefinition.End.X,
                        _currentLineDefinition.End.Y);
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
            if (_isUpdatingFromSource || _currentLineDefinition?.Start == null) return;

            // 将显示坐标转换为笛卡尔坐标存储
            var (cartesianX, cartesianY) = TernaryCoordinateHelper.DisplayToCartesian(
                StartX.Value, StartY.Value);

            _currentLineDefinition.Start.X = cartesianX;
            _currentLineDefinition.Start.Y = cartesianY;
        }

        /// <summary>
        /// 终点坐标值变化事件处理
        /// </summary>
        private void EndCoordinate_ValueChanged(object sender, FunctionEventArgs<double> e)
        {
            if (_isUpdatingFromSource || _currentLineDefinition?.End == null) return;

            // 将显示坐标转换为笛卡尔坐标存储
            var (cartesianX, cartesianY) = TernaryCoordinateHelper.DisplayToCartesian(
                EndX.Value, EndY.Value);

            _currentLineDefinition.End.X = cartesianX;
            _currentLineDefinition.End.Y = cartesianY;
        }

        private void PickStartPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLineDefinition != null)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(_currentLineDefinition.Start));
            }
        }

        private void PickEndPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLineDefinition != null)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(_currentLineDefinition.End));
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