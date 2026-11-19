using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoChemistryNexus.Views;

public partial class EpmaEmptyState : UserControl
{
    public EpmaEmptyState() => InitializeComponent();

    /// <summary>「新建」按钮命令（由宿主视图提供，因新建需要弹出编辑窗口）</summary>
    public static readonly DependencyProperty CreateCommandProperty =
        DependencyProperty.Register(nameof(CreateCommand), typeof(ICommand), typeof(EpmaEmptyState), new PropertyMetadata(null));

    public ICommand? CreateCommand
    {
        get => (ICommand?)GetValue(CreateCommandProperty);
        set => SetValue(CreateCommandProperty, value);
    }
}
