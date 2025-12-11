using GeoChemistryNexus.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GeoChemistryNexus.Controls
{
    public partial class CustomPropertyGrid : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(CustomPropertyGrid),
                new PropertyMetadata(null, OnSelectedObjectChanged));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public ObservableCollection<PropertyItemViewModel> Properties { get; } = new ObservableCollection<PropertyItemViewModel>();

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(CustomPropertyGrid), new PropertyMetadata("属性检查器"));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public CustomPropertyGrid()
        {
            InitializeComponent();
        }

        private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (CustomPropertyGrid)d;
            grid.RefreshProperties(e.NewValue);
        }

        private void RefreshProperties(object target)
        {
            Properties.Clear();
            if (target == null) return;

            var properties = target.GetType().GetProperties();
            foreach (var prop in properties)
            {
                // Check Browsable
                var browsableAttr = prop.GetCustomAttributes(typeof(BrowsableAttribute), true).FirstOrDefault() as BrowsableAttribute;
                if (browsableAttr != null && !browsableAttr.Browsable) continue;

                var vm = new PropertyItemViewModel(target, prop);
                Properties.Add(vm);
            }

            var view = CollectionViewSource.GetDefaultView(Properties);
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
        }
    }
}
