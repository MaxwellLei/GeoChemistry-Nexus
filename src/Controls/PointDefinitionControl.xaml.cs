using GeoChemistryNexus.Models;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools.Extension;
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
            var newPoint = e.NewValue as PointDefinition;

            // 标志位，表示本次UI更新来源于数据源，避免触发循环更新
            control._isUpdatingFromSource = true;
            if (newPoint != null)
            {
                control.NumericUpDownX.Value = newPoint.X;
                control.NumericUpDownY.Value = newPoint.Y;
            }
            else
            {
                // 如果源为空，可以清空或设置为默认值
                control.NumericUpDownX.Value = 0;
                control.NumericUpDownY.Value = 0;
            }
            control._isUpdatingFromSource = false;
        }

        /// <summary>
        /// 当UI控件的值被用户修改时，更新PointValue依赖属性
        /// </summary>
        private void OnNumericUpDownValueChanged(object sender, FunctionEventArgs<double> e)
        {
            // 如果UI的更新是代码触发的，则直接返回
            if (_isUpdatingFromSource) return;

            // 创建一个新的PointDefinition实例并更新依赖属性
            PointValue = new PointDefinition
            {
                X = NumericUpDownX.Value,
                Y = NumericUpDownY.Value
            };
        }
    }
}
