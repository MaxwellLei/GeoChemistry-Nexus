using System.Windows;
using System.Windows.Media;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 把元素裁成圆角矩形。WPF 的 Border.CornerRadius 只裁背景，不裁子元素。
    /// </summary>
    public static class HomeRoundedClip
    {
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.RegisterAttached(
                "Radius",
                typeof(double),
                typeof(HomeRoundedClip),
                new PropertyMetadata(0d, OnRadiusChanged));

        public static void SetRadius(DependencyObject element, double value) => element.SetValue(RadiusProperty, value);

        public static double GetRadius(DependencyObject element) => (double)element.GetValue(RadiusProperty);

        private static void OnRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            element.SizeChanged -= OnSizeChanged;
            if (GetRadius(element) > 0)
                element.SizeChanged += OnSizeChanged;

            UpdateClip(element);
        }

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is FrameworkElement element)
                UpdateClip(element);
        }

        private static void UpdateClip(FrameworkElement element)
        {
            double radius = GetRadius(element);
            if (radius <= 0 || element.ActualWidth <= 0 || element.ActualHeight <= 0)
            {
                element.Clip = null;
                return;
            }

            element.Clip = new RectangleGeometry
            {
                RadiusX = radius,
                RadiusY = radius,
                Rect = new Rect(0, 0, element.ActualWidth, element.ActualHeight)
            };
        }
    }
}
