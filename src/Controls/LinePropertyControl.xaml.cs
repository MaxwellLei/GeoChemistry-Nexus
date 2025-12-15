using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    public partial class LinePropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(LinePropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public LinePropertyControl()
        {
            InitializeComponent();
        }

        private void PickStartPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedObject is LineDefinition lineDef)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(lineDef.Start));
            }
        }

        private void PickEndPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedObject is LineDefinition lineDef)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(lineDef.End));
            }
        }
    }
}