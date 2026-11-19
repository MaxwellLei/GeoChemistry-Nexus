using System.Windows;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Views;

public partial class EpmaLocalizationWindow : Window
{
    public EpmaLocalizationWindow(EpmaLocalizationSheetViewModel sheet)
    {
        InitializeComponent();
        DataContext = sheet;
        sheet.HostWindow = this;
        GeoChemistryNexus.Helpers.UiScaleHelper.Attach(this);
        GeoChemistryNexus.Helpers.WindowWorkAreaHelper.Attach(this);
        sheet.CloseRequested += confirmed => DialogResult = confirmed;
    }

    // Custom title bar buttons only change the window state, nothing else.
    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
}
