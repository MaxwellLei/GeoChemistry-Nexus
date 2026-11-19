using System;

namespace GeoChemistryNexus.Controls.Startup
{
    /// <summary>
    /// Five scenes. Normal pace is one second per scene. Once initialization is ready the remaining
    /// scenes are sped up (see <see cref="ReadySpeed"/>), but the opening scenes and the final logo
    /// scene are always shown at a pace the user can follow. If initialization is slower than the
    /// animation, the last stretch slows down instead (see <see cref="AdvanceWaiting"/>).
    /// </summary>
    internal sealed class StartupTimeline
    {
        private const double SceneSpeed = 1 / 5.0;
        private const double WaitingStart = .9;
        private const double WaitingLimit = .995;
        private const double ResumeSeconds = .3;

        // ---- 加载完成后的加速策略（可按观感调整） ----
        // 前 GuaranteedScenes 幕始终按正常速度播放，保证用户一定能看到开头。
        private const double GuaranteedScenes = 2;
        // 中间场景（岩石之后、Logo 之前）加载完成后的每幕耗时（秒）。
        private const double MiddleSceneSeconds = .45;
        // 最后一幕（Logo 与名称出现）的每幕耗时（秒），不宜太短，否则看不清 Logo。
        private const double FinalSceneSeconds = .8;
        // 速度切换的平滑系数，越大切换越快；避免加速时画面突然跳变。
        private const double SpeedSmoothing = 8;
        private double _speed = SceneSpeed;

        public double Progress { get; private set; }
        public double LoadingProgress { get; private set; }
        public bool IsReady { get; private set; }
        public bool IsComplete { get; private set; }
        public double Opacity => 1;
        private bool _waiting;
        private double _waitingElapsed;
        private double _resumeStart;
        private double _resumeDuration;
        private double _resumeElapsed;
        private double _resumeSlope;

        public void Report(double percent)
        {
            if (double.IsFinite(percent)) LoadingProgress = Math.Max(LoadingProgress, Math.Clamp(percent, 0, 100));
        }

        public void Finish()
        {
            if (IsReady) return;
            LoadingProgress = 100;
            IsReady = true;
            if (!_waiting) return;
            // Release a slow load from its current frame, matching its current speed rather than
            // jumping to 100%, replaying the last scene, or accelerating all the intermediate scenes.
            _resumeStart = Progress;
            double distance = 1 - Progress;
            double speed = SceneSpeed * Math.Exp(-WaitingRate * _waitingElapsed);
            _resumeDuration = Math.Max(ResumeSeconds, distance / SceneSpeed);
            _resumeSlope = speed * _resumeDuration / distance;
        }

        public void Advance(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds <= 0 || IsComplete) return;
            if (_waiting)
            {
                if (!IsReady)
                {
                    AdvanceWaiting(seconds);
                    return;
                }
                if (_resumeElapsed < _resumeDuration)
                {
                    double used = Math.Min(seconds, _resumeDuration - _resumeElapsed);
                    _resumeElapsed += used;
                    seconds -= used;
                    double t = Math.Clamp(_resumeElapsed / _resumeDuration, 0, 1);
                    // Cubic Hermite interpolation: continuous entry velocity and a stationary end.
                    double eased = (-2 * t * t * t + 3 * t * t)
                        + (t * t * t - 2 * t * t + t) * _resumeSlope;
                    Progress = _resumeStart + (1 - _resumeStart) * Math.Clamp(eased, 0, 1);
                }
                if (_resumeElapsed < _resumeDuration) return;
                Progress = 1;
            }
            else if (Progress < 1)
            {
                double limit = IsReady ? 1 : WaitingStart;
                // 未加载完时保持正常速度；加载完成后按进度所处场景平滑过渡到目标速度。
                double target = IsReady ? ReadySpeed(Progress) : SceneSpeed;
                _speed += (target - _speed) * (1 - Math.Exp(-SpeedSmoothing * seconds));
                double used = Math.Min(seconds, (limit - Progress) / _speed);
                Progress = Math.Min(limit, Progress + used * _speed);
                seconds -= used;
                if (!IsReady && Progress >= WaitingStart)
                {
                    _waiting = true;
                    AdvanceWaiting(seconds);
                    return;
                }
                if (Progress < 1) return;
            }
            // The fifth scene is the ending: no additional still frame or fade delay.
            IsComplete = true;
        }

        /// <summary>程序已加载完成时，当前进度对应的播放速度（进度/秒）。</summary>
        private static double ReadySpeed(double progress)
        {
            const double sceneLength = 1 / 5.0;
            double guaranteedEnd = GuaranteedScenes * sceneLength;
            double finalStart = 1 - sceneLength;
            if (progress < guaranteedEnd) return SceneSpeed;
            if (progress < finalStart) return sceneLength / MiddleSceneSeconds;
            return sceneLength / FinalSceneSeconds;
        }

        private static double WaitingRate => SceneSpeed / (WaitingLimit - WaitingStart);

        private void AdvanceWaiting(double seconds)
        {
            _waitingElapsed += seconds;
            // Match the one-second-scene speed at entry, then progressively slow down. The artwork
            // remains animated, but the timeline can never complete before the real application does.
            Progress = WaitingStart + (WaitingLimit - WaitingStart)
                * (1 - Math.Exp(-WaitingRate * _waitingElapsed));
        }
    }
}
