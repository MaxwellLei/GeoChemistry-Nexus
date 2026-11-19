using System;
using System.Windows;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 让子窗体初始时比所属主窗体小：打开前统一收敛宽高和最小尺寸，
    /// 避免弹出的小组件窗体比主窗体还大；不限制最大尺寸，用户可自由放大 / 最大化。
    /// </summary>
    public static class ChildWindowSizeHelper
    {
        /// <param name="child">待显示的子窗体（需在 Show 之前调用）。</param>
        /// <param name="owner">主窗体；为空或不可见时退回到当前工作区尺寸。</param>
        /// <param name="ratio">子窗体最多占主窗体的比例。</param>
        public static void FitInside(Window child, Window? owner, double ratio = 0.92)
        {
            double referenceWidth;
            double referenceHeight;

            if (owner != null && owner.IsVisible && owner.ActualWidth > 0 && owner.ActualHeight > 0)
            {
                referenceWidth = owner.ActualWidth;
                referenceHeight = owner.ActualHeight;
            }
            else
            {
                referenceWidth = SystemParameters.WorkArea.Width;
                referenceHeight = SystemParameters.WorkArea.Height;
            }

            double limitWidth = Math.Floor(referenceWidth * ratio);
            double limitHeight = Math.Floor(referenceHeight * ratio);

            // 注意：不能设置 MaxWidth / MaxHeight。
            // WPF 会按它限制窗体的布局区域，最大化或手动拉大后超出的部分会变成纯黑色。
            // 这里只约束“初始尺寸”，最大化交给系统（WindowWorkAreaHelper 对齐到工作区）。
            child.MinWidth = Math.Min(child.MinWidth, limitWidth);
            child.MinHeight = Math.Min(child.MinHeight, limitHeight);

            // 按内容自适应的窗体没有固定值，无需处理
            if (child.SizeToContent != SizeToContent.Manual && child.SizeToContent != SizeToContent.Height)
                return;

            if (!double.IsNaN(child.Width))
                child.Width = Math.Min(child.Width, limitWidth);
            if (child.SizeToContent == SizeToContent.Manual && !double.IsNaN(child.Height))
                child.Height = Math.Min(child.Height, limitHeight);
        }
    }
}

