using GeoChemistryNexus.Views;
using GeoChemistryNexus.Controls.Startup;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// Owns the native splash on a separate STA dispatcher. WPF business views must be created
    /// on the application thread, but their constructors and theme loading cannot freeze the splash.
    /// </summary>
    public sealed class StartupSplashHost
    {
        private readonly TaskCompletionSource<StartWindow> _shown = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Action? _onUserClosed;
        private StartWindow? _window;
        private bool _closing;
        private Task? _finish;

        private StartupSplashHost(Action? onUserClosed)
        {
            _onUserClosed = onUserClosed;
            var thread = new Thread(Run) { Name = "GCN startup animation", IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>Returns after the real sample artwork has rendered, before any business initialization.</summary>
        public static async Task<StartupSplashHost> ShowAsync(Action? onUserClosed = null)
        {
            var host = new StartupSplashHost(onUserClosed);
            await host._shown.Task;
            return host;
        }

        private void Run()
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            // Fail only this startup operation if drawing throws; do not strand a dispatcher or awaiter.
            dispatcher.UnhandledException += (_, e) =>
            {
                _shown.TrySetException(e.Exception);
                _stopped.TrySetException(e.Exception);
                e.Handled = true;
                dispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
            };
            try
            {
                _window = new StartWindow();
                _window.Closed += (_, _) =>
                {
                    _shown.TrySetException(new OperationCanceledException("Startup window closed."));
                    if (!_closing) _onUserClosed?.Invoke();
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                };
                _window.Show();
                _ = NotifyFirstFrameAsync(_window);
                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                _shown.TrySetException(ex);
                _stopped.TrySetException(ex);
            }
            finally
            {
                _window?.Animation.Stop();
                if (_window?.IsVisible == true) _window.Close();
                _stopped.TrySetResult(true);
            }
        }

        private async Task NotifyFirstFrameAsync(StartWindow window)
        {
            try { await window.FirstFrame; _shown.TrySetResult(window); }
            catch (Exception ex) { _shown.TrySetException(ex); }
        }

        public void ReportProgress(double percent, string? status = null)
        {
            var window = _window;
            if (window == null || window.Dispatcher.HasShutdownStarted) return;
            window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => window.ReportProgress(percent, status)));
        }

        public void ReportProgress(double percent, StartupStatus status)
        {
            var window = _window;
            if (window == null || window.Dispatcher.HasShutdownStarted) return;
            window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => window.ReportProgress(percent, status)));
        }

        public Task FinishAsync() => _finish ??= FinishCoreAsync();

        private async Task FinishCoreAsync()
        {
            var window = await _shown.Task;
            if (window.Dispatcher.HasShutdownStarted) return;
            // Unwrap the task so completion waits for the fifth scene, not just message dispatch.
            Task finished = await window.Dispatcher.InvokeAsync(window.FinishAsync, DispatcherPriority.Background);
            await Task.WhenAny(finished, _stopped.Task);
            if (_stopped.Task.IsFaulted) await _stopped.Task;
        }

        public async Task CloseAsync()
        {
            var window = _window;
            if (window != null && !window.Dispatcher.HasShutdownStarted)
            {
                try { await window.Dispatcher.InvokeAsync(() => { _closing = true; window.Close(); }); }
                catch (TaskCanceledException) when (window.Dispatcher.HasShutdownStarted) { }
            }
            await _stopped.Task;
        }
    }
}
