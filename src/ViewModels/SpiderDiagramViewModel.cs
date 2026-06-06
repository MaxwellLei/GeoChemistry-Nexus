using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models.SpiderDiagram;
using ScottPlot;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SpiderDiagramViewModel : ObservableObject
    {
        private WpfPlot? _wpfPlot;

        /// <summary>
        /// 当前蛛网图类型：REE 或 TraceElement
        /// </summary>
        [ObservableProperty]
        private string _diagramType = "REE";

        /// <summary>
        /// 可用的标准化方案列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<NormalizationStandard> _availableStandards = new();

        /// <summary>
        /// 当前选中的标准化方案
        /// </summary>
        [ObservableProperty]
        private NormalizationStandard? _selectedStandard;

        /// <summary>
        /// 是否启用标准化（默认启用）。禁用时直接绘制原始浓度值。
        /// </summary>
        [ObservableProperty]
        private bool _isNormalizationEnabled = true;

        /// <summary>
        /// 当前元素顺序（仅包含已选中的元素）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _elementOrder = new();

        /// <summary>
        /// 所有可用元素（含选中状态和排序）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ElementItemViewModel> _allElements = new();

        /// <summary>
        /// 当前在元素列表中选中的项（用于上移下移操作）
        /// </summary>
        [ObservableProperty]
        private ElementItemViewModel? _selectedElementItem;

        /// <summary>
        /// 蛛网图是否处于绘图模式
        /// </summary>
        [ObservableProperty]
        private bool _isSpiderPlotMode = false;

        /// <summary>
        /// 样品数据：每行一组样品的元素值
        /// Key = 样品名, Value = { 元素符号 -> 浓度值(ppm) }
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<SpiderSampleData> _samples = new();

        /// <summary>
        /// 标题
        /// </summary>
        public string Title => DiagramType == "REE"
            ? "REE Spider Diagram"
            : "Multi-Element Spider Diagram";

        /// <summary>
        /// 当前所选标准化方案的参考值标题
        /// </summary>
        public string SelectedStandardDisplayTitle => SelectedStandard == null
            ? "标准化参考值"
            : $"{SelectedStandard.ShortName} 参考值 (ppm)";

        /// <summary>
        /// 当前所选标准化方案的参考值列表，按该图类型的默认顺序展示全部参考值
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, double>> SelectedStandardDisplayValues
        {
            get
            {
                if (SelectedStandard == null)
                {
                    return Array.Empty<KeyValuePair<string, double>>();
                }

                var defaultOrder = DiagramType == "REE"
                    ? NormalizationData.ReeElementOrder
                    : NormalizationData.TraceElementOrder;

                var orderedValues = defaultOrder
                    .Where(element => SelectedStandard.Values.ContainsKey(element))
                    .Select(element => new KeyValuePair<string, double>(element, SelectedStandard.Values[element]))
                    .ToList();

                foreach (var entry in SelectedStandard.Values)
                {
                    if (!orderedValues.Any(item => item.Key == entry.Key))
                    {
                        orderedValues.Add(new KeyValuePair<string, double>(entry.Key, entry.Value));
                    }
                }

                return orderedValues;
            }
        }

        /// <summary>
        /// 是否存在可展示的标准化参考值
        /// </summary>
        public bool HasSelectedStandardDisplayValues => SelectedStandardDisplayValues.Count > 0;

        /// <summary>
        /// 元素排序/选择变更事件，用于通知外部（如 MainPlotViewModel）刷新数据表格
        /// </summary>
        public event Action? ElementOrderChanged;

        /// <summary>
        /// 蛛网图配置变化事件，用于通知外部通过模板系统重绘
        /// </summary>
        public event Action? PlotSettingsChanged;

        /// <summary>
        /// 预定义颜色序列
        /// </summary>
        private static readonly string[] PlotColors = new[]
        {
            "#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd",
            "#8c564b", "#e377c2", "#7f7f7f", "#bcbd22", "#17becf",
            "#aec7e8", "#ffbb78", "#98df8a", "#ff9896", "#c5b0d5"
        };

        // 防止递归更新
        private bool _isUpdatingElementOrder = false;

        public SpiderDiagramViewModel()
        {
        }

        /// <summary>
        /// 初始化为指定类型的蛛网图
        /// </summary>
        public void Initialize(string diagramType, WpfPlot? wpfPlot = null)
        {
            DiagramType = diagramType;
            _wpfPlot = wpfPlot;
            Samples.Clear();
            IsNormalizationEnabled = true;

            // 加载对应的标准化方案
            var standards = diagramType == "REE"
                ? NormalizationData.GetReeStandards()
                : NormalizationData.GetTraceElementStandards();

            AvailableStandards = new ObservableCollection<NormalizationStandard>(standards);

            // 默认选中第一个
            SelectedStandard = AvailableStandards.FirstOrDefault();

            // 设置默认元素顺序
            var defaultOrder = diagramType == "REE"
                ? NormalizationData.ReeElementOrder
                : NormalizationData.TraceElementOrder;

            // 初始化所有元素列表（全部默认选中）
            InitializeAllElements(defaultOrder);

            OnPropertyChanged(nameof(Title));
        }

        /// <summary>
        /// 初始化所有元素列表
        /// </summary>
        private void InitializeAllElements(List<string> defaultOrder)
        {
            // 取消旧元素的事件订阅
            foreach (var item in AllElements)
            {
                item.PropertyChanged -= ElementItem_PropertyChanged;
            }

            AllElements.Clear();
            foreach (var element in defaultOrder)
            {
                var item = new ElementItemViewModel(element, true);
                item.PropertyChanged += ElementItem_PropertyChanged;
                AllElements.Add(item);
            }

            // 同步更新 ElementOrder
            SyncElementOrder();
        }

        /// <summary>
        /// 当单个元素的选中状态改变时触发
        /// </summary>
        private void ElementItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ElementItemViewModel.IsSelected))
            {
                SyncElementOrder();
            }
        }

        /// <summary>
        /// 从 AllElements 同步计算 ElementOrder（仅包含选中的元素）
        /// </summary>
        private void SyncElementOrder()
        {
            if (_isUpdatingElementOrder) return;
            _isUpdatingElementOrder = true;

            try
            {
                var selectedElements = AllElements
                    .Where(e => e.IsSelected)
                    .Select(e => e.Name)
                    .ToList();

                ElementOrder = new ObservableCollection<string>(selectedElements);
            }
            finally
            {
                _isUpdatingElementOrder = false;
            }
        }

        /// <summary>
        /// 上移元素
        /// </summary>
        [RelayCommand]
        private void MoveElementUp(ElementItemViewModel? item)
        {
            if (item == null) return;
            int index = AllElements.IndexOf(item);
            if (index <= 0) return;

            AllElements.Move(index, index - 1);
            SyncElementOrder();
            SelectedElementItem = item;
            ElementOrderChanged?.Invoke();
        }

        /// <summary>
        /// 下移元素
        /// </summary>
        [RelayCommand]
        private void MoveElementDown(ElementItemViewModel? item)
        {
            if (item == null) return;
            int index = AllElements.IndexOf(item);
            if (index < 0 || index >= AllElements.Count - 1) return;

            AllElements.Move(index, index + 1);
            SyncElementOrder();
            SelectedElementItem = item;
            ElementOrderChanged?.Invoke();
        }

        /// <summary>
        /// 全选元素
        /// </summary>
        [RelayCommand]
        private void SelectAllElements()
        {
            _isUpdatingElementOrder = true;
            try
            {
                foreach (var item in AllElements)
                {
                    item.IsSelected = true;
                }
            }
            finally
            {
                _isUpdatingElementOrder = false;
            }
            SyncElementOrder();
            ElementOrderChanged?.Invoke();
        }

        /// <summary>
        /// 取消全选
        /// </summary>
        [RelayCommand]
        private void DeselectAllElements()
        {
            _isUpdatingElementOrder = true;
            try
            {
                foreach (var item in AllElements)
                {
                    item.IsSelected = false;
                }
            }
            finally
            {
                _isUpdatingElementOrder = false;
            }
            SyncElementOrder();
            ElementOrderChanged?.Invoke();
        }

        /// <summary>
        /// 重置为默认顺序和选择
        /// </summary>
        [RelayCommand]
        private void ResetElementOrder()
        {
            var defaultOrder = DiagramType == "REE"
                ? NormalizationData.ReeElementOrder
                : NormalizationData.TraceElementOrder;

            InitializeAllElements(defaultOrder);
            ElementOrderChanged?.Invoke();
        }

        /// <summary>
        /// 设置 WpfPlot 控件引用
        /// </summary>
        public void SetPlotControl(WpfPlot wpfPlot)
        {
            _wpfPlot = wpfPlot;
        }

        partial void OnSelectedStandardChanged(NormalizationStandard? value)
        {
            RefreshSelectedStandardDisplay();

            if (Samples.Count == 0) return;

            // 模板化模式下，由外部统一负责重绘，避免重复叠加渲染
            if (IsSpiderPlotMode)
            {
                PlotSettingsChanged?.Invoke();
                return;
            }

            // 兼容旧模式
            if (value != null && _wpfPlot != null)
            {
                RenderSpiderDiagram();
            }
        }

        partial void OnIsNormalizationEnabledChanged(bool value)
        {
            if (Samples.Count == 0) return;

            // 模板化模式下，由外部统一负责重绘，避免重复叠加渲染
            if (IsSpiderPlotMode)
            {
                PlotSettingsChanged?.Invoke();
                return;
            }

            // 兼容旧模式
            if (_wpfPlot != null)
            {
                RenderSpiderDiagram();
            }
        }

        private void RefreshSelectedStandardDisplay()
        {
            OnPropertyChanged(nameof(SelectedStandardDisplayTitle));
            OnPropertyChanged(nameof(SelectedStandardDisplayValues));
            OnPropertyChanged(nameof(HasSelectedStandardDisplayValues));
        }

        /// <summary>
        /// 直接加载已解析的样品数据
        /// </summary>
        public void LoadSamples(IEnumerable<SpiderSampleData> samples)
        {
            Samples.Clear();

            if (samples != null)
            {
                foreach (var sample in samples)
                {
                    if (sample != null)
                    {
                        Samples.Add(sample);
                    }
                }
            }

            NotifySamplesLoaded();
        }

        /// <summary>
        /// 从数据表加载样品数据
        /// </summary>
        /// <param name="dataRows">每行数据 { 列名 -> 值 }</param>
        /// <param name="sampleNameColumn">样品名列</param>
        public void LoadSamplesFromData(
            List<Dictionary<string, string>> dataRows,
            string sampleNameColumn = "Sample",
            IReadOnlyList<int>? sourceRowIndices = null)
        {
            var parsedSamples = new List<SpiderSampleData>(dataRows?.Count ?? 0);

            for (int rowIndex = 0; rowIndex < dataRows.Count; rowIndex++)
            {
                var row = dataRows[rowIndex];
                string sampleName = row.ContainsKey(sampleNameColumn) ? row[sampleNameColumn] : $"Sample {rowIndex + 1}";

                var values = new Dictionary<string, double>(ElementOrder.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var element in ElementOrder)
                {
                    if (row.TryGetValue(element, out string? textValue) && double.TryParse(textValue, out double val))
                    {
                        values[element] = val;
                    }
                }

                parsedSamples.Add(new SpiderSampleData
                {
                    Name = sampleName,
                    ElementValues = values,
                    SourceRowIndices = sourceRowIndices != null && rowIndex < sourceRowIndices.Count
                        ? new List<int> { sourceRowIndices[rowIndex] }
                        : new List<int>()
                });
            }

            LoadSamples(parsedSamples);
        }

        private void NotifySamplesLoaded()
        {
            if (Samples.Count == 0) return;

            // 模板化模式下，由外部统一负责重绘，避免重复叠加渲染
            if (IsSpiderPlotMode)
            {
                PlotSettingsChanged?.Invoke();
                return;
            }

            // 兼容旧模式
            if (_wpfPlot != null)
            {
                RenderSpiderDiagram();
            }
        }

        /// <summary>
        /// 渲染蛛网图
        /// </summary>
        [RelayCommand]
        public void RenderSpiderDiagram()
        {
            if (_wpfPlot == null || Samples.Count == 0)
                return;

            // 启用标准化时必须有方案；禁用标准化时则不依赖方案
            if (IsNormalizationEnabled && SelectedStandard == null)
                return;

            var plot = _wpfPlot.Plot;
            plot.Clear();

            // 设置标题
            plot.Axes.Title.Label.Text = Title;

            // 获取有效的元素列表：启用标准化时按方案中存在参考值过滤，禁用时使用全部已选元素
            var validElements = IsNormalizationEnabled
                ? ElementOrder.Where(e => SelectedStandard!.Values.ContainsKey(e)).ToList()
                : ElementOrder.ToList();

            if (validElements.Count == 0) return;

            // X 轴位置（1-based index）
            double[] xPositions = Enumerable.Range(1, validElements.Count).Select(i => (double)i).ToArray();

            // 绘制每个样品
            for (int sampleIdx = 0; sampleIdx < Samples.Count; sampleIdx++)
            {
                var sample = Samples[sampleIdx];
                var normalizedValues = new List<double>();
                var xValues = new List<double>();

                for (int i = 0; i < validElements.Count; i++)
                {
                    string element = validElements[i];
                    if (sample.ElementValues.ContainsKey(element))
                    {
                        double rawValue = sample.ElementValues[element];

                        if (IsNormalizationEnabled)
                        {
                            double refValue = SelectedStandard!.Values[element];
                            if (refValue > 0 && rawValue > 0)
                            {
                                // 对数轴：存储 Log10 值用于绘制
                                normalizedValues.Add(Math.Log10(rawValue / refValue));
                                xValues.Add(xPositions[i]);
                            }
                        }
                        else
                        {
                            if (rawValue > 0)
                            {
                                // 未标准化：直接使用原始浓度的对数值
                                normalizedValues.Add(Math.Log10(rawValue));
                                xValues.Add(xPositions[i]);
                            }
                        }
                    }
                }

                if (xValues.Count > 0)
                {
                    var color = ScottPlot.Color.FromHex(PlotColors[sampleIdx % PlotColors.Length]);

                    // 绘制连线 + 标记（主对象不参与图例）
                    var scatter = plot.Add.Scatter(xValues.ToArray(), normalizedValues.ToArray());
                    scatter.LineWidth = 1.5f;
                    scatter.Color = color;
                    scatter.MarkerSize = 5;
                    scatter.LegendText = string.Empty; // 主对象不显示图例

                    // 创建图例代理（空数据，仅用于图例显示，固定大小）
                    var legendProxy = plot.Add.ScatterPoints(new Coordinates[] { });
                    legendProxy.LegendText = sample.Name;
                    legendProxy.Color = color;
                    legendProxy.MarkerSize = 8; // 图例中固定大小
                    legendProxy.LineWidth = 1.5f;
                    legendProxy.MarkerShape = scatter.MarkerShape;
                }
            }

            // 配置 Y 轴为对数坐标刻度
            plot.Axes.Left.Label.Text = IsNormalizationEnabled
                ? $"Sample / {SelectedStandard!.ShortName}"
                : "Concentration (ppm)";

            // 对数轴刻度生成器：显示 10^n 形式的标签
            var tickGen = new ScottPlot.TickGenerators.NumericAutomatic();
            tickGen.MinorTickGenerator = new ScottPlot.TickGenerators.LogMinorTickGenerator();
            tickGen.IntegerTicksOnly = true;
            tickGen.LabelFormatter = y =>
            {
                double val = Math.Pow(10, y);
                return val.ToString("G10");
            };
            plot.Axes.Left.TickGenerator = tickGen;

            // 设置 X 轴自定义刻度标签
            double[] tickPositions = xPositions;
            string[] tickLabels = validElements.ToArray();

            var customTicks = new ScottPlot.TickGenerators.NumericManual();
            for (int i = 0; i < tickPositions.Length; i++)
            {
                customTicks.AddMajor(tickPositions[i], tickLabels[i]);
            }
            plot.Axes.Bottom.TickGenerator = customTicks;

            // 设置轴限（Log10 空间：-2 = 0.01, 4 = 10000）
            plot.Axes.SetLimits(
                left: 0.5,
                right: validElements.Count + 0.5,
                bottom: -2,
                top: 4
            );

            // 启用图例
            plot.Legend.IsVisible = true;

            // 刷新
            _wpfPlot.Refresh();
        }

        /// <summary>
        /// 返回到模板浏览模式
        /// </summary>
        [RelayCommand]
        private void BackToTemplateMode()
        {
            IsSpiderPlotMode = false;
        }

        /// <summary>
        /// 导出图表为图片
        /// </summary>
        [RelayCommand]
        private void ExportImage()
        {
            if (_wpfPlot == null) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = FileDialogFilterHelper.PngSvg,
                DefaultExt = ".png",
                FileName = $"{DiagramType}_SpiderDiagram"
            };

            if (dialog.ShowDialog() == true)
            {
                if (dialog.FileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    _wpfPlot.Plot.SaveSvg(dialog.FileName, 800, 600);
                }
                else
                {
                    _wpfPlot.Plot.SavePng(dialog.FileName, 800, 600);
                }
            }
        }

        /// <summary>
        /// 复制图表到剪贴板
        /// </summary>
        [RelayCommand]
        private void CopyToClipboard()
        {
            if (_wpfPlot == null) return;

            try
            {
                // 保存临时文件并复制到剪贴板
                string tempPath = System.IO.Path.GetTempFileName() + ".png";
                _wpfPlot.Plot.SavePng(tempPath, 800, 600);

                var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(tempPath));
                Clipboard.SetImage(bitmap);

                System.IO.File.Delete(tempPath);
            }
            catch
            {
                // ignore
            }
        }
    }

    /// <summary>
    /// 蛛网图样品数据
    /// </summary>
    public class SpiderSampleData
    {
        /// <summary>
        /// 样品名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 元素浓度值 (元素符号 -> ppm)
        /// </summary>
        public Dictionary<string, double> ElementValues { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// 对应源数据表中的行号
        /// </summary>
        public List<int> SourceRowIndices { get; set; } = new List<int>();
    }
}
