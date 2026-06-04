using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models.SpiderDiagram;
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

        public void Highlight()
        {
            foreach (var entry in _seriesEntries)
            {
                bool isTarget = !_hasExplicitActiveScatter || ReferenceEquals(entry.Scatter, _activeScatter);
                if (isTarget)
                {
                    entry.Scatter.Color = ScottPlot.Colors.Red;
                    entry.Scatter.LineWidth = entry.BaseStyle.LineWidth + 1;
                    entry.Scatter.MarkerSize = entry.BaseStyle.MarkerSize;
                    entry.Scatter.MarkerShape = entry.BaseStyle.MarkerShape;
                    entry.Scatter.MarkerStyle.OutlineColor = ScottPlot.Colors.Red;
                    entry.Scatter.MarkerStyle.OutlineWidth = 2;
                }
                else
                {
                    entry.Scatter.Color = entry.BaseStyle.Color.WithAlpha(60);
                    entry.Scatter.LineWidth = entry.BaseStyle.LineWidth;
                    entry.Scatter.MarkerSize = entry.BaseStyle.MarkerSize;
                    entry.Scatter.MarkerShape = entry.BaseStyle.MarkerShape;
                    entry.Scatter.MarkerStyle.OutlineColor = entry.BaseStyle.MarkerOutlineColor.WithAlpha(60);
                    entry.Scatter.MarkerStyle.OutlineWidth = entry.BaseStyle.MarkerOutlineWidth;
                }
            }

            _wpfPlot?.Refresh();
        }

        public void Dim()
        {
            foreach (var entry in _seriesEntries)
            {
                entry.Scatter.Color = entry.BaseStyle.Color.WithAlpha(60);
                entry.Scatter.MarkerStyle.OutlineColor = entry.BaseStyle.MarkerOutlineColor.WithAlpha(60);
                entry.Scatter.MarkerStyle.OutlineWidth = entry.BaseStyle.MarkerOutlineWidth;
            }

            _wpfPlot?.Refresh();
        }

        public void Restore()
        {
            foreach (var entry in _seriesEntries)
            {
                entry.Scatter.Color = entry.BaseStyle.Color;
                entry.Scatter.LineWidth = entry.BaseStyle.LineWidth;
                entry.Scatter.MarkerSize = entry.BaseStyle.MarkerSize;
                entry.Scatter.MarkerShape = entry.BaseStyle.MarkerShape;
                entry.Scatter.MarkerStyle.OutlineWidth = entry.BaseStyle.MarkerOutlineWidth;
                entry.Scatter.MarkerStyle.OutlineColor = entry.BaseStyle.MarkerOutlineColor;
            }

            _wpfPlot?.Refresh();
        }

        private void PropertyModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (PropertyModel == null) return;

            if (e.PropertyName == nameof(SpiderSamplePropertyModel.SampleName))
            {
                var sampleName = PropertyModel.SampleName;
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
