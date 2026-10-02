using GeoChemistryNexus.ViewModels;
using System.Windows.Controls;
using System.Windows.Documents;

namespace GeoChemistryNexus.Controls.GeothermometerEditorPanels
{
    public partial class GeoTHelpDocPanel : UserControl
    {
        public GeoTHelpDocPanel()
        {
            InitializeComponent();
        }

        public RichTextBox HelpDocEditorControl => HelpDocEditor;

        private void BoldButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm && vm.IsContentReadOnly) return;
            EditingCommands.ToggleBold.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void ItalicButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm && vm.IsContentReadOnly) return;
            EditingCommands.ToggleItalic.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void UnderlineButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm && vm.IsContentReadOnly) return;
            EditingCommands.ToggleUnderline.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is GeothermometerEditorViewModel vm && vm.IsContentReadOnly) return;
            if (HelpDocEditor?.Selection == null) return;

            string? sizeText = FontSizeComboBox.SelectedItem is ComboBoxItem item
                ? item.Content?.ToString()
                : FontSizeComboBox.SelectedItem as string;

            if (sizeText != null && double.TryParse(sizeText, out double size) && size > 0 && size <= 200)
            {
                HelpDocEditor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
                HelpDocEditor.Focus();
            }
        }
    }
}
