using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// ScatterPropertyControl.xaml 的交互逻辑
    /// </summary>
    public partial class ScatterPropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(ScatterPropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public ScatterPropertyControl()
        {
            InitializeComponent();
        }

        private void PickPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedObject is ScatterDefinition scatterDef && scatterDef.StartAndEnd != null)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(scatterDef.StartAndEnd));
            }
        }
    }
}
