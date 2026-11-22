using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Views;

public partial class EpmaCalculationView : UserControl
{
    private EpmaCalculationViewModel? _presentationViewModel;
    public EpmaCalculationView()
    {
        InitializeComponent();
        DataContextChanged += WorkbenchDataContextChanged;
        Loaded += (_, _) => UpdateWorkbenchLayout();
        IsVisibleChanged += (_, _) =>
        {
            // The workbench is initially collapsed behind the library. Wait for its
            // first visible measure before choosing the responsive arrangement.
            if (IsVisible) Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(UpdateWorkbenchLayout));
        };
    }

    private void WorkbenchDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_presentationViewModel is not null) _presentationViewModel.PropertyChanged -= PresentationChanged;
        _presentationViewModel = e.NewValue as EpmaCalculationViewModel;
        if (_presentationViewModel is not null) _presentationViewModel.PropertyChanged += PresentationChanged;
        UpdateWorkbenchLayout();
    }

    private void PresentationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EpmaCalculationViewModel.IsHelpVisible) or nameof(EpmaCalculationViewModel.HelpRtf)) ShowHelpDocument();
    }

    // The help document is plain rich text; loading it into the viewer is the only view-side work.
    private void ShowHelpDocument()
    {
        if (_presentationViewModel is not { IsHelpVisible: true } viewModel) return;
        HelpDocViewer.Document.Blocks.Clear();
        if (string.IsNullOrEmpty(viewModel.HelpRtf)) return;
        try { RtfHelper.LoadRtfString(HelpDocViewer, viewModel.HelpRtf); }
        catch { HelpDocViewer.Document.Blocks.Clear(); }
    }

    public void Initialize(EpmaCalculationViewModel viewModel)
    {
        DataContext = viewModel;
        viewModel.InitializeWorksheet(ScriptGrid);
    }

    private void WorkbenchSizeChanged(object sender, SizeChangedEventArgs e) => UpdateWorkbenchLayout();

    private void UpdateWorkbenchLayout()
    {
        if (ToolbarActions is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        bool wrapToolbar = ActualWidth < 1100;
        Grid.SetRow(ToolbarActions, wrapToolbar ? 1 : 0);
        Grid.SetColumn(ToolbarActions, wrapToolbar ? 0 : 2);
        Grid.SetColumnSpan(ToolbarActions, wrapToolbar ? 3 : 1);
        ToolbarActions.HorizontalAlignment = HorizontalAlignment.Right;
        ToolbarActions.Margin = wrapToolbar ? new Thickness(0, 8, 0, 0) : new Thickness(0);
    }
}
