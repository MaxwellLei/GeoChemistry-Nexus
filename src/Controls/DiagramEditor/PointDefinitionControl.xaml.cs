using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 属性面板的“坐标点”一行：[彩色圆点 + 名称] X 输入框 Y 输入框 [拾取 / 锁定图标]。
    /// X / Y 前缀显示在输入框内部；三元图模式下自动在显示坐标与存储的笛卡尔坐标之间转换（始终修改原 PointDefinition 实例）。
    /// </summary>
    public partial class PointDefinitionControl : UserControl
    {
        // 防止在通过代码更新UI时,UI的ValueChanged事件又反过来更新属性
        private bool _isUpdatingFromSource = false;

        public static readonly DependencyProperty PointValueProperty =
            DependencyProperty.Register("PointValue", typeof(PointDefinition), typeof(PointDefinitionControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPointValueChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PointDefinitionControl),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        /// <summary>行首名称（如“起点”）；为空时不显示名称列。</summary>
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(PointDefinitionControl),
                new PropertyMetadata(string.Empty));

        /// <summary>名称前的彩色圆点；为空时不显示圆点。</summary>
        public static readonly DependencyProperty DotBrushProperty =
            DependencyProperty.Register(nameof(DotBrush), typeof(Brush), typeof(PointDefinitionControl),
                new PropertyMetadata(null));

        /// <summary>是否显示“在图上拾取”按钮（只读时显示锁定图标）。</summary>
        public static readonly DependencyProperty ShowPickButtonProperty =
            DependencyProperty.Register(nameof(ShowPickButton), typeof(bool), typeof(PointDefinitionControl),
                new PropertyMetadata(true));

        public PointDefinition PointValue
        {
            get { return (PointDefinition)GetValue(PointValueProperty); }
            set { SetValue(PointValueProperty, value); }
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public Brush? DotBrush
        {
            get => (Brush?)GetValue(DotBrushProperty);
            set => SetValue(DotBrushProperty, value);
        }

        public bool ShowPickButton
        {
            get => (bool)GetValue(ShowPickButtonProperty);
            set => SetValue(ShowPickButtonProperty, value);
        }

        public PointDefinitionControl()
        {
            InitializeComponent();
            // 监听UI控件的值变化事件
            NumericUpDownX.ValueChanged += OnNumericUpDownValueChanged;
            NumericUpDownY.ValueChanged += OnNumericUpDownValueChanged;
        }

        /// <summary>
        /// 当IsReadOnly属性变化时更新UI
        /// </summary>
        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PointDefinitionControl)d;
            bool isReadOnly = (bool)e.NewValue;
            control.NumericUpDownX.IsReadOnly = isReadOnly;
            control.NumericUpDownY.IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// 控件加载时更新坐标标签
        /// </summary>
        private void UserControl_Loaded(object? sender, RoutedEventArgs e)
        {
            UpdateCoordinateLabels();
            UpdateUIFromPoint(PointValue);
        }

        /// <summary>
        /// 根据三元图模式更新坐标标签
        /// </summary>
        private void UpdateCoordinateLabels()
        {
            ControlAssist.SetTitle(NumericUpDownX, TernaryCoordinateHelper.XAxisLabel);
            ControlAssist.SetTitle(NumericUpDownY, TernaryCoordinateHelper.YAxisLabel);

            // 三元图模式下限制最大值为1
            double max = TernaryCoordinateHelper.IsTernaryMode ? 1.0 : double.MaxValue;
            NumericUpDownX.Maximum = max;
            NumericUpDownY.Maximum = max;
        }

        /// <summary>
        /// 当PointValue依赖属性变化时,更新UI
        /// </summary>
        private static void OnPointValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (PointDefinitionControl)d;

            if (e.OldValue is PointDefinition oldPoint)
                oldPoint.PropertyChanged -= control.OnPointPropertyChanged;

            if (e.NewValue is PointDefinition newPoint)
                newPoint.PropertyChanged += control.OnPointPropertyChanged;

            control.UpdateUIFromPoint(e.NewValue as PointDefinition);
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
            // 标志位,表示本次UI更新来源于数据源,避免触发循环更新
            _isUpdatingFromSource = true;
            if (point != null)
            {
                var (displayX, displayY) = TernaryCoordinateHelper.CartesianToDisplay(point.X, point.Y);
                NumericUpDownX.Value = Math.Round(displayX, 4);
                NumericUpDownY.Value = Math.Round(displayY, 4);
            }
            else
            {
                NumericUpDownX.Value = 0;
                NumericUpDownY.Value = 0;
            }
            _isUpdatingFromSource = false;
        }

        /// <summary>
        /// 当UI控件的值被用户修改时,更新PointValue依赖属性
        /// </summary>
        private void OnNumericUpDownValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 如果UI的更新是代码触发的,则直接返回
            if (_isUpdatingFromSource) return;

            var (cartesianX, cartesianY) = TernaryCoordinateHelper.DisplayToCartesian(
                NumericUpDownX.Value, NumericUpDownY.Value);

            // 直接更新属性,而不是创建新对象,保持对象引用一致
            if (PointValue != null)
            {
                PointValue.X = cartesianX;
                PointValue.Y = cartesianY;
            }
            else
            {
                PointValue = new PointDefinition { X = cartesianX, Y = cartesianY };
            }
        }

        private void PickPointButton_Click(object? sender, RoutedEventArgs e)
        {
            // 发送拾取点请求
            if (PointValue != null)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(PointValue));
            }
        }

        private void Coordinate_GotFocus(object? sender, RoutedEventArgs e)
        {
            if (PointValue != null)
            {
                PointValue.IsHighlighted = true;
            }
        }

        private void Coordinate_LostFocus(object? sender, RoutedEventArgs e)
        {
            if (PointValue != null)
            {
                PointValue.IsHighlighted = false;
            }
        }
    }
}
