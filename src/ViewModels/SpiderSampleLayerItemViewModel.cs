using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Helpers.PlotMarkers;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Models.SpiderDiagram;
using GeoChemistryNexus.Services;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.ViewModels
{
    public class SpiderSampleLayerItemViewModel : LayerItemViewModel, IPlotLayer
    {
        public event Action<SpiderSampleLayerItemViewModel, string>? SampleNameChanged;
        public override bool ShowInlineDeleteButton => true;

        public override void ClearEventSubscriptions()
        {
            SampleNameChanged = null;
            base.ClearEventSubscriptions();
        }

        private sealed class ScatterStyleState
        {
            public ScottPlot.Color Color { get; private set; }
            public float LineWidth { get; private set; }
            public float MarkerSize { get; private set; }
            public MarkerShape MarkerShape { get; private set; }
            public float MarkerOutlineWidth { get; private set; }
            public ScottPlot.Color MarkerOutlineColor { get; private set; }

            public ScatterStyleState(Scatter scatter)
            {
                UpdateFrom(scatter);
            }

            public void UpdateFrom(Scatter scatter)
            {
                Color = scatter.Color;
                LineWidth = scatter.LineWidth;
                MarkerSize = scatter.MarkerSize;
                MarkerShape = scatter.MarkerShape;
                MarkerOutlineWidth = scatter.MarkerStyle.OutlineWidth;
                MarkerOutlineColor = scatter.MarkerStyle.OutlineColor;
            }
        }

        private sealed class SpiderSampleSeriesEntry
        {
            public SpiderSampleData Sample { get; }
            public Scatter Scatter { get; }
            public ScatterStyleState BaseStyle { get; }

            public SpiderSampleSeriesEntry(SpiderSampleData sample, Scatter scatter)
            {
                Sample = sample;
                Scatter = scatter;
                BaseStyle = new ScatterStyleState(scatter);
            }
        }

        private readonly List<SpiderSampleSeriesEntry> _seriesEntries = new();
        private readonly Scatter? _legendProxy;
        private readonly WpfPlot? _wpfPlot;
        private Scatter? _activeScatter;
        private bool _hasExplicitActiveScatter;

        public SpiderSamplePropertyModel? PropertyModel { get; private set; }
        public IReadOnlyList<SpiderSampleData> Samples => _seriesEntries.Select(entry => entry.Sample).ToList();
        public SpiderSampleData? Sample => GetActiveSample() ?? _seriesEntries.FirstOrDefault()?.Sample;
        public int SeriesCount => _seriesEntries.Count;
        public Scatter? LegendProxy => _legendProxy;

        public SpiderSampleLayerItemViewModel(SpiderSampleData sample, Scatter scatter, Scatter legendProxy, WpfPlot? wpfPlot = null)
            : this(
                sample.Name,
                new[] { (Sample: sample, Scatter: scatter) },
                legendProxy,
                wpfPlot)
        {
        }

        public SpiderSampleLayerItemViewModel(
            string sampleName,
            IEnumerable<(SpiderSampleData Sample, Scatter Scatter)> seriesEntries,
            Scatter legendProxy,
            WpfPlot? wpfPlot = null)
            : base(sampleName)
        {
            _legendProxy = legendProxy;
            _wpfPlot = wpfPlot;

            foreach (var (sample, scatter) in seriesEntries)
            {
                if (sample == null || scatter == null)
                {
                    continue;
                }

                _seriesEntries.Add(new SpiderSampleSeriesEntry(sample, scatter));
            }

            ResetActivePlottable();
            PropertyModel = new SpiderSamplePropertyModel(_seriesEntries.Select(entry => entry.Scatter), legendProxy, wpfPlot);
            PropertyModel.SampleName = sampleName;
            PropertyModel.PropertyChanged += PropertyModel_PropertyChanged;
            PropertyChanged += SpiderSampleLayerItemViewModel_PropertyChanged;

            ApplyVisibility(IsVisible);
        }

        internal void RegisterPlottablesForLookup(Dictionary<IPlottable, LayerItemViewModel> lookup)
        {
            foreach (var entry in _seriesEntries)
            {
                lookup[entry.Scatter] = this;
            }

            if (_legendProxy != null)
            {
                lookup[_legendProxy] = this;
            }
        }

        public void Render(Plot plot)
        {
            // 数据已经在 RenderSpiderPlot 中渲染，这里不需要额外处理
        }

        public bool ContainsPlottable(IPlottable plottable)
        {
            if (plottable == null)
            {
                return false;
            }

            return _seriesEntries.Any(entry => ReferenceEquals(entry.Scatter, plottable));
        }

        public bool ContainsSourceRowIndex(int rowIndex)
        {
            if (rowIndex < 0)
            {
                return false;
            }

            return _seriesEntries.Any(entry => entry.Sample.SourceRowIndices.Contains(rowIndex));
        }

        public bool TrySetActiveByRowIndex(int rowIndex)
        {
            if (rowIndex < 0)
            {
                return false;
            }

            var match = _seriesEntries.FirstOrDefault(entry => entry.Sample.SourceRowIndices.Contains(rowIndex));
            if (match == null)
            {
                return false;
            }

            _activeScatter = match.Scatter;
            _hasExplicitActiveScatter = true;
            Plottable = match.Scatter;
            return true;
        }

        /// <summary>
        /// 获取当前活动系列上用于数据模式闪烁的坐标（绘图坐标系）。
        /// </summary>
        public bool TryGetFlashCoordinate(out Coordinates coordinate, int preferredPointIndex = -1)
        {
            coordinate = default;
            if (Plottable is not Scatter scatter)
            {
                return false;
            }

            var dataSource = scatter.GetIDataSource();
            if (dataSource == null || dataSource.Length <= 0)
            {
                return false;
            }

            int index = preferredPointIndex >= 0 && preferredPointIndex < dataSource.Length
                ? preferredPointIndex
                : Math.Clamp(dataSource.Length / 2, 0, dataSource.Length - 1);

            coordinate = dataSource.GetCoordinateScaled(index);
            return !double.IsNaN(coordinate.X) && !double.IsNaN(coordinate.Y)
                && !double.IsInfinity(coordinate.X) && !double.IsInfinity(coordinate.Y);
        }

        /// <summary>
        /// 在鼠标附近命中最近的样品系列顶点，并返回闪烁坐标与对应数据行。
        /// </summary>
        public bool TryHitNearPoint(
            Coordinates mouseCoordinates,
            RenderDetails render,
            float radius,
            out Coordinates flashCoordinate,
            out int sourceRowIndex,
            out int pointIndex)
        {
            flashCoordinate = default;
            sourceRowIndex = -1;
            pointIndex = -1;

            SpiderSampleSeriesEntry? bestEntry = null;
            int bestPointIndex = -1;
            double bestDistanceSquared = double.MaxValue;

            void Consider(SpiderSampleSeriesEntry entry, DataPoint nearest)
            {
                if (!nearest.IsReal)
                {
                    return;
                }

                double dx = nearest.X - mouseCoordinates.X;
                double dy = nearest.Y - mouseCoordinates.Y;
                double distanceSquared = dx * dx + dy * dy;
                if (distanceSquared >= bestDistanceSquared)
                {
                    return;
                }

                bestDistanceSquared = distanceSquared;
                bestEntry = entry;
                bestPointIndex = nearest.Index;
            }

            foreach (var entry in _seriesEntries)
            {
                if (entry.Scatter is not IGetNearest hittable)
                {
                    continue;
                }

                Consider(entry, hittable.GetNearest(mouseCoordinates, render, radius));
            }

            // 未命中顶点时，放大半径吸附最近顶点（覆盖点到折线的情况）
            if (bestEntry == null)
            {
                float expandedRadius = Math.Max(radius * 3f, 20f);
                foreach (var entry in _seriesEntries)
                {
                    if (entry.Scatter is not IGetNearest hittable)
                    {
                        continue;
                    }

                    Consider(entry, hittable.GetNearest(mouseCoordinates, render, expandedRadius));
                }
            }

            if (bestEntry == null || bestPointIndex < 0)
            {
                return false;
            }

            _activeScatter = bestEntry.Scatter;
            _hasExplicitActiveScatter = true;
            Plottable = bestEntry.Scatter;

            pointIndex = bestPointIndex;
            if (!TryGetFlashCoordinate(out flashCoordinate, pointIndex))
            {
                return false;
            }

            sourceRowIndex = bestEntry.Sample.SourceRowIndices.FirstOrDefault(index => index >= 0);
            return sourceRowIndex >= 0;
        }

        public void SetActivePlottable(IPlottable? plottable)
        {
            if (plottable is Scatter scatter)
            {
                var match = _seriesEntries.FirstOrDefault(entry => ReferenceEquals(entry.Scatter, scatter));
                if (match != null)
                {
                    _activeScatter = match.Scatter;
                    _hasExplicitActiveScatter = true;
                    Plottable = match.Scatter;
                    return;
                }
            }

            ResetActivePlottable();
        }

        /// <summary>
        /// 从分组中移除指定样品线（不负责从 Plot / Samples 集合移除）。
        /// </summary>
        public bool TryRemoveSample(
            SpiderSampleData sample,
            out Scatter? removedScatter,
            out int seriesIndex)
        {
            removedScatter = null;
            seriesIndex = -1;
            if (sample == null)
            {
                return false;
            }

            seriesIndex = _seriesEntries.FindIndex(entry => ReferenceEquals(entry.Sample, sample));
            if (seriesIndex < 0)
            {
                // 回退：按行号匹配
                seriesIndex = _seriesEntries.FindIndex(entry =>
                    entry.Sample.SourceRowIndices.Any(row => sample.SourceRowIndices.Contains(row)));
            }

            if (seriesIndex < 0)
            {
                return false;
            }

            var entry = _seriesEntries[seriesIndex];
            _seriesEntries.RemoveAt(seriesIndex);
            removedScatter = entry.Scatter;

            if (ReferenceEquals(_activeScatter, entry.Scatter) || !_hasExplicitActiveScatter)
            {
                ResetActivePlottable();
            }
            else
            {
                Plottable = _activeScatter;
            }

            RebuildPropertyModelKeepName();
            return true;
        }

        /// <summary>
        /// 撤销删除时把样品线插回分组。
        /// </summary>
        public void InsertSampleSeries(int seriesIndex, SpiderSampleData sample, Scatter scatter)
        {
            if (sample == null || scatter == null)
            {
                return;
            }

            var entry = new SpiderSampleSeriesEntry(sample, scatter);
            if (seriesIndex < 0 || seriesIndex > _seriesEntries.Count)
            {
                _seriesEntries.Add(entry);
            }
            else
            {
                _seriesEntries.Insert(seriesIndex, entry);
            }

            if (!_hasExplicitActiveScatter || _activeScatter == null)
            {
                ResetActivePlottable();
            }

            RebuildPropertyModelKeepName();
        }

        public IEnumerable<IPlottable> EnumerateManagedPlottables()
        {
            foreach (var entry in _seriesEntries)
            {
                yield return entry.Scatter;
            }

            if (_legendProxy != null)
            {
                yield return _legendProxy;
            }
        }

        private void RebuildPropertyModelKeepName()
        {
            var previousName = PropertyModel?.SampleName ?? Name;
            bool hadPreviousModel = PropertyModel != null;
            var previousColor = PropertyModel?.Color;
            var previousLineWidth = PropertyModel?.LineWidth ?? 1.5f;
            var previousMarkerSize = PropertyModel?.MarkerSize ?? 5f;
            var previousMarkerShape = PropertyModel?.MarkerShape ?? PlotMarkerShape.FilledCircle;

            // 删除时可能仍处于闪烁/半透明遮罩态，先恢复 BaseStyle，避免把临时色写入新 PropertyModel。
            foreach (var entry in _seriesEntries)
            {
                entry.Scatter.Color = entry.BaseStyle.Color;
                entry.Scatter.LineWidth = entry.BaseStyle.LineWidth;
                ApplyConfiguredMarkerStyle(entry.Scatter, entry.BaseStyle.Color, entry.BaseStyle.MarkerSize);
            }

            if (PropertyModel != null)
            {
                PropertyModel.PropertyChanged -= PropertyModel_PropertyChanged;
            }

            PropertyModel = new SpiderSamplePropertyModel(
                _seriesEntries.Select(entry => entry.Scatter),
                _legendProxy,
                _wpfPlot);
            PropertyModel.PropertyChanged += PropertyModel_PropertyChanged;

            if (hadPreviousModel && !string.IsNullOrWhiteSpace(previousColor))
            {
                PropertyModel.Color = previousColor;
                PropertyModel.LineWidth = previousLineWidth;
                PropertyModel.MarkerSize = previousMarkerSize;
                PropertyModel.MarkerShape = previousMarkerShape;
            }

            PropertyModel.SampleName = previousName;
            Tag = PropertyModel;
        }

        public void ResetActivePlottable()
        {
            _activeScatter = _seriesEntries.FirstOrDefault()?.Scatter;
            _hasExplicitActiveScatter = false;
            Plottable = _activeScatter;
        }

        public IReadOnlyList<int> GetSelectedRowIndices()
        {
            var activeRows = GetActiveSample()?.SourceRowIndices?
                .Where(index => index >= 0)
                .Distinct()
                .ToList();

            if (activeRows != null && activeRows.Count > 0)
            {
                return activeRows;
            }

            return _seriesEntries
                .SelectMany(entry => entry.Sample.SourceRowIndices)
                .Where(index => index >= 0)
                .Distinct()
                .ToList();
        }

        private SpiderSampleData? GetActiveSample()
        {
            if (_activeScatter != null)
            {
                var activeEntry = _seriesEntries.FirstOrDefault(entry => ReferenceEquals(entry.Scatter, _activeScatter));
                if (activeEntry != null)
                {
                    return activeEntry.Sample;
                }
            }

            return _seriesEntries.FirstOrDefault()?.Sample;
        }

        private void SpiderSampleLayerItemViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsVisible))
            {
                ApplyVisibility(IsVisible);
                return;
            }

            if (e.PropertyName == nameof(Name))
            {
                var sampleName = Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(sampleName))
                {
                    var fallbackName = PropertyModel != null && !string.IsNullOrWhiteSpace(PropertyModel.SampleName)
                        ? PropertyModel.SampleName
                        : (LanguageService.Instance["data_point"] ?? "Sample");
                    if (Name != fallbackName)
                    {
                        Name = fallbackName;
                    }

                    MessageHelper.Warning(LanguageService.Instance["geo_validate_name_required"]);
                    return;
                }

                if (PropertyModel != null && PropertyModel.SampleName != sampleName)
                {
                    PropertyModel.SampleName = sampleName;
                }
            }
        }

        private void ApplyVisibility(bool isVisible)
        {
            foreach (var entry in _seriesEntries)
            {
                entry.Scatter.IsVisible = isVisible;
            }

            if (_legendProxy != null)
            {
                _legendProxy.IsVisible = isVisible;
            }

            _wpfPlot?.Refresh();
        }

        private void RefreshBaseStyles()
        {
            foreach (var entry in _seriesEntries)
            {
                entry.BaseStyle.UpdateFrom(entry.Scatter);
            }
        }

        private PlotMarkerShape CurrentPlotMarkerShape =>
            PropertyModel?.MarkerShape ?? PlotMarkerShape.FilledCircle;

        /// <summary>
        /// 按属性面板配置应用标记形状（与图解数据点同一套 PlotMarkerShape）。
        /// </summary>
        private void ApplyConfiguredMarkerStyle(Scatter scatter, ScottPlot.Color markerColor, float? markerSize = null)
        {
            var shape = CurrentPlotMarkerShape;
            float strokeWidth = PlotMarkerStyleApplier.IsFilled(shape) ? 0f : 1.5f;
            PlotMarkerStyleApplier.Apply(
                scatter.MarkerStyle,
                shape,
                markerColor,
                strokeWidth,
                markerColor);
            scatter.MarkerSize = markerSize
                ?? PropertyModel?.MarkerSize
                ?? scatter.MarkerSize;
        }

        public void Highlight()
        {
            // 非数据模式：以分组整体显示红色（组内全部线条+顶点）
            foreach (var entry in _seriesEntries)
            {
                entry.Scatter.Color = ScottPlot.Colors.Red;
                entry.Scatter.LineWidth = entry.BaseStyle.LineWidth + 1;
                ApplyConfiguredMarkerStyle(
                    entry.Scatter,
                    ScottPlot.Colors.Red,
                    entry.BaseStyle.MarkerSize);
                if (PlotMarkerStyleApplier.IsFilled(CurrentPlotMarkerShape))
                {
                    entry.Scatter.MarkerStyle.OutlineColor = ScottPlot.Colors.Red;
                    entry.Scatter.MarkerStyle.OutlineWidth = Math.Max(2, entry.BaseStyle.MarkerOutlineWidth);
                }
            }

            _wpfPlot?.Refresh();
        }

        public void Dim()
        {
            foreach (var entry in _seriesEntries)
            {
                var dimColor = entry.BaseStyle.Color.WithAlpha(60);
                entry.Scatter.Color = dimColor;
                ApplyConfiguredMarkerStyle(entry.Scatter, dimColor, entry.BaseStyle.MarkerSize);
            }

            _wpfPlot?.Refresh();
        }

        /// <summary>
        /// 数据模式：组内仅保留指定样品线，其余线条施加与未选中相同的半透明遮罩。
        /// </summary>
        public void DimExcept(IPlottable? exceptPlottable)
        {
            foreach (var entry in _seriesEntries)
            {
                if (exceptPlottable != null && ReferenceEquals(entry.Scatter, exceptPlottable))
                {
                    continue;
                }

                var dimColor = entry.BaseStyle.Color.WithAlpha(60);
                entry.Scatter.Color = dimColor;
                ApplyConfiguredMarkerStyle(entry.Scatter, dimColor, entry.BaseStyle.MarkerSize);
            }
        }

        public void Restore()
        {
            foreach (var entry in _seriesEntries)
            {
                var color = entry.BaseStyle.Color;
                if (PropertyModel != null)
                {
                    try
                    {
                        color = ScottPlot.Color.FromHex(
                            GraphMapTemplateService.ConvertWpfHexToScottPlotHex(PropertyModel.Color));
                    }
                    catch
                    {
                        color = entry.BaseStyle.Color;
                    }
                }

                entry.Scatter.Color = color;
                entry.Scatter.LineWidth = PropertyModel?.LineWidth ?? entry.BaseStyle.LineWidth;
                ApplyConfiguredMarkerStyle(
                    entry.Scatter,
                    color,
                    PropertyModel?.MarkerSize ?? entry.BaseStyle.MarkerSize);
            }

            _wpfPlot?.Refresh();
        }

        /// <summary>
        /// 数据模式悬停预览：仅将命中线条临时标红，不改变当前选中/活动样品线。
        /// <paramref name="preserveFlashPlottable"/> 为当前选中闪烁线时不会被清回底色。
        /// <paramref name="dimOthers"/> 为 true 时，其余线条使用未选中半透明遮罩。
        /// </summary>
        public void ApplyHoverPreviewColor(
            IPlottable? plottable,
            ScottPlot.Color color,
            IPlottable? preserveFlashPlottable = null,
            bool dimOthers = false)
        {
            foreach (var entry in _seriesEntries)
            {
                if (ReferenceEquals(entry.Scatter, plottable))
                {
                    entry.Scatter.Color = color;
                    entry.Scatter.LineWidth = entry.BaseStyle.LineWidth;
                    ApplyConfiguredMarkerStyle(entry.Scatter, color, entry.BaseStyle.MarkerSize);
                    continue;
                }

                // 选中闪烁线由闪烁逻辑维护，悬停预览不得清掉
                if (preserveFlashPlottable != null
                    && ReferenceEquals(entry.Scatter, preserveFlashPlottable))
                {
                    continue;
                }

                var restoreColor = dimOthers
                    ? entry.BaseStyle.Color.WithAlpha(60)
                    : entry.BaseStyle.Color;

                entry.Scatter.Color = restoreColor;
                entry.Scatter.LineWidth = entry.BaseStyle.LineWidth;
                ApplyConfiguredMarkerStyle(entry.Scatter, restoreColor, entry.BaseStyle.MarkerSize);
            }
        }

        /// <summary>
        /// 数据模式闪烁：仅临时覆盖指定样品线（整条线+全部顶点），不改动同组其他线条。
        /// </summary>
        public void ApplyTemporaryFlashColor(ScottPlot.Color color, IPlottable? targetPlottable = null)
        {
            var targetScatter = targetPlottable as Scatter
                ?? _activeScatter
                ?? _seriesEntries.FirstOrDefault()?.Scatter;
            if (targetScatter == null)
            {
                return;
            }

            foreach (var entry in _seriesEntries)
            {
                if (!ReferenceEquals(entry.Scatter, targetScatter))
                {
                    continue;
                }

                entry.Scatter.Color = color;
                entry.Scatter.LineWidth = entry.BaseStyle.LineWidth;
                ApplyConfiguredMarkerStyle(entry.Scatter, color, entry.BaseStyle.MarkerSize);
                return;
            }
        }

        private void PropertyModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (PropertyModel == null) return;

            if (e.PropertyName == nameof(SpiderSamplePropertyModel.SampleName))
            {
                var sampleName = PropertyModel.SampleName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(sampleName))
                {
                    var fallbackName = string.IsNullOrWhiteSpace(Name)
                        ? (LanguageService.Instance["data_point"] ?? "Sample")
                        : Name;
                    if (PropertyModel.SampleName != fallbackName)
                    {
                        PropertyModel.SampleName = fallbackName;
                    }

                    MessageHelper.Warning(LanguageService.Instance["geo_validate_name_required"]);
                    return;
                }

                Name = sampleName;

                foreach (var entry in _seriesEntries)
                {
                    entry.Sample.Name = sampleName;
                }

                SampleNameChanged?.Invoke(this, sampleName);
            }

            RefreshBaseStyles();
        }
    }
}
