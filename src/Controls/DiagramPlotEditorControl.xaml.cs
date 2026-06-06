using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace GeoChemistryNexus.Controls
{
    public partial class DiagramPlotEditorControl : UserControl
    {
        public DiagramPlotEditorViewModel ViewModel { get; }

        public static readonly DependencyProperty ConfirmCommandProperty =
            DependencyProperty.Register(nameof(ConfirmCommand), typeof(ICommand), typeof(DiagramPlotEditorControl));

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(DiagramPlotEditorControl));

        public ICommand? ConfirmCommand
        {
            get => (ICommand?)GetValue(ConfirmCommandProperty);
            set => SetValue(ConfirmCommandProperty, value);
        }

        public ICommand? CancelCommand
        {
            get => (ICommand?)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        public DiagramPlotEditorControl()
        {
            InitializeComponent();
            ViewModel = new DiagramPlotEditorViewModel();
            DataContext = ViewModel;

            InitializeLanguageCombo();
            RefreshCategorySuggestions();

            WeakReferenceMessenger.Default.Register<CategoryConfigUpdatedMessage>(this, (_, _) =>
            {
                Application.Current.Dispatcher.Invoke(ViewModel.ReloadCategories);
            });

            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(DiagramPlotEditorViewModel.SelectedSectionIndex))
                    UpdateSectionVisibility();
            };

            ViewModel.GetCurrentRtfContent = () =>
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

            ViewModel.SetCurrentRtfContent = rtf =>
            {
                try
                {
                    if (string.IsNullOrEmpty(rtf))
                        HelpDocEditor.Document.Blocks.Clear();
                    else
                        RtfHelper.LoadRtfString(HelpDocEditor, rtf);
                }
                catch
                {
                    HelpDocEditor.Document.Blocks.Clear();
                }
            };
        }

        public void InitializeForCreate() => ViewModel.InitializeForCreate();

        public void InitializeForEdit(Models.GraphMapTemplateEntity entity) => ViewModel.InitializeForEdit(entity);

        private void InitializeLanguageCombo()
        {
            LanguageCombo.SetBinding(
                System.Windows.Controls.Primitives.Selector.ItemsSourceProperty,
                new System.Windows.Data.Binding(nameof(DiagramPlotEditorViewModel.AppLanguageOptions)));
            LanguageCombo.DisplayMemberPath = nameof(CultureOption.DisplayName);
            LanguageCombo.SelectedValuePath = nameof(CultureOption.Code);
        }

        private void NavSection_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && int.TryParse(rb.Tag?.ToString(), out int index))
                ViewModel.SelectedSectionIndex = index;
        }

        private void UpdateSectionVisibility()
        {
            int index = ViewModel.SelectedSectionIndex;
            BasicPanel.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            TranslationPanel.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            HelpPanel.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;

            switch (index)
            {
                case 0:
                    NavBasic.IsChecked = true;
                    break;
                case 1:
                    NavTranslation.IsChecked = true;
                    break;
                case 2:
                    NavHelp.IsChecked = true;
                    break;
            }
        }

        private void LanguageCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (ViewModel.TryAddLanguage(LanguageCombo.Text?.Trim() ?? string.Empty))
                LanguageCombo.Text = string.Empty;
            e.Handled = true;
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageCombo.SelectedValue is string code && ViewModel.TryAddLanguage(code))
            {
                LanguageCombo.SelectedIndex = -1;
                LanguageCombo.Text = string.Empty;
            }
        }

        private void RemoveLanguage_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is LanguageTagModel item)
                ViewModel.RemoveLanguage(item);
        }

        private void CategoryCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (ViewModel.TryAddCategory(CategoryCombo.Text?.Trim() ?? string.Empty))
                CategoryCombo.Text = string.Empty;
            e.Handled = true;
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!CategoryCombo.IsDropDownOpen)
            {
                CategoryCombo.SelectedIndex = -1;
                return;
            }

            if (CategoryCombo.SelectedItem is CategoryDisplayItem item &&
                ViewModel.TryAddCategory(item.DisplayName, item.OriginalObject as Dictionary<string, string>))
            {
                CategoryCombo.SelectedIndex = -1;
                CategoryCombo.Text = string.Empty;
            }

            RefreshCategorySuggestions();
        }

        private void RemoveCategory_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CategoryPartModel item)
            {
                ViewModel.RemoveCategory(item);
                RefreshCategorySuggestions();
            }
        }

        private void RefreshCategorySuggestions()
        {
            var list = ViewModel.GetCategorySuggestions();
            if (list == null)
            {
                CategoryCombo.ItemsSource = null;
                return;
            }

            CategoryCombo.ItemsSource = list.Select(c => new CategoryDisplayItem
            {
                DisplayName = PlotCategoryHelper.GetName(c),
                OriginalObject = c
            }).ToList();
        }

        private void TranslationGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Context")
            {
                e.Column.IsReadOnly = true;
                e.Column.Header = LanguageService.Instance["translation_item_context"] ?? "Item";
                e.Column.Width = new DataGridLength(200);
                e.Column.MinWidth = 160;
            }
            else if (e.PropertyName is "ObjectRef" or DiagramTemplateTranslationHelper.TranslationKeyColumn)
            {
                e.Cancel = true;
            }
            else
            {
                string langCode = e.PropertyName;
                bool isDefault = string.Equals(langCode, ViewModel.DefaultLanguageCode, StringComparison.OrdinalIgnoreCase);
                e.Column = CreateMultilineTranslationColumn(langCode, isDefault ? $"{langCode} *" : langCode);
            }
        }

        private static DataGridTemplateColumn CreateMultilineTranslationColumn(string bindingPath, string header)
        {
            var binding = new Binding($"[{bindingPath}]")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.TwoWay
            };

            var displayTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            displayTextBlock.SetBinding(TextBlock.TextProperty, new Binding($"[{bindingPath}]"));
            displayTextBlock.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            displayTextBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top);

            var editTextBox = new FrameworkElementFactory(typeof(TextBox));
            editTextBox.SetBinding(TextBox.TextProperty, binding);
            editTextBox.SetValue(TextBox.AcceptsReturnProperty, true);
            editTextBox.SetValue(TextBox.TextWrappingProperty, TextWrapping.Wrap);
            editTextBox.SetValue(TextBox.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            editTextBox.SetValue(TextBox.MinHeightProperty, 64.0);
            editTextBox.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            editTextBox.SetValue(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Top);
            editTextBox.SetValue(TextBox.PaddingProperty, new Thickness(2));

            return new DataGridTemplateColumn
            {
                Header = header,
                Width = new DataGridLength(140),
                MinWidth = 120,
                CellTemplate = new DataTemplate { VisualTree = displayTextBlock },
                CellEditingTemplate = new DataTemplate { VisualTree = editTextBox }
            };
        }

        private void TranslationGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.EditingElement is not TextBox textBox) return;

            textBox.PreviewKeyDown -= TranslationCellTextBox_PreviewKeyDown;
            textBox.PreviewKeyDown += TranslationCellTextBox_PreviewKeyDown;
        }

        private static void TranslationCellTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                e.Handled = true;
        }

        private void TranslationGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is not System.Data.DataRowView rowView) return;

            int maxLines = 1;
            foreach (System.Data.DataColumn column in rowView.Row.Table.Columns)
            {
                if (!DiagramTemplateTranslationHelper.IsLanguageColumn(column.ColumnName)) continue;

                if (rowView[column.ColumnName] is string text && !string.IsNullOrEmpty(text))
                    maxLines = Math.Max(maxLines, text.Split('\n').Length);
            }

            e.Row.MinHeight = Math.Max(32, maxLines * 18 + 12);
        }

        private void TranslationGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => ViewModel.OnTranslationCellChanged()));
        }

        private void SwapTranslationLanguage_Click(object sender, RoutedEventArgs e)
        {
            var langs = GetTranslationLanguages();
            if (langs.Count < 2)
            {
                HandyControl.Controls.MessageBox.Show(
                    LanguageService.Instance["at_least_two_languages_swap"] ?? "At least two languages required.",
                    LanguageService.Instance["tips"] ?? "Tips",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new DualComboSelectDialog(
                langs,
                LanguageService.Instance["swap_language_columns"] ?? "Swap Languages",
                LanguageService.Instance["swap_language_columns"] ?? "Swap",
                OwnerWindow());
            if (dialog.ShowDialog() == true &&
                !string.IsNullOrEmpty(dialog.FirstSelection) &&
                !string.IsNullOrEmpty(dialog.SecondSelection) &&
                dialog.FirstSelection != dialog.SecondSelection)
            {
                ViewModel.SwapTranslationLanguagesCommand.Execute(Tuple.Create(dialog.FirstSelection, dialog.SecondSelection));
            }
        }

        private List<string> GetTranslationLanguages()
        {
            if (ViewModel.TranslationTable == null) return new List<string>();
            return ViewModel.TranslationTable.Columns.Cast<System.Data.DataColumn>()
                .Where(c => DiagramTemplateTranslationHelper.IsLanguageColumn(c.ColumnName))
                .Select(c => c.ColumnName)
                .ToList();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.GetHelpDocumentsForSubmit();

            if (ConfirmCommand?.CanExecute(ViewModel) ?? ConfirmCommand?.CanExecute(null) ?? false)
                ConfirmCommand.Execute(ViewModel);
            else if (ConfirmCommand != null)
                ConfirmCommand.Execute(ViewModel);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (CancelCommand?.CanExecute(ViewModel) ?? true)
                CancelCommand?.Execute(ViewModel);
        }

        private Window? OwnerWindow() => Window.GetWindow(this);

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

            string? sizeText = FontSizeComboBox.SelectedItem is ComboBoxItem item
                ? item.Content?.ToString()
                : FontSizeComboBox.SelectedItem as string;

            if (sizeText != null && double.TryParse(sizeText, out double size) && size > 0 && size <= 200)
            {
                HelpDocEditor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
                HelpDocEditor.Focus();
            }
        }

        private class CategoryDisplayItem
        {
            public string DisplayName { get; set; } = string.Empty;
            public object? OriginalObject { get; set; }
        }
    }
}
