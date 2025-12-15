using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 支持 GridLength (GridUnitType.Star 或 Pixel) 的动画
    /// </summary>
    public class GridLengthAnimation : AnimationTimeline
    {
        public override Type TargetPropertyType => typeof(GridLength);

        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }

        public static readonly DependencyProperty FromProperty = DependencyProperty.Register(
            "From", typeof(GridLength), typeof(GridLengthAnimation));

        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }

        public static readonly DependencyProperty ToProperty = DependencyProperty.Register(
            "To", typeof(GridLength), typeof(GridLengthAnimation));

        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }

        public IEasingFunction EasingFunction
        {
            get => (IEasingFunction)GetValue(EasingFunctionProperty);
            set => SetValue(EasingFunctionProperty, value);
        }

        public static readonly DependencyProperty EasingFunctionProperty =
            DependencyProperty.Register("EasingFunction", typeof(IEasingFunction), typeof(GridLengthAnimation));

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            double fromVal = From.Value;
            double toVal = To.Value;

            // 如果单位类型不一致，无法进行插值动画，直接返回目标值
            // 注意：这里只处理 Pixel-Pixel 或 Star-Star 的情况
            // Auto 不支持动画
            if (From.GridUnitType != To.GridUnitType)
            {
                // 特殊处理：如果一个是 0 (Pixel/Star)，另一个是 Star，可以将 0 视为 0 Star
                if (fromVal == 0 && To.IsStar)
                {
                    fromVal = 0; // 视为 0 Star
                }
                else if (toVal == 0 && From.IsStar)
                {
                    toVal = 0; // 视为 0 Star
                }
                else
                {
                    return To;
                }
            }

            double progress = animationClock.CurrentProgress.Value;
            if (EasingFunction != null)
            {
                progress = EasingFunction.Ease(progress);
            }

            double newValue = fromVal + (toVal - fromVal) * progress;
            
            // 防止出现负数
            if (newValue < 0) newValue = 0;

            return new GridLength(newValue, To.IsStar || From.IsStar ? GridUnitType.Star : GridUnitType.Pixel);
        }
    }
}
