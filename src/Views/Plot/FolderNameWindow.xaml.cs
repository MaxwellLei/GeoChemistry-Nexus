using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System.Collections.Generic;
using System.Windows;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// 新建 / 重命名文件夹窗口。业务逻辑全部在 <see cref="FolderNameDialogViewModel"/> 中，
    /// 这里只负责窗口外壳、对话框结果和打开时的输入框焦点。
    /// </summary>
    public partial class FolderNameWindow : Window
    {
        private readonly FolderNameDialogViewModel _viewModel;

        public FolderNameWindow(FolderNameDialogViewModel viewModel, Window? owner = null)
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();

            Owner = owner;
            WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;

            NativeWindowShadowHelper.Attach(this);
            UiScaleHelper.Attach(this);
            ChildWindowPlacementHelper.Attach(this);

            viewModel.CloseRequested += OnCloseRequested;
            Closed += (_, _) => viewModel.CloseRequested -= OnCloseRequested;
            Loaded += (_, _) =>
            {
                PrimaryBox.Focus();
                PrimaryBox.SelectAll();
            };
        }

        /// <summary>确认后的各语言名称（仅包含已填写的语言）。</summary>
        public Dictionary<string, string> Names => _viewModel.Names;

        private void OnCloseRequested(bool confirmed) => DialogResult = confirmed;
    }
}
