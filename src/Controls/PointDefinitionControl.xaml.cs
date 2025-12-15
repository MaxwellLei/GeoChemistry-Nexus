using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.ViewModels;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools.Extension;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// PointDefinitionControl.xaml 的交互逻辑
    /// </summary>
    public partial class PointDefinitionControl : UserControl
    {
        // 防止在通过代码更新UI时，UI的ValueChanged事件又反过来更新属性
        private bool _isUpdatingFromSource = false;

        public static readonly DependencyProperty PointValueProperty =
            DependencyProperty.Register("PointValue", typeof(PointDefinition), typeof(PointDefinitionControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPointValueChanged));

        public PointDefinition PointValue
        {
            get { return (PointDefinition)GetValue(PointValueProperty); }
            set { SetValue(PointValueProperty, value); }
        }

        public PointDefinitionControl()
        {
            InitializeComponent();
            // 监听UI控件的值变化事件
            NumericUpDownX.ValueChanged += OnNumericUpDownValueChanged;
            NumericUpDownY.ValueChanged += OnNumericUpDownValueChanged;
        }

        /// <summary>
        /// 当PointValue依赖属性变化时，更新UI
        /// </summary>
        private static void OnPointValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PointDefinitionControl)d;
            var oldPoint = e.OldValue as PointDefinition;
            var newPoint = e.NewValue as PointDefinition;

            // 解绑旧对象
            if (oldPoint != null)
            {
                oldPoint.PropertyChanged -= control.OnPointPropertyChanged;
            }

            // 绑定新对象
            if (newPoint != null)
            {
                newPoint.PropertyChanged += control.OnPointPropertyChanged;
            }

            // 更新 UI
            control.UpdateUIFromPoint(newPoint);
        }

        private void OnPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PointDefinition.X) || e.PropertyName == nameof(PointDefinition.Y))
            {
                // 在 UI 线程更新
                Dispatcher.Invoke(() => UpdateUIFromPoint(PointValue));
            }
        }

        private void UpdateUIFromPoint(PointDefinition? point)
        {
            // 标志位，表示本次UI更新来源于数据源，避免触发循环更新
            _isUpdatingFromSource = true;
            if (point != null)
            {
                // 如果是三元图就转换坐标为三元值
                if(MainPlotViewModel.BaseMapType == "Ternary")
                {
                    var ternary = MainPlotViewModel.ToTernary(point.X, point.Y, MainPlotViewModel.Clockwise);
                    NumericUpDownX.Value = Math.Round(ternary.Item1, 4);
                    NumericUpDownY.Value = Math.Round(ternary.Item2, 4);
                }
                else
                {
                    NumericUpDownX.Value = Math.Round(point.X, 4);
                    NumericUpDownY.Value = Math.Round(point.Y, 4);
                }
            }
            else
            {
                // 如果源为空，可以清空或设置为默认值
                NumericUpDownX.Value = 0;
                NumericUpDownY.Value = 0;
            }
            _isUpdatingFromSource = false;
        }

        /// <summary>
        /// 当UI控件的值被用户修改时，更新PointValue依赖属性
        /// </summary>
        private void OnNumericUpDownValueChanged(object sender, FunctionEventArgs<double> e)
        {
            // 如果UI的更新是代码触发的，则直接返回
            if (_isUpdatingFromSource) return;

            // 如果是三元图就转换坐标为笛卡尔值
            if(MainPlotViewModel.BaseMapType == "Ternary")
            {
                (Double tempX,Double tempY) = MainPlotViewModel.ToCartesian(NumericUpDownX.Value, NumericUpDownY.Value,
                    1 - NumericUpDownY.Value - NumericUpDownX.Value);
                PointValue = new PointDefinition
                {
                    X = tempX,
                    Y = tempY
                };
            }
            else
            {
                // 直接更新属性，而不是创建新对象，保持对象引用一致
                if (PointValue != null)
                {
                    PointValue.X = NumericUpDownX.Value;
                    PointValue.Y = NumericUpDownY.Value;
                }
                else
                {
                    PointValue = new PointDefinition
                    {
                        X = NumericUpDownX.Value,
                        Y = NumericUpDownY.Value
                    };
                }
            }
        }

        private void PickPointButton_Click(object sender, RoutedEventArgs e)
        {
            // 发送拾取点请求
            if (PointValue != null)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(PointValue));
            }
        }
    }
}
