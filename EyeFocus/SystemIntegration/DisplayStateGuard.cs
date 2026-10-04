using System;
using System.Threading;
using Microsoft.Win32;
using EyeFocus.Display;
using EyeFocus.Models;
using EyeFocus.Profiles;
using EyeFocus.Storage;

namespace EyeFocus.SystemIntegration
{
    public interface IDisplayStateGuard : IDisposable
    {
        void Initialize();
        void NotifySettingsOpened();
        void EnforceActiveProfile();
    }

    public class DisplayStateGuard : IDisplayStateGuard
    {
        private readonly IDisplayEngine _displayEngine;
        private readonly IProfileManager _profileManager;
        private readonly ISettingsStore _settingsStore;
        private System.Threading.Timer? _guardTimer;
        private int _fastGuardTicksRemaining;
        private readonly object _syncLock = new();
        private bool _isDisposed;

        public DisplayStateGuard(
            IDisplayEngine displayEngine,
            IProfileManager profileManager,
            ISettingsStore settingsStore)
        {
            _displayEngine = displayEngine;
            _profileManager = profileManager;
            _settingsStore = settingsStore;
        }

        public void Initialize()
        {
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            // Run watchdog every 1000ms
            _guardTimer = new System.Threading.Timer(OnTimerTick, null, 1000, 1000);
            LogService.Info("DisplayStateGuard initialized (active gamma watchdog & auto-recovery enabled).");
        }

        public void NotifySettingsOpened()
        {
            lock (_syncLock)
            {
                // Run fast-recovery checks (every 300ms for ~6 seconds = 20 ticks)
                _fastGuardTicksRemaining = 20;
                _guardTimer?.Change(100, 300);
            }
            LogService.Info("DisplayStateGuard entered fast-recovery mode for Windows Display Settings.");
        }

        public void EnforceActiveProfile()
        {
            try
            {
                var active = _profileManager.GetActiveProfile();
                if (active != null)
                {
                    _displayEngine.ApplyProfile(active);
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"Error enforcing active profile: {ex.Message}");
            }
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            LogService.Info("Windows display configuration change event received. Triggering fast recovery.");
            NotifySettingsOpened();
        }

        private void OnTimerTick(object? state)
        {
            if (_isDisposed) return;

            try
            {
                CheckAndRestoreIfNeeded();
            }
            catch (Exception ex)
            {
                LogService.Debug($"DisplayStateGuard tick error: {ex.Message}");
            }
            finally
            {
                lock (_syncLock)
                {
                    if (_fastGuardTicksRemaining > 0)
                    {
                        _fastGuardTicksRemaining--;
                        if (_fastGuardTicksRemaining == 0)
                        {
                            // Return to standard 1000ms interval
                            _guardTimer?.Change(1000, 1000);
                        }
                    }
                }
            }
        }

        private void CheckAndRestoreIfNeeded()
        {
            var active = _profileManager.GetActiveProfile();
            if (active == null) return;

            var monitors = _displayEngine.MonitorManager.GetMonitors();
            foreach (var monitor in monitors)
            {
                if (monitor.SupportsGamma && _displayEngine.GammaController.IsGammaRampReset(monitor))
                {
                    LogService.Info($"External gamma reset detected on [{monitor.FriendlyName}]. Auto-restoring profile [{active.Name}]...");
                    _displayEngine.ApplyProfile(active, monitor.StableMonitorId);
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _guardTimer?.Dispose();
        }
    }
}
