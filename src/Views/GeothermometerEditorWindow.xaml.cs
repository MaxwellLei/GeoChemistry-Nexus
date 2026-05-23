using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// GeothermometerEditorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GeothermometerEditorWindow : Window
    {
        public GeothermometerEditorWindow()
        {
            InitializeComponent();
        }

        public GeothermometerEditorWindow(GeothermometerEditorViewModel viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.OnCloseRequested = () => this.Close();

            // 连接 ViewModel 与 RichTextBox 的回调
            viewModel.GetCurrentRtfContent = () =>
            {
                try
                {
                    return RtfHelper.GetRtfString(HelpDocEditor);
                }
                catch
                {
                    return null;
                }
            };

            viewModel.SetCurrentRtfContent = (rtf) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(rtf))
                    {
                        HelpDocEditor.Document.Blocks.Clear();
                    }
                    else
                    {
                        RtfHelper.LoadRtfString(HelpDocEditor, rtf);
                    }
                }
                catch
                {
                    HelpDocEditor.Document.Blocks.Clear();
                }
            };
        }

        // ==================== RTF 格式化按钮事件 ====================

        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleBold.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleItalic.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void UnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleUnderline.Execute(null, HelpDocEditor);
            HelpDocEditor.Focus();
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HelpDocEditor?.Selection == null) return;

            string sizeText = null;

            if (FontSizeComboBox.SelectedItem is ComboBoxItem item)
            {
                sizeText = item.Content?.ToString();
            }
            else if (FontSizeComboBox.SelectedItem is string s)
            {
                sizeText = s;
            }

            if (sizeText != null && double.TryParse(sizeText, out double size) && size > 0 && size <= 200)
            {
                HelpDocEditor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
                HelpDocEditor.Focus();
            }
        }
    }
}
