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
    private double _detailHeight = 220;
    private bool _expanded;
    private EpmaCalculationViewModel? _presentationViewModel;
    public EpmaCalculationView()
    {
        InitializeComponent();
        DataContextChanged += WorkbenchDataContextChanged;
        Loaded += (_, _) => UpdateWorkbenchLayout();
        SettingsGrid.SizeChanged += WorkbenchSizeChanged;
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
        SetDetailsExpanded(_presentationViewModel?.IsDiagnosticsExpanded == true);
        UpdateWorkbenchLayout();
    }

    private void PresentationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EpmaCalculationViewModel.IsDiagnosticsExpanded))
            SetDetailsExpanded(_presentationViewModel?.IsDiagnosticsExpanded == true, true);
        if (e.PropertyName == nameof(EpmaCalculationViewModel.HasIronParameters)) UpdateWorkbenchLayout();
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

    private void SetDetailsExpanded(bool expanded, bool rememberHeight = false)
    {
        if (rememberHeight && _expanded && DetailsRow.ActualHeight >= 80)
            _detailHeight = DetailsRow.ActualHeight;
        _expanded = expanded;
        DetailsRow.MinHeight = expanded ? 120 : 0;
        DetailsRow.Height = expanded ? new GridLength(_detailHeight) : GridLength.Auto;
    }

    private void DiagnosticSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is EpmaDiagnosticInfo diagnostic
            && DataContext is EpmaCalculationViewModel vm)
            vm.FocusDiagnostic(diagnostic);
    }

    private void RememberHeight(object sender, DragCompletedEventArgs e)
    {
        if (DetailsRow.ActualHeight >= 80) _detailHeight = DetailsRow.ActualHeight;
    }

    private void WorkbenchSizeChanged(object sender, SizeChangedEventArgs e) => UpdateWorkbenchLayout();

    private void UpdateWorkbenchLayout()
    {
        if (SettingsGrid is null || ToolbarActions is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        bool compact = ActualWidth < 860;
        SettingsGrid.ColumnDefinitions[0].Width = _presentationViewModel?.HasIronParameters == true
            ? new GridLength(compact ? 1 : 1.3, GridUnitType.Star) : new GridLength(0);
        SettingsGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
        SettingsGrid.ColumnDefinitions[2].Width = compact ? new GridLength(0) : new GridLength(1.7, GridUnitType.Star);
        Grid.SetRow(DisplayCard, compact ? 1 : 0);
        Grid.SetColumn(DisplayCard, compact ? 0 : 2);
        Grid.SetColumnSpan(DisplayCard, compact ? 2 : 1);
        DisplayCard.BorderThickness = compact ? new Thickness(0, 1, 0, 0) : new Thickness(0);
        NormalizationCard.BorderThickness = compact ? new Thickness(0) : new Thickness(0, 0, 1, 0);

        bool wrapToolbar = ActualWidth < 1100;
        Grid.SetRow(ToolbarActions, wrapToolbar ? 1 : 0);
        Grid.SetColumn(ToolbarActions, wrapToolbar ? 0 : 2);
        Grid.SetColumnSpan(ToolbarActions, wrapToolbar ? 3 : 1);
        ToolbarActions.HorizontalAlignment = HorizontalAlignment.Right;
        ToolbarActions.Margin = wrapToolbar ? new Thickness(0, 8, 0, 0) : new Thickness(0);

        DetailsRow.MaxHeight = Math.Max(140, ActualHeight * 0.55);
        // Compact mode puts input/display in a second row; leave room for both rows.
        SettingsScroll.MaxHeight = Math.Max(compact || ActualHeight >= 600 ? 224 : 112, Math.Ceiling(SettingsGrid.ActualHeight));
    }
}
