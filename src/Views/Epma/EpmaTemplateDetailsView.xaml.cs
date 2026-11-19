using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views;

public partial class EpmaTemplateDetailsView : UserControl
{
    public static readonly DependencyProperty IsEditorPreviewProperty = DependencyProperty.Register(nameof(IsEditorPreview), typeof(bool), typeof(EpmaTemplateDetailsView), new PropertyMetadata(false));
    public bool IsEditorPreview { get => (bool)GetValue(IsEditorPreviewProperty); set => SetValue(IsEditorPreviewProperty, value); }
    public EpmaTemplateDetailsView() => InitializeComponent();
}
