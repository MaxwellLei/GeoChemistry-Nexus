using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 数据系列的一行：列名 + 单位。
    /// </summary>
    public partial class DataSeriesRow : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _unit = string.Empty;
    }

    /// <summary>
    /// 脚本“数据系列”表格编辑器：把以英文逗号分隔的列名串 / 单位串展示为“# / 列名 / 单位”表格，
    /// 增删改后再写回两个字符串（与脚本定义里的 RequiredDataSeries、DataSeriesUnits 一一对应）。
    /// </summary>
    public partial class InspectorDataSeriesEditor : UserControl
    {
        private bool _isSyncing;

        public static readonly DependencyProperty NamesProperty =
            DependencyProperty.Register(nameof(Names), typeof(string), typeof(InspectorDataSeriesEditor),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSourceChanged));

        public static readonly DependencyProperty UnitsProperty =
            DependencyProperty.Register(nameof(Units), typeof(string), typeof(InspectorDataSeriesEditor),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSourceChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(InspectorDataSeriesEditor),
                new PropertyMetadata(false));

        /// <summary>以英文逗号分隔的列名（如 SiO2,Na2O,K2O）。</summary>
        public string Names
        {
            get => (string)GetValue(NamesProperty);
            set => SetValue(NamesProperty, value);
        }

        /// <summary>以英文逗号分隔的单位（与列名按位置对应）。</summary>
        public string Units
        {
            get => (string)GetValue(UnitsProperty);
            set => SetValue(UnitsProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public ObservableCollection<DataSeriesRow> Rows { get; } = new ObservableCollection<DataSeriesRow>();

        public InspectorDataSeriesEditor()
        {
            InitializeComponent();
            Rows.CollectionChanged += Rows_CollectionChanged;
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InspectorDataSeriesEditor editor && !editor._isSyncing)
            {
                editor.RebuildRows();
            }
        }

        /// <summary>由外部字符串重新生成表格行。</summary>
        private void RebuildRows()
        {
            _isSyncing = true;
            try
            {
                Rows.Clear();
                string names = Names ?? string.Empty;
                if (string.IsNullOrWhiteSpace(names))
                {
                    return;
                }

                string[] nameParts = names.Split(',');
                string[] unitParts = (Units ?? string.Empty).Split(',');
                for (int i = 0; i < nameParts.Length; i++)
                {
                    Rows.Add(new DataSeriesRow
                    {
                        Name = nameParts[i].Trim(),
                        Unit = i < unitParts.Length ? unitParts[i].Trim() : string.Empty
                    });
                }
            }
            finally
            {
                _isSyncing = false;
            }
            SubscribeRows();
        }

        private void SubscribeRows()
        {
            foreach (var row in Rows)
            {
                row.PropertyChanged -= Row_PropertyChanged;
                row.PropertyChanged += Row_PropertyChanged;
            }
        }

        private void Rows_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (DataSeriesRow row in e.OldItems)
                    row.PropertyChanged -= Row_PropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (DataSeriesRow row in e.NewItems)
                    row.PropertyChanged += Row_PropertyChanged;
            }

            if (!_isSyncing)
            {
                WriteBack();
            }
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_isSyncing)
            {
                WriteBack();
            }
        }

        /// <summary>把表格内容写回列名串 / 单位串。</summary>
        private void WriteBack()
        {
            string names = string.Join(",", Rows.Select(r => (r.Name ?? string.Empty).Replace(",", string.Empty).Trim()));
            bool anyUnit = Rows.Any(r => !string.IsNullOrWhiteSpace(r.Unit));
            string units = anyUnit
                ? string.Join(",", Rows.Select(r => (r.Unit ?? string.Empty).Replace(",", string.Empty).Trim()))
                : string.Empty;

            _isSyncing = true;
            try
            {
                SetCurrentValue(NamesProperty, names);
                SetCurrentValue(UnitsProperty, units);
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Rows.Add(new DataSeriesRow());
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is DataSeriesRow row)
            {
                Rows.Remove(row);
            }
        }
    }
}
