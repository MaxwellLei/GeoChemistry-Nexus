using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 多语言表格的默认列宽：附加在外层 ScrollViewer 上，让所有尚未被用户手动调整的语言列平分可用宽度
    /// （每列不小于 <see cref="MinimumAutoWidth"/>）。用户拖动过的列保持自己的宽度。
    /// </summary>
    public static class LocalizationTableLayoutBehavior
    {
        /// <summary>字段名称列的宽度，与表格模板中的固定列宽保持一致。</summary>
        public const double ItemColumnWidth = 96;

        /// <summary>自动平分时每列的最小宽度。</summary>
        public const double MinimumAutoWidth = 220;

        // 卡片外框 + 冻结列右侧加粗分界线所占宽度
        private const double Chrome = 4;

        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.RegisterAttached(
            "Columns", typeof(IEnumerable), typeof(LocalizationTableLayoutBehavior),
            new PropertyMetadata(null, OnColumnsChanged));

        private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
            "State", typeof(LayoutState), typeof(LocalizationTableLayoutBehavior));

        public static IEnumerable? GetColumns(DependencyObject element) => (IEnumerable?)element.GetValue(ColumnsProperty);
        public static void SetColumns(DependencyObject element, IEnumerable? value) => element.SetValue(ColumnsProperty, value);

        private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer viewer) return;
            var state = (LayoutState?)viewer.GetValue(StateProperty);
            if (state == null)
            {
                state = new LayoutState(viewer);
                viewer.SetValue(StateProperty, state);
            }
            state.Attach(e.NewValue as IEnumerable);
        }

        private sealed class LayoutState
        {
            private readonly ScrollViewer _viewer;
            private INotifyCollectionChanged? _observed;

            public LayoutState(ScrollViewer viewer)
            {
                _viewer = viewer;
                viewer.ScrollChanged += (_, e) => { if (e.ViewportWidthChange != 0) Fit(); };
                viewer.Loaded += (_, _) => Fit();
            }

            public void Attach(IEnumerable? columns)
            {
                if (_observed != null) _observed.CollectionChanged -= OnCollectionChanged;
                _observed = columns as INotifyCollectionChanged;
                if (_observed != null) _observed.CollectionChanged += OnCollectionChanged;
                Fit();
            }

            private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Fit();

            private void Fit()
            {
                var columns = GetColumns(_viewer)?.OfType<EpmaLocalizationColumn>().ToList();
                double viewport = _viewer.ViewportWidth;
                if (columns == null || columns.Count == 0 || viewport <= 0) return;

                double customized = columns.Where(c => c.IsWidthCustomized).Sum(c => c.Width);
                int auto = columns.Count(c => !c.IsWidthCustomized);
                if (auto == 0) return;
                double each = Math.Max(MinimumAutoWidth, Math.Floor((viewport - ItemColumnWidth - Chrome - customized) / auto));
                foreach (var column in columns) column.SetAutoWidth(each);
            }
        }
    }
}
