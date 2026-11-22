using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 检查器的「分组 + 条目」列表（<see cref="GeoChemistryNexus.Models.CalculationStep"/>）。
    /// 只有数据绑定：<see cref="ItemsSource"/> 给出步骤，<see cref="ToggleCommand"/> 在点击分组标题时以该步骤为参数执行。
    /// </summary>
    public partial class CalculationStepList : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(CalculationStepList), new PropertyMetadata(null));

        public static readonly DependencyProperty ToggleCommandProperty =
            DependencyProperty.Register(nameof(ToggleCommand), typeof(ICommand), typeof(CalculationStepList), new PropertyMetadata(null));

        /// <summary>要显示的步骤集合。</summary>
        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>点击分组标题时执行的命令，参数为被点击的步骤。</summary>
        public ICommand? ToggleCommand
        {
            get => (ICommand?)GetValue(ToggleCommandProperty);
            set => SetValue(ToggleCommandProperty, value);
        }

        public CalculationStepList()
        {
            InitializeComponent();
        }
    }
}
