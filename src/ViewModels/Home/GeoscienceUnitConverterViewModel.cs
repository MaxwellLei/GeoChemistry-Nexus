using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.ViewModels
{
    public sealed class GeoCategoryTabItem : ObservableObject
    {
        public GeoUnitCategory Category { get; init; }
        public string TitleKey { get; init; } = string.Empty;
        public string DefaultTitle { get; init; } = string.Empty;
        public string Icon { get; init; } = string.Empty;

        public string Title => LanguageService.Instance[TitleKey] is { Length: > 0 } t ? t : DefaultTitle;

        public void RefreshLanguage() => OnPropertyChanged(nameof(Title));
    }

    public sealed partial class GeoUnitResultItemViewModel : ObservableObject
    {
        public GeoUnitDefinition Unit { get; }
        private readonly Action<GeoUnitDefinition> _onSetAsBase;

        [ObservableProperty]
        private double rawValue;

        [ObservableProperty]
        private string formattedValue = "0";

        [ObservableProperty]
        private bool isBaseUnit;

        public string Symbol => Unit.Symbol;
        public string Name => Unit.Name;
        public string Description => Unit.Description;

        public GeoUnitResultItemViewModel(GeoUnitDefinition unit, Action<GeoUnitDefinition> onSetAsBase)
        {
            Unit = unit;
            _onSetAsBase = onSetAsBase;
        }

        public void Update(double value, bool isBase)
        {
            RawValue = value;
            FormattedValue = GeoscienceUnitConverterService.FormatSmart(value);
            IsBaseUnit = isBase;
        }

        public void RefreshLanguage()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
        }

        [RelayCommand]
        private void Copy()
        {
            try
            {
                // 复制时尽量保留完整高精度有效数字
                string copyText = Math.Abs(RawValue) < 1e-4 || Math.Abs(RawValue) >= 1e7
                    ? RawValue.ToString("G8", CultureInfo.InvariantCulture)
                    : RawValue.ToString("0.########", CultureInfo.InvariantCulture);

                Clipboard.SetText(copyText);

                string template = LanguageService.Instance["geoscience_unit_conv_copied_val"];
                if (string.IsNullOrEmpty(template)) template = "已复制 {0} {1}";
                MessageHelper.Success(string.Format(template, copyText, Unit.Symbol));
            }
            catch (Exception ex)
            {
                string failMsg = LanguageService.Instance["copy_failed"];
                if (string.IsNullOrEmpty(failMsg)) failMsg = "复制失败: ";
                MessageHelper.Warning(failMsg + ex.Message);
            }
        }

        [RelayCommand]
        private void SetAsBase()
        {
            _onSetAsBase?.Invoke(Unit);
        }
    }

    public partial class GeoscienceUnitConverterViewModel : ObservableObject
    {
        // ────────────────── 分类选项卡 ──────────────────
        public ObservableCollection<GeoCategoryTabItem> Categories { get; } = new();

        [ObservableProperty]
        private GeoCategoryTabItem? selectedCategoryItem;

        partial void OnSelectedCategoryItemChanged(GeoCategoryTabItem? value)
        {
            if (value == null) return;
            LoadCategory(value.Category);
        }

        // ────────────────── 单位与预设 ──────────────────
        public ObservableCollection<GeoUnitDefinition> CurrentUnits { get; } = new();

        public ObservableCollection<GeoPresetItem> CurrentPresets { get; } = new();

        [ObservableProperty]
        private GeoUnitDefinition? selectedSourceUnit;

        partial void OnSelectedSourceUnitChanged(GeoUnitDefinition? value)
        {
            Recalculate();
        }

        // ────────────────── 输入数值 ──────────────────
        [ObservableProperty]
        private double inputValue = 1.0;

        partial void OnInputValueChanged(double value)
        {
            Recalculate();
        }

        // ────────────────── 联动结果看板 ──────────────────
        public ObservableCollection<GeoUnitResultItemViewModel> Results { get; } = new();

        public GeoscienceUnitConverterViewModel()
        {
            LanguageService.Instance.PropertyChanged += OnAppLanguageChanged;
            InitCategories();
            SelectedCategoryItem = Categories.FirstOrDefault();
        }

        private void OnAppLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
            {
                foreach (var cat in Categories)
                {
                    cat.RefreshLanguage();
                }

                foreach (var r in Results)
                {
                    r.RefreshLanguage();
                }

                if (SelectedCategoryItem != null)
                {
                    LoadPresets(SelectedCategoryItem.Category);

                    var currentSourceId = SelectedSourceUnit?.Id;
                    CurrentUnits.Clear();
                    foreach (var u in GeoscienceUnitConverterService.GetUnits(SelectedCategoryItem.Category))
                    {
                        CurrentUnits.Add(u);
                    }
                    SelectedSourceUnit = CurrentUnits.FirstOrDefault(u => u.Id == currentSourceId) ?? CurrentUnits.FirstOrDefault();
                }
            }
        }

        private void InitCategories()
        {
            Categories.Clear();
            Categories.Add(new GeoCategoryTabItem
            {
                Category = GeoUnitCategory.Grade,
                TitleKey = "geoscience_unit_conv_cat_grade",
                DefaultTitle = "品位 / 浓度",
                Icon = "\ue9e9"
            });
            Categories.Add(new GeoCategoryTabItem
            {
                Category = GeoUnitCategory.Pressure,
                TitleKey = "geoscience_unit_conv_cat_pressure",
                DefaultTitle = "压力 / 应力",
                Icon = "\ue819"
            });
            Categories.Add(new GeoCategoryTabItem
            {
                Category = GeoUnitCategory.Scale,
                TitleKey = "geoscience_unit_conv_cat_scale",
                DefaultTitle = "微区尺度",
                Icon = "\ue722"
            });
            Categories.Add(new GeoCategoryTabItem
            {
                Category = GeoUnitCategory.Temperature,
                TitleKey = "geoscience_unit_conv_cat_temperature",
                DefaultTitle = "温度 / 地温",
                Icon = "\ue706"
            });
            Categories.Add(new GeoCategoryTabItem
            {
                Category = GeoUnitCategory.Time,
                TitleKey = "geoscience_unit_conv_cat_time",
                DefaultTitle = "同位素年代",
                Icon = "\ue823"
            });
        }

        private void LoadPresets(GeoUnitCategory category)
        {
            var presets = GeoscienceUnitConverterService.GetPresets(category);
            CurrentPresets.Clear();
            foreach (var p in presets)
            {
                CurrentPresets.Add(p);
            }
        }

        private void LoadCategory(GeoUnitCategory category)
        {
            var units = GeoscienceUnitConverterService.GetUnits(category);
            CurrentUnits.Clear();
            foreach (var u in units)
            {
                CurrentUnits.Add(u);
            }

            LoadPresets(category);

            // 初始化看板行
            Results.Clear();
            foreach (var u in units)
            {
                Results.Add(new GeoUnitResultItemViewModel(u, OnSetAsBaseUnit));
            }

            // 默认选中第一个单位
            SelectedSourceUnit = CurrentUnits.FirstOrDefault();
            InputValue = GetDefaultValueForCategory(category);
            Recalculate();
        }

        private double GetDefaultValueForCategory(GeoUnitCategory category) => category switch
        {
            GeoUnitCategory.Grade => 1.0,
            GeoUnitCategory.Pressure => 1.0,
            GeoUnitCategory.Scale => 1.0,
            GeoUnitCategory.Temperature => 25.0,
            GeoUnitCategory.Time => 1.0,
            _ => 1.0
        };

        private void OnSetAsBaseUnit(GeoUnitDefinition unit)
        {
            if (unit == null) return;
            // 将当前该单位对应的换算数值设置为新的基准输入数值
            if (SelectedSourceUnit != null && Results.Any(r => r.Unit.Id == unit.Id))
            {
                var targetRow = Results.First(r => r.Unit.Id == unit.Id);
                double targetVal = targetRow.RawValue;
                SelectedSourceUnit = unit;
                InputValue = targetVal;
            }
            else
            {
                SelectedSourceUnit = unit;
            }
        }

        private void Recalculate()
        {
            if (SelectedSourceUnit == null || Results.Count == 0) return;

            foreach (var item in Results)
            {
                bool isBase = item.Unit.Id == SelectedSourceUnit.Id;
                double convertedVal = GeoscienceUnitConverterService.Convert(InputValue, SelectedSourceUnit, item.Unit);
                item.Update(convertedVal, isBase);
            }
        }

        [RelayCommand]
        private void ApplyPreset(GeoPresetItem? preset)
        {
            if (preset == null) return;

            // 匹配预设单位
            var matchUnit = CurrentUnits.FirstOrDefault(u => u.Symbol.Equals(preset.UnitSymbol, StringComparison.OrdinalIgnoreCase))
                         ?? CurrentUnits.FirstOrDefault();

            if (matchUnit != null)
            {
                SelectedSourceUnit = matchUnit;
            }

            InputValue = preset.Value;
            Recalculate();
        }

        [RelayCommand]
        private void Reset()
        {
            if (SelectedCategoryItem != null)
            {
                SelectedSourceUnit = CurrentUnits.FirstOrDefault();
                InputValue = GetDefaultValueForCategory(SelectedCategoryItem.Category);
                Recalculate();
            }
        }
    }
}
