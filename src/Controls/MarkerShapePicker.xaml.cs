using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GeoChemistryNexus.Controls
{
    public partial class MarkerShapePicker : UserControl
    {
        public static readonly DependencyProperty SelectedShapeProperty =
            DependencyProperty.Register(
                nameof(SelectedShape),
                typeof(PlotMarkerShape),
                typeof(MarkerShapePicker),
                new FrameworkPropertyMetadata(
                    PlotMarkerShape.FilledSquare,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedShapeChanged));

        public PlotMarkerShape SelectedShape
        {
            get => (PlotMarkerShape)GetValue(SelectedShapeProperty);
            set => SetValue(SelectedShapeProperty, value);
        }

        public IReadOnlyList<PlotMarkerShapeItem> MarkerShapes => PlotMarkerShapeHelper.GetMarkerShapes();

        public Geometry PreviewIcon
        {
            get => (Geometry)GetValue(PreviewIconProperty);
            private set => SetValue(PreviewIconPropertyKey, value);
        }

        public bool PreviewIsFilled
        {
            get => (bool)GetValue(PreviewIsFilledProperty);
            private set => SetValue(PreviewIsFilledPropertyKey, value);
        }

        private static readonly DependencyPropertyKey PreviewIconPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(PreviewIcon),
                typeof(Geometry),
                typeof(MarkerShapePicker),
                new PropertyMetadata(Geometry.Empty));

        public static readonly DependencyProperty PreviewIconProperty = PreviewIconPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey PreviewIsFilledPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(PreviewIsFilled),
                typeof(bool),
                typeof(MarkerShapePicker),
                new PropertyMetadata(true));

        public static readonly DependencyProperty PreviewIsFilledProperty = PreviewIsFilledPropertyKey.DependencyProperty;

        public ICommand ShowPickerCommand { get; }

        private long _lastClosedTicks;

        public MarkerShapePicker()
        {
            InitializeComponent();
            ShowPickerCommand = new RelayCommand(ShowPicker);
            ShapePopup.Closed += (_, _) => _lastClosedTicks = DateTime.Now.Ticks;
            UpdatePreview(SelectedShape);
        }

        private void ShowPicker()
        {
            // 避免点击触发按钮时立刻因 Popup 关闭再打开闪烁
            if (DateTime.Now.Ticks - _lastClosedTicks < 200 * 10000)
            {
                return;
            }

            ShapePopup.IsOpen = true;
        }

        private static void OnSelectedShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkerShapePicker picker && e.NewValue is PlotMarkerShape shape)
            {
                picker.UpdatePreview(shape);
            }
        }

        private void UpdatePreview(PlotMarkerShape shape)
        {
            var item = PlotMarkerShapeHelper.GetItem(shape);
            PreviewIcon = item.Icon;
            PreviewIsFilled = item.IsFilled;
        }

        private void ShapeList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ItemsControl.ContainerFromElement(ShapeList, e.OriginalSource as DependencyObject) is not ListBoxItem item)
            {
                return;
            }

            if (item.DataContext is not PlotMarkerShapeItem shapeItem)
            {
                return;
            }

            SelectedShape = shapeItem.Shape;
            ShapePopup.IsOpen = false;
            e.Handled = true;
        }
    }
}
