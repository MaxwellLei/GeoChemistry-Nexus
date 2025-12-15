using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    public partial class ArrowPropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(ArrowPropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public ArrowPropertyControl()
        {
            InitializeComponent();
        }

        private void PickStartPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedObject is ArrowDefinition arrowDef)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(arrowDef.Start));
            }
        }

        private void PickEndPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedObject is ArrowDefinition arrowDef)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(arrowDef.End));
            }
        }
    }
}