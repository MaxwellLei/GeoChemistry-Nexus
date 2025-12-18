using System;
using System.Windows;
using System.Windows.Input;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// NewTemplateWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NewTemplateWindow : Window
    {
        public NewTemplateWindow()
        {
            InitializeComponent();
        }

        public ICommand ConfirmCommand
        {
            get { return (ICommand)GetValue(ConfirmCommandProperty); }
            set { SetValue(ConfirmCommandProperty, value); }
        }

        public static readonly DependencyProperty ConfirmCommandProperty =
            DependencyProperty.Register("ConfirmCommand", typeof(ICommand), typeof(NewTemplateWindow), new PropertyMetadata(null));

        public ICommand CancelCommand
        {
            get { return (ICommand)GetValue(CancelCommandProperty); }
            set { SetValue(CancelCommandProperty, value); }
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register("CancelCommand", typeof(ICommand), typeof(NewTemplateWindow), new PropertyMetadata(null));
    }
}
