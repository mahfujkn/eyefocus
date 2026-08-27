using System;
using Microsoft.Win32;
using EyeFocus.Display;
using EyeFocus.Models;
using EyeFocus.Profiles;
using EyeFocus.Storage;

namespace EyeFocus.Automation
{
    public class AutoDayNightService : IAutoDayNightService
    {
        private readonly IProfileManager _profileManager;
        private readonly IDisplayEngine _displayEngine;
        private readonly ISettingsStore _settingsStore;

        private System.Threading.Timer? _evaluationTimer;
        private readonly object _lock = new();
        private bool _isDisposed;
        private bool _isEnabled;
        private DayNightPeriod? _lastAppliedPeriod;

        public event Action<DayNightPeriod>? PeriodChanged;

        public bool IsEnabled => _isEnabled;
        public DayNightPeriod CurrentPeriod => _lastAppliedPeriod ?? DayNightScheduleCalculator.CalculateCurrentPeriod(_settingsStore.Load());

        public AutoDayNightService(
            IProfileManager profileManager,
            IDisplayEngine displayEngine,
            ISettingsStore settingsStore)
        {
            _profileManager = profileManager;
            _displayEngine = displayEngine;
            _settingsStore = settingsStore;
            _isEnabled = settingsStore.Load().AutomaticDayNightEnabled;
        }

        public void Initialize()
        {
            lock (_lock)
            {
                _isEnabled = _settingsStore.Load().AutomaticDayNightEnabled;

                // Hook Windows System Events for time changes & power wake
                try
                {
                    SystemEvents.TimeChanged += OnSystemTimeChanged;
                    SystemEvents.PowerModeChanged += OnPowerModeChanged;
                }
                catch (Exception ex)
                {
                    LogService.Error("Failed to hook SystemEvents in AutoDayNightService", ex);
                }

                // Setup 30-second lightweight evaluation timer
                _evaluationTimer = new System.Threading.Timer(OnTimerTick, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30));

                Evaluate(forceApply: false);
            }
        }

        public void SetEnabled(bool enabled)
        {
            lock (_lock)
            {
                _isEnabled = enabled;
                var settings = _settingsStore.Load();
                if (settings.AutomaticDayNightEnabled != enabled)
                {
                    settings.AutomaticDayNightEnabled = enabled;
                    _settingsStore.Save(settings);
                }

                if (enabled)
                {
                    Evaluate(forceApply: true);
                }
            }
        }

        public void Evaluate(bool forceApply = false)
        {
            lock (_lock)
            {
                if (_isDisposed) return;

                var settings = _settingsStore.Load();
                if (!settings.AutomaticDayNightEnabled && !forceApply)
                {
                    return;
                }

                var calculatedPeriod = DayNightScheduleCalculator.CalculateCurrentPeriod(settings);
                string targetProfileId = (calculatedPeriod == DayNightPeriod.Day)
                    ? settings.DayProfileId
                    : settings.NightProfileId;

                var activeProfile = _profileManager.GetActiveProfile();

                // Only apply when the state changed, or when forceApply is requested
                if (forceApply || _lastAppliedPeriod != calculatedPeriod || (activeProfile != null && activeProfile.Id != targetProfileId))
                {
                    _lastAppliedPeriod = calculatedPeriod;
                    LogService.Info($"Auto Day/Night transitioning to: {calculatedPeriod} (Profile: {targetProfileId})");

                    var targetProfile = _profileManager.GetProfile(targetProfileId);
                    if (targetProfile != null)
                    {
                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            _profileManager.SetActiveProfile(targetProfileId);
                            _displayEngine.ApplyProfile(targetProfile);
                        });
                    }

                    PeriodChanged?.Invoke(calculatedPeriod);
                }
            }
        }

        private void OnTimerTick(object? state)
        {
            try
            {
                Evaluate(forceApply: false);
            }
            catch (Exception ex)
            {
                LogService.Error("Error during AutoDayNight timer tick", ex);
            }
        }

        private void OnSystemTimeChanged(object? sender, EventArgs e)
        {
            LogService.Info("System time change detected in AutoDayNightService. Re-evaluating schedule...");
            Evaluate(forceApply: true);
        }

        private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                LogService.Info("System resume from sleep/hibernate detected. Re-evaluating Auto Day/Night...");
                Evaluate(forceApply: true);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_isDisposed) return;
                _isDisposed = true;

                try
                {
                    SystemEvents.TimeChanged -= OnSystemTimeChanged;
                    SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                }
                catch { }

                _evaluationTimer?.Dispose();
                _evaluationTimer = null;
            }
        }
    }
}
