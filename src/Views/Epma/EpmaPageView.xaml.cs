using System.Windows.Controls;
using GeoChemistryNexus.ViewModels;
namespace GeoChemistryNexus.Views;
public partial class EpmaPageView : Page
{
    private static EpmaPageView? _instance;
    public static EpmaPageView GetPage() => _instance ??= new EpmaPageView();
    public EpmaPageView()
    {
        InitializeComponent();
        var viewModel = new EpmaPageViewModel(); DataContext = viewModel;
        WorkbenchView.Initialize(viewModel.Workbench);
        Unloaded += (_, _) => viewModel.LeavePage();
    }
    /// <summary>Forwards the once-per-launch automatic update check to the view model.</summary>
    public System.Threading.Tasks.Task CheckUpdatesIfNeededAsync()
        => (DataContext as EpmaPageViewModel)?.CheckUpdatesIfNeededAsync() ?? System.Threading.Tasks.Task.CompletedTask;
}
