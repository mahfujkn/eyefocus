using System;
using Microsoft.Win32;
using EyeFocus.Display;
using EyeFocus.Profiles;
using EyeFocus.Storage;

namespace EyeFocus.SystemIntegration
{
    public interface IPowerEventManager : IDisposable
    {
        void Initialize();
    }

    public class PowerEventManager : IPowerEventManager
    {
        private readonly IDisplayEngine _displayEngine;
        private readonly IProfileManager _profileManager;

        public PowerEventManager(IDisplayEngine displayEngine, IProfileManager profileManager)
        {
            _displayEngine = displayEngine;
            _profileManager = profileManager;
        }

        public void Initialize()
        {
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            LogService.Info("PowerEventManager initialized.");
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                LogService.Info("System resumed from sleep/suspend. Refreshing monitors and reapplying active profile.");
                _displayEngine.MonitorManager.RefreshMonitors();
                var activeProfile = _profileManager.GetActiveProfile();
                _displayEngine.ApplyProfile(activeProfile);
            }
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            LogService.Info("Display configuration changed event detected. Refreshing monitors and updating dimmer overlays.");
            _displayEngine.MonitorManager.RefreshMonitors();
            _displayEngine.SoftwareDimmer.UpdateMonitorsLayout();
            var activeProfile = _profileManager.GetActiveProfile();
            _displayEngine.ApplyProfile(activeProfile);

            // Guard against Windows Color Management / DWM resetting GPU gamma LUT after the event
            System.Threading.Tasks.Task.Delay(1200).ContinueWith(_ =>
            {
                try
                {
                    var p = _profileManager.GetActiveProfile();
                    _displayEngine.ApplyProfile(p);
                }
                catch { }
            });
        }

        public void Dispose()
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        }
    }
}
