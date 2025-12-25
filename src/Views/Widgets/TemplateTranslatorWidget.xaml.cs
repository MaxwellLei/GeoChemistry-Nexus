using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
                if (files != null && files.Length > 0)
                {
                    if (this.DataContext is TemplateTranslatorViewModel vm)
                    {
                        vm.ImportTemplateCommand.Execute(files[0]);
                    }
                }
            }
        }

        private void Border_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = LanguageService.Instance["select_drawing_template_file"]        // 选择绘图模板文件
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (this.DataContext is TemplateTranslatorViewModel vm)
                {
                    vm.ImportTemplateCommand.Execute(openFileDialog.FileName);
                }
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
            var dialog = new InputDialog("请输入新语言代码 (例如: fr-FR):", "添加语言")
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                if (this.DataContext is TemplateTranslatorViewModel vm)
                {
                    vm.AddLanguageCommand.Execute(dialog.InputText);
                }
            }
        }

        private void SwapLanguage_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as TemplateTranslatorViewModel;
            if (vm == null || vm.TranslationTable == null) return;

            var languages = new List<string>();
            foreach (System.Data.DataColumn col in vm.TranslationTable.Columns)
            {
                if (col.ColumnName != "Context" && col.ColumnName != "ObjectRef")
                {
                    languages.Add(col.ColumnName);
                }
            }

            if (languages.Count < 2)
            {
                MessageBox.Show("至少需要两种语言才能进行交换。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SwapDialog(languages)
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedLang1) && !string.IsNullOrEmpty(dialog.SelectedLang2))
            {
                if (dialog.SelectedLang1 == dialog.SelectedLang2)
                {
                    MessageBox.Show("请选择两个不同的语言进行交换。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                vm.SwapLanguagesCommand.Execute(new Tuple<string, string>(dialog.SelectedLang1, dialog.SelectedLang2));
            }
        }

        private class SwapDialog : Window
        {
            public string SelectedLang1 { get; private set; }
            public string SelectedLang2 { get; private set; }
            private ComboBox _combo1;
            private ComboBox _combo2;

            public SwapDialog(IEnumerable<string> items)
            {
                Title = "交换语言列";
                MinWidth = 350;
                SizeToContent = SizeToContent.WidthAndHeight;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ResizeMode = ResizeMode.NoResize;
                ShowInTaskbar = false;
                WindowStyle = WindowStyle.SingleBorderWindow;

                var stackPanel = new StackPanel { Margin = new Thickness(20) };
                
                stackPanel.Children.Add(new TextBlock { Text = "语言 1:", Margin = new Thickness(0, 0, 0, 5) });
                _combo1 = new ComboBox { ItemsSource = items, SelectedIndex = 0 };
                stackPanel.Children.Add(_combo1);

                stackPanel.Children.Add(new TextBlock { Text = "语言 2:", Margin = new Thickness(0, 15, 0, 5) });
                int count = 0;
                foreach(var item in items) count++;
                _combo2 = new ComboBox { ItemsSource = items, SelectedIndex = count > 1 ? 1 : 0 };
                stackPanel.Children.Add(_combo2);

                var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };

                var btnCancel = new Button { Content = "取消", Width = 80, IsCancel = true };

                var btnOk = new Button { Content = "确定", Width = 80, IsDefault = true, Margin = new Thickness(10, 0, 0, 0) };
                btnOk.Click += (s, e) => 
                { 
                    SelectedLang1 = _combo1.SelectedItem as string; 
                    SelectedLang2 = _combo2.SelectedItem as string; 
                    DialogResult = true; 
                };

                btnPanel.Children.Add(btnCancel);
                btnPanel.Children.Add(btnOk);
                stackPanel.Children.Add(btnPanel);

                Content = stackPanel;
            }
        }

        private void RemoveLanguage_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as TemplateTranslatorViewModel;
            if (vm == null || vm.TranslationTable == null) return;

            var languages = new List<string>();
            foreach (System.Data.DataColumn col in vm.TranslationTable.Columns)
            {
                if (col.ColumnName != "Context" && col.ColumnName != "ObjectRef")
                {
                    languages.Add(col.ColumnName);
                }
            }

            var selectDialog = new SelectionDialog(languages, "请选择要删除的语言:");
            if (selectDialog.ShowDialog() == true && !string.IsNullOrEmpty(selectDialog.SelectedValue))
            {
                if (MessageBox.Show($"确定要删除语言 '{selectDialog.SelectedValue}' 吗？此操作不可撤销。", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    vm.RemoveLanguageCommand.Execute(selectDialog.SelectedValue);
                }
            }
        }

        private class InputDialog : Window
        {
            public string InputText { get; private set; }
            private TextBox _textBox;

            public InputDialog(string prompt, string title, string defaultValue = "")
            {
                Title = title;
                MinWidth = 350;
                SizeToContent = SizeToContent.WidthAndHeight;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ResizeMode = ResizeMode.NoResize;
                ShowInTaskbar = false;
                WindowStyle = WindowStyle.SingleBorderWindow;

                var stackPanel = new StackPanel { Margin = new Thickness(20) };
                stackPanel.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10) });

                _textBox = new TextBox { Text = defaultValue };
                stackPanel.Children.Add(_textBox);

                var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };

                var btnCancel = new Button { Content = "取消", Width = 80, IsCancel = true };

                var btnOk = new Button { Content = "确定", Width = 80, IsDefault = true, Margin = new Thickness(10,0,0,0) };
                btnOk.Click += (s, e) => { InputText = _textBox.Text; DialogResult = true; };

                btnPanel.Children.Add(btnCancel);
                btnPanel.Children.Add(btnOk);
                stackPanel.Children.Add(btnPanel);

                Content = stackPanel;
            }
        }

        private class SelectionDialog : Window
        {
            public string SelectedValue { get; private set; }
            private ComboBox _comboBox;

            public SelectionDialog(IEnumerable<string> items, string prompt)
            {
                Title = "选择";
                MinWidth = 300;
                SizeToContent = SizeToContent.WidthAndHeight;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ResizeMode = ResizeMode.NoResize;
                ShowInTaskbar = false;
                WindowStyle = WindowStyle.SingleBorderWindow;

                var stackPanel = new StackPanel { Margin = new Thickness(20) };
                stackPanel.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10) });

                _comboBox = new ComboBox { ItemsSource = items, SelectedIndex = 0 };
                stackPanel.Children.Add(_comboBox);

                var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };

                var btnCancel = new Button { Content = "取消", Width = 80, IsCancel = true };

                var btnOk = new Button { Content = "确定", Width = 80, IsDefault = true, Margin = new Thickness(10, 0, 0, 0) };
                btnOk.Click += (s, e) => { SelectedValue = _comboBox.SelectedItem as string; DialogResult = true; };

                btnPanel.Children.Add(btnCancel);
                btnPanel.Children.Add(btnOk);
                stackPanel.Children.Add(btnPanel);

                Content = stackPanel;
            }
        }
    }
}
