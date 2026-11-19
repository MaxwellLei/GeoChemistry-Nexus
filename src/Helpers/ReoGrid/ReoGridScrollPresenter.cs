using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using unvell.ReoGrid;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 使用 WPF 自己布局的细滚动条代替 ReoGrid 内置滚动条（后者会占用固定宽度的边槽）。
    /// 只依赖 ReoGrid 的公开滚动接口，由 <see cref="GeoChemistryNexus.Controls.ReoGridTableHost"/> 创建。
    /// </summary>
    public sealed class ReoGridScrollPresenter
    {
        private readonly ReoGridControl _grid;
        private readonly Grid _host;
        private readonly ScrollBar _horizontal, _vertical, _nativeHorizontal, _nativeVertical;
        private readonly List<(DependencyPropertyDescriptor Descriptor, ScrollBar Bar)> _subscriptions = new();
        private bool _attached, _refreshPending, _synchronizing;

        public ReoGridScrollPresenter(ReoGridControl grid, Grid host, ScrollBar horizontal, ScrollBar vertical)
        {
            _grid = grid; _host = host; _horizontal = horizontal; _vertical = vertical;
            ScrollBar? nativeHorizontal = null, nativeVertical = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(grid); i++)
                if (VisualTreeHelper.GetChild(grid, i) is ScrollBar bar)
                {
                    if (bar.Orientation == Orientation.Horizontal) nativeHorizontal = bar;
                    else nativeVertical = bar;
                }
            if (nativeHorizontal is null || nativeVertical is null)
                throw new InvalidOperationException("ReoGrid's horizontal and vertical scroll controls were not found.");
            _nativeHorizontal = nativeHorizontal; _nativeVertical = nativeVertical;
            _grid.Loaded += (_, _) => Attach();
            _grid.Unloaded += (_, _) => Detach();
            Attach();
        }

        private void Attach()
        {
            if (_attached) return;
            _attached = true;
            // 关闭原生滚动条同时会释放 ReoGrid 内部为它们预留的绘制区域。
            _grid.SetSettings(WorkbookSettings.View_ShowScrolls, false);
            _grid.WorksheetScrolled += WorksheetScrolled;
            _grid.CurrentWorksheetChanged += RangeChanged;
            _host.SizeChanged += SizeChanged;
            _horizontal.SizeChanged += SizeChanged;
            _vertical.SizeChanged += SizeChanged;
            _horizontal.ValueChanged += ValueChanged;
            _vertical.ValueChanged += ValueChanged;
            _horizontal.MouseWheel += ScrollBarMouseWheel;
            _vertical.MouseWheel += ScrollBarMouseWheel;
            foreach (var native in new[] { _nativeHorizontal, _nativeVertical })
                foreach (var property in new[] { RangeBase.MaximumProperty, RangeBase.MinimumProperty, RangeBase.ValueProperty,
                    RangeBase.LargeChangeProperty, RangeBase.SmallChangeProperty, ScrollBar.ViewportSizeProperty })
                {
                    var descriptor = DependencyPropertyDescriptor.FromProperty(property, typeof(ScrollBar));
                    descriptor.AddValueChanged(native, RangeChanged);
                    _subscriptions.Add((descriptor, native));
                }
            QueueRefresh();
        }

        private void Detach()
        {
            if (!_attached) return;
            _attached = false;
            _grid.WorksheetScrolled -= WorksheetScrolled;
            _grid.CurrentWorksheetChanged -= RangeChanged;
            _host.SizeChanged -= SizeChanged;
            _horizontal.SizeChanged -= SizeChanged;
            _vertical.SizeChanged -= SizeChanged;
            _horizontal.ValueChanged -= ValueChanged;
            _vertical.ValueChanged -= ValueChanged;
            _horizontal.MouseWheel -= ScrollBarMouseWheel;
            _vertical.MouseWheel -= ScrollBarMouseWheel;
            foreach (var (descriptor, bar) in _subscriptions) descriptor.RemoveValueChanged(bar, RangeChanged);
            _subscriptions.Clear();
        }

        private void WorksheetScrolled(object? sender, unvell.ReoGrid.Events.WorksheetScrolledEventArgs e) => QueueRefresh();
        private void RangeChanged(object? sender, EventArgs e) => QueueRefresh();
        private void SizeChanged(object sender, SizeChangedEventArgs e) => QueueRefresh();

        private void QueueRefresh()
        {
            if (!_attached || _refreshPending || _synchronizing) return;
            _refreshPending = true;
            _grid.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _refreshPending = false;
                if (_attached) Refresh();
            }));
        }

        private void Refresh()
        {
            if (_host.ActualWidth <= 0 || _host.ActualHeight <= 0) return;
            var sheet = _grid.CurrentWorksheet;
            double scale = sheet.ScaleFactor;
            double contentWidth = sheet.ColumnCount == 0 ? 0
                : sheet.ColumnHeaders[sheet.ColumnCount - 1].Right * scale + (sheet.HasSettings(WorksheetSettings.View_ShowRowHeader) ? sheet.RowHeaderWidth : 0);
            double contentHeight = sheet.RowCount == 0 ? 0
                : sheet.RowHeaders[sheet.RowCount - 1].Bottom * scale + ReoGridTableAppearance.GetColumnHeaderHeight(sheet);
            double verticalWidth = double.IsFinite(_vertical.Width) ? _vertical.Width : 8;
            double horizontalHeight = double.IsFinite(_horizontal.Height) ? _horizontal.Height : 8;
            // 先假设两条滚动条都不显示，避免缩放后两条滚动条互相“撑住”对方一直可见。
            bool horizontal = false, vertical = false;
            for (int i = 0; i < 3; i++)
            {
                horizontal = contentWidth > _host.ActualWidth - (vertical ? verticalWidth : 0) + 0.5;
                vertical = contentHeight > _host.ActualHeight - (horizontal ? horizontalHeight : 0) + 0.5;
            }
            _synchronizing = true;
            try
            {
                _horizontal.Visibility = horizontal ? Visibility.Visible : Visibility.Collapsed;
                _vertical.Visibility = vertical ? Visibility.Visible : Visibility.Collapsed;
                Synchronize(_horizontal, _nativeHorizontal, horizontal);
                Synchronize(_vertical, _nativeVertical, vertical);
                double x = horizontal ? _nativeHorizontal.Value : 0;
                double y = vertical ? _nativeVertical.Value : 0;
                if (x != _nativeHorizontal.Value || y != _nativeVertical.Value) _grid.ScrollCurrentWorksheet(x, y);
            }
            finally { _synchronizing = false; }
        }

        private static void Synchronize(ScrollBar target, ScrollBar source, bool visible)
        {
            target.Minimum = 0;
            target.Maximum = visible ? source.Maximum : 0;
            target.ViewportSize = source.ViewportSize;
            target.LargeChange = source.LargeChange;
            target.SmallChange = source.SmallChange;
            target.Value = visible ? source.Value : 0;
        }

        private void ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_synchronizing) _grid.ScrollCurrentWorksheet(_horizontal.Value, _vertical.Value);
        }

        private void ScrollBarMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var bar = (ScrollBar)sender;
            double step = SystemParameters.WheelScrollLines < 0 ? bar.LargeChange
                : Math.Max(1, SystemParameters.WheelScrollLines) * (bar.Orientation == Orientation.Vertical ? 26 : 40);
            bar.Value = Math.Clamp(bar.Value - e.Delta / 120.0 * step, bar.Minimum, bar.Maximum);
            e.Handled = true;
        }
    }
}
