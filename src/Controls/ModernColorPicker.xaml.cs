using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// ModernColorPicker.xaml 的交互逻辑
    /// </summary>
    public partial class ModernColorPicker : UserControl
    {
        public static readonly DependencyProperty SelectedBrushProperty =
            DependencyProperty.Register("SelectedBrush", typeof(Brush), typeof(ModernColorPicker),
                new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ModernColorPicker),
                new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

        public Brush SelectedBrush
        {
            get { return (Brush)GetValue(SelectedBrushProperty); }
            set { SetValue(SelectedBrushProperty, value); }
        }

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public ICommand ShowColorDialogCommand { get; }

        public ModernColorPicker()
        {
            InitializeComponent();
            ShowColorDialogCommand = new RelayCommand(ShowColorDialog);
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (ModernColorPicker)d;
            picker.SelectedBrush = new SolidColorBrush((Color)e.NewValue);
        }

        private void DropDownButton_Click(object sender, RoutedEventArgs e)
        {
            ColorPopup.IsOpen = !ColorPopup.IsOpen;
        }

        private void QuickColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorString)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorString);
                    SelectedColor = color;
                    SelectedBrush = new SolidColorBrush(color);
                    ColorPopup.IsOpen = false;
                }
                catch
                {
                    // 颜色转换失败，忽略
                }
            }
        }

        private void MoreColorsButton_Click(object sender, RoutedEventArgs e)
        {
            ColorPopup.IsOpen = false;
            ShowColorDialog();
        }

        private void ShowColorDialog()
        {
            var colorDialog = new System.Windows.Forms.ColorDialog
            {
                Color = System.Drawing.Color.FromArgb(SelectedColor.A, SelectedColor.R, SelectedColor.G, SelectedColor.B),
                FullOpen = true
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var winFormsColor = colorDialog.Color;
                var wpfColor = Color.FromArgb(winFormsColor.A, winFormsColor.R, winFormsColor.G, winFormsColor.B);
                SelectedColor = wpfColor;
                SelectedBrush = new SolidColorBrush(wpfColor);
            }
        }
    }

    // 简单的命令实现
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
