using System.Collections.Generic;
using System.Windows;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Views
{
    public partial class AddWidgetWindow : HandyControl.Controls.Window
    {
        public HomeAppItem SelectedWidget { get; private set; }

        public AddWidgetWindow(List<HomeAppItem> availableWidgets)
        {
            InitializeComponent();
            WidgetList.ItemsSource = availableWidgets;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (WidgetList.SelectedItem is HomeAppItem item)
            {
                SelectedWidget = item;
                DialogResult = true;
                Close();
            }
            else
            {
                HandyControl.Controls.Growl.Warning("请选择一个组件");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
