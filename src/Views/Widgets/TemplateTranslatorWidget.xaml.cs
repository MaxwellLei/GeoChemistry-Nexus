using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class TemplateTranslatorWidget : UserControl
    {
        public TemplateTranslatorWidget()
        {
            InitializeComponent();
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 &&
                    DataContext is TemplateTranslatorViewModel vm)
                {
                    vm.ImportTemplateCommand.Execute(files[0]);
                }
            }
        }

        private void Border_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = FileDialogFilterHelper.JsonOrAll,
                Title = LanguageService.Instance["select_drawing_template_file"]
            };

            if (openFileDialog.ShowDialog() == true &&
                DataContext is TemplateTranslatorViewModel vm)
            {
                vm.ImportTemplateCommand.Execute(openFileDialog.FileName);
            }
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Context")
            {
                e.Column.IsReadOnly = true;
                e.Column.Header = "内容项";
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }
            else if (e.PropertyName == "ObjectRef")
            {
                e.Cancel = true;
            }
            else
            {
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }
        }

        private void AddLanguage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TextInputDialog(
                LanguageService.Instance["enter_language_code_prompt"] ?? "Enter language code (e.g. fr-FR):",
                LanguageService.Instance["add_language_column"] ?? "Add Language",
                Window.GetWindow(this));

            if (dialog.ShowDialog() == true &&
                !string.IsNullOrWhiteSpace(dialog.InputText) &&
                DataContext is TemplateTranslatorViewModel vm)
            {
                vm.AddLanguageCommand.Execute(dialog.InputText.Trim());
            }
        }

        private void SwapLanguage_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not TemplateTranslatorViewModel vm || vm.TranslationTable == null)
            {
                return;
            }

            var languages = GetTranslationLanguages(vm.TranslationTable);
            if (languages.Count < 2)
            {
                HandyControl.Controls.MessageBox.Show(
                    LanguageService.Instance["at_least_two_languages_swap"] ?? "At least two languages required.",
                    LanguageService.Instance["tips"] ?? "Tips",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new DualComboSelectDialog(
                languages,
                LanguageService.Instance["swap_language_columns"] ?? "Swap Languages",
                LanguageService.Instance["swap_language_columns"] ?? "Swap",
                Window.GetWindow(this));

            if (dialog.ShowDialog() == true &&
                !string.IsNullOrEmpty(dialog.FirstSelection) &&
                !string.IsNullOrEmpty(dialog.SecondSelection) &&
                dialog.FirstSelection != dialog.SecondSelection)
            {
                vm.SwapLanguagesCommand.Execute(Tuple.Create(dialog.FirstSelection, dialog.SecondSelection));
            }
        }

        private void RemoveLanguage_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not TemplateTranslatorViewModel vm || vm.TranslationTable == null)
            {
                return;
            }

            var languages = GetTranslationLanguages(vm.TranslationTable);
            if (languages.Count == 0)
            {
                return;
            }

            var selectDialog = new SingleComboSelectDialog(
                languages,
                LanguageService.Instance["select_language_to_delete"] ?? "Select language to delete:",
                LanguageService.Instance["delete_language_column"] ?? "Delete Language",
                Window.GetWindow(this));

            if (selectDialog.ShowDialog() != true || string.IsNullOrEmpty(selectDialog.SelectedItem))
            {
                return;
            }

            var confirm = HandyControl.Controls.MessageBox.Show(
                string.Format(
                    LanguageService.Instance["confirm_delete_language"] ?? "Delete language '{0}'?",
                    selectDialog.SelectedItem),
                LanguageService.Instance["tips"] ?? "Tips",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                vm.RemoveLanguageCommand.Execute(selectDialog.SelectedItem);
            }
        }

        private static List<string> GetTranslationLanguages(DataTable table)
        {
            return table.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName is not "Context" and not "ObjectRef")
                .Select(c => c.ColumnName)
                .ToList();
        }
    }
}
