using System;
using EyeFocus.Display;
using EyeFocus.Storage;

namespace EyeFocus.Safety
{
    public class SafetyManager : ISafetyManager
    {
        private readonly IDisplayEngine _displayEngine;
        private readonly ISettingsStore _settingsStore;
        private bool _isDisposed = false;

        public SafetyManager(IDisplayEngine displayEngine, ISettingsStore settingsStore)
        {
            _displayEngine = displayEngine;
            _settingsStore = settingsStore;
        }

        public void Initialize()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => HandleProcessExit();
            AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleUnhandledException(e);
            LogService.Info("SafetyManager initialized.");
        }

        private void HandleProcessExit()
        {
            try
            {
                var settings = _settingsStore.Load();
                if (settings.RestoreOnExit)
                {
                    _displayEngine.RestoreInitialState();
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error during safety process exit cleanup", ex);
            }
        }

        private void HandleUnhandledException(UnhandledExceptionEventArgs e)
        {
            try
            {
                LogService.Error($"Unhandled exception trapped in SafetyManager: {e.ExceptionObject}");
                _displayEngine.RestoreInitialState();
            }
            catch
            {
                // Best-effort cleanup on crash
            }
        }

        public void SafeExit()
        {
            HandleProcessExit();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                SafeExit();
            }
        }
    }
}
