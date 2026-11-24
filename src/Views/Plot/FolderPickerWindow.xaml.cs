using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// 文件夹选择窗口（移动到 / 选择保存位置）。业务逻辑全部在 <see cref="FolderPickerDialogViewModel"/> 中，
    /// 这里只负责窗口外壳、对话框结果和把选中项滚动到可视区域。
    /// </summary>
    public partial class FolderPickerWindow : Window
    {
        private readonly FolderPickerDialogViewModel _viewModel;

        public FolderPickerWindow(FolderPickerDialogViewModel viewModel, Window? owner = null)
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();

            Owner = owner;
            WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
            viewModel.DialogOwner = this;

            NativeWindowShadowHelper.Attach(this);
            UiScaleHelper.Attach(this);
            ChildWindowPlacementHelper.Attach(this);

            viewModel.CloseRequested += OnCloseRequested;
            viewModel.ScrollToSelectionRequested += ScrollSelectionIntoView;
            Loaded += (_, _) => ScrollSelectionIntoView();
            Closed += (_, _) =>
            {
                viewModel.CloseRequested -= OnCloseRequested;
                viewModel.ScrollToSelectionRequested -= ScrollSelectionIntoView;
            };
        }

        /// <summary>确认后选中的文件夹路径（当前界面语言，不含根）。</summary>
        public IReadOnlyList<string>? SelectedPath => _viewModel.SelectedPath;

        private void OnCloseRequested(bool confirmed) => DialogResult = confirmed;

        private void ScrollSelectionIntoView()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
            {
                if (_viewModel.SelectedNode == null)
                    return;

                var chain = new Stack<FolderPickerNodeViewModel>();
                for (var node = _viewModel.SelectedNode; node != null; node = node.Parent)
                    chain.Push(node);

                ItemsControl container = FolderTree;
                while (chain.Count > 0)
                {
                    var node = chain.Pop();
                    container.UpdateLayout();
                    if (container.ItemContainerGenerator.ContainerFromItem(node) is not TreeViewItem item)
                        return;

                    if (chain.Count == 0)
                        item.BringIntoView();
                    container = item;
                }
            });
        }
    }
}
