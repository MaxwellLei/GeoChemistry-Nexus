using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    public partial class TextPropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(TextPropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public TextPropertyControl()
        {
            InitializeComponent();
        }

        private void PickPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedObject is TextDefinition textDef && textDef.StartAndEnd != null)
            {
                WeakReferenceMessenger.Default.Send(new PickPointRequestMessage(textDef.StartAndEnd));
            }
        }
    }
}
