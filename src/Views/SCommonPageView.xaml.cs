using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// SCommonPageView.xaml 的交互逻辑
    /// </summary>
    public partial class SCommonPageView : Page
    {
        private static SCommonPageView commonPage = null!;

        public SCommonPageView()
        {
            InitializeComponent();
            var viewModel = new SCommonPageViewModel();
            this.DataContext = viewModel;
            viewModel.CoverFlowMain = this.CoverFlow;
            viewModel.RebuildCoverFlowAction = RebuildCoverFlow;
            viewModel.GetFlowPic();
        }

        public static Page GetPage()
        {
            if (commonPage == null)
            {
                commonPage = new SCommonPageView();
            }
            return commonPage;
        }

        /// <summary>
        /// HandyControl CoverFlow 无公开 Clear，删除/覆盖后需重建控件。
        /// 图片通过内存加载，避免 URI 解码锁定磁盘文件导致无法删除。
        /// </summary>
        private void RebuildCoverFlow(IReadOnlyList<string> imagePaths, int pageIndex)
        {
            if (DataContext is not SCommonPageViewModel viewModel)
                return;

            CoverFlowHost.Children.Clear();

            var coverFlow = new CoverFlow { Margin = new Thickness(0) };
            var coverItems = new List<object>(imagePaths.Count);
            foreach (var file in imagePaths)
            {
                // 即使解码失败也占位，保证与 ViewModel 路径列表索引一致
                coverItems.Add(new Image
                {
                    Source = StartPicHelper.LoadBitmapWithoutFileLock(file),
                    Stretch = Stretch.Uniform
                });
            }
            coverFlow.AddRange(coverItems);

            int safeIndex = imagePaths.Count == 0
                ? 0
                : Math.Clamp(pageIndex, 0, imagePaths.Count - 1);
            coverFlow.PageIndex = safeIndex;

            BindingOperations.SetBinding(
                coverFlow,
                CoverFlow.PageIndexProperty,
                new Binding(nameof(SCommonPageViewModel.CoverFlowPageIndex))
                {
                    Mode = BindingMode.TwoWay,
                    Source = viewModel
                });

            CoverFlowHost.Children.Add(coverFlow);
            CoverFlow = coverFlow;
            viewModel.CoverFlowMain = coverFlow;
        }
    }
}
