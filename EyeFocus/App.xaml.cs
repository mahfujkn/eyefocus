using System;
using System.Linq;
using System.Windows;
using EyeFocus.Automation;
using EyeFocus.Display;
using EyeFocus.Profiles;
using EyeFocus.Safety;
using EyeFocus.Storage;
using EyeFocus.SystemIntegration;
using EyeFocus.ViewModels;

namespace EyeFocus
{
    public partial class App : System.Windows.Application
    {
        private ISettingsStore? _settingsStore;
        private IDisplayEngine? _displayEngine;
        private ISafetyManager? _safetyManager;
        private ITrayManager? _trayManager;
        private IPowerEventManager? _powerEventManager;
        private IHotkeyManager? _hotkeyManager;
        private IAutoDayNightService? _autoDayNightService;
        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Register global unhandled exception handlers
            DispatcherUnhandledException += (s, ev) =>
            {
                LogService.Error("Unhandled Dispatcher Exception", ev.Exception);
                ev.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                if (ev.ExceptionObject is Exception ex)
                {
                    LogService.Error("Unhandled AppDomain Exception", ex);
                }
            };

            try
            {
                LogService.Info("EyeFocus starting up with Material 3 Expressive UI...");

                // 1. Storage & Config
                _settingsStore = new SettingsStore();
                var settings = _settingsStore.Load();

                // 2. Display Engine
                var capabilityDetector = new CapabilityDetector();
                var monitorManager = new MonitorManager(capabilityDetector);
                var ddcCiController = new DdcCiController();
                var wmiBrightnessController = new WmiBrightnessController();
                var hardwareColorController = new HardwareColorController(ddcCiController);
                var gammaController = new GammaController();
                var softwareDimmer = new SoftwareDimmer(monitorManager);

                _displayEngine = new DisplayEngine(
                    monitorManager,
                    softwareDimmer,
                    gammaController,
                    ddcCiController,
                    wmiBrightnessController,
                    hardwareColorController,
                    _settingsStore);

                // 3. Safety Manager
                _safetyManager = new SafetyManager(_displayEngine, _settingsStore);
                _safetyManager.Initialize();

                // 4. Profiles Engine
                var profileManager = new ProfileManager(_settingsStore);

                // 5. Automatic Day/Night Background Service
                _autoDayNightService = new AutoDayNightService(profileManager, _displayEngine, _settingsStore);
                _autoDayNightService.Initialize();

                // 6. System Integration
                var startupManager = new StartupManager();
                if (!settings.StartWithWindows && startupManager.IsStartWithWindowsEnabled())
                {
                    startupManager.SetStartWithWindows(false);
                }
                _hotkeyManager = new HotkeyManager(_settingsStore);
                _powerEventManager = new PowerEventManager(_displayEngine, profileManager);
                _powerEventManager.Initialize();

                _trayManager = new TrayManager(profileManager, _settingsStore);
                _trayManager.Initialize();

                // 7. ViewModels & Window
                var mainViewModel = new MainViewModel(
                    _displayEngine,
                    profileManager,
                    _hotkeyManager,
                    _settingsStore,
                    startupManager,
                    _autoDayNightService);

                _mainWindow = new MainWindow(mainViewModel, _settingsStore, _hotkeyManager);

                // Wire Tray events with Dispatcher invocation
                _trayManager.OpenRequested += (s, ev) =>
                {
                    Dispatcher.Invoke(() => _mainWindow.ShowAndRestore());
                };
                _trayManager.SettingsRequested += (s, ev) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        mainViewModel.CurrentTab = "Settings";
                        _mainWindow.ShowAndRestore();
                    });
                };
                _trayManager.ExitRequested += (s, ev) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        LogService.Info("Exit requested from tray context menu.");
                        _mainWindow?.ForceExit();
                        Shutdown();
                    });
                };

                // Check start minimized argument
                bool startMinimized = settings.StartMinimized || e.Args.Contains("--minimized");
                if (!startMinimized)
                {
                    _mainWindow.Show();
                }

                LogService.Info("EyeFocus startup sequence completed.");
            }
            catch (Exception ex)
            {
                LogService.Error("Critical failure during EyeFocus startup", ex);
                System.Windows.MessageBox.Show($"EyeFocus encountered an initialization error: {ex.Message}\n\nCheck logs for details.", "EyeFocus Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogService.Info("EyeFocus exiting...");
            try
            {
                _autoDayNightService?.Dispose();
                _trayManager?.Dispose();
                _hotkeyManager?.Dispose();
                _powerEventManager?.Dispose();
                _safetyManager?.Dispose();
            }
            catch (Exception ex)
            {
                LogService.Error("Error during shutdown cleanup", ex);
            }
            base.OnExit(e);
        }
    }
}
