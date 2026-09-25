using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using EyeFocus.Models;
using EyeFocus.Services;
using EyeFocus.Storage;
using EyeFocus.SystemIntegration;
using EyeFocus.UI.Themes;

namespace EyeFocus.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsStore _settingsStore;
        private readonly IStartupManager _startupManager;
        private readonly IHotkeyManager _hotkeyManager;

        private string _selectedTheme = "System";
        private bool _startWithWindows;
        private bool _startMinimized;
        private bool _minimizeToTray;
        private bool _rememberLastProfile;
        private bool _restoreOnExit;

        private bool _softwareDimFallback;
        private bool _ddcCiEnabled;
        private bool _gammaFallbackEnabled;
        private bool _debugLogging;
        private string _statusMessage = string.Empty;

        public ObservableCollection<HotkeyItemViewModel> Hotkeys { get; } = new();

        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (SetProperty(ref _selectedTheme, value))
                {
                    ThemeService.ApplyTheme(value);
                    SaveSettings();
                    SnackbarService.Instance.Show($"Theme set to {value}");
                }
            }
        }

        public bool IsThemeSystem
        {
            get => SelectedTheme == "System";
            set { if (value) SelectedTheme = "System"; }
        }

        public bool IsThemeLight
        {
            get => SelectedTheme == "Light";
            set { if (value) SelectedTheme = "Light"; }
        }

        public bool IsThemeDark
        {
            get => SelectedTheme == "Dark";
            set { if (value) SelectedTheme = "Dark"; }
        }

        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (SetProperty(ref _startWithWindows, value))
                {
                    _startupManager.SetStartWithWindows(value, StartMinimized);
                    SaveSettings();
                }
            }
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set { if (SetProperty(ref _startMinimized, value)) SaveSettings(); }
        }

        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set { if (SetProperty(ref _minimizeToTray, value)) SaveSettings(); }
        }

        public bool RememberLastProfile
        {
            get => _rememberLastProfile;
            set { if (SetProperty(ref _rememberLastProfile, value)) SaveSettings(); }
        }

        public bool RestoreOnExit
        {
            get => _restoreOnExit;
            set { if (SetProperty(ref _restoreOnExit, value)) SaveSettings(); }
        }

        public bool SoftwareDimFallback
        {
            get => _softwareDimFallback;
            set { if (SetProperty(ref _softwareDimFallback, value)) SaveSettings(); }
        }

        public bool DdcCiEnabled
        {
            get => _ddcCiEnabled;
            set { if (SetProperty(ref _ddcCiEnabled, value)) SaveSettings(); }
        }

        public bool GammaFallbackEnabled
        {
            get => _gammaFallbackEnabled;
            set { if (SetProperty(ref _gammaFallbackEnabled, value)) SaveSettings(); }
        }

        public bool DebugLogging
        {
            get => _debugLogging;
            set
            {
                if (SetProperty(ref _debugLogging, value))
                {
                    LogService.IsDebugEnabled = value;
                    SaveSettings();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand OpenLogFolderCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand ResetDefaultsCommand { get; }
        public ICommand ReloadHotkeysCommand { get; }

        public SettingsViewModel(
            ISettingsStore settingsStore,
            IStartupManager startupManager,
            IHotkeyManager hotkeyManager)
        {
            _settingsStore = settingsStore;
            _startupManager = startupManager;
            _hotkeyManager = hotkeyManager;

            OpenLogFolderCommand = new RelayCommand(OnOpenLogFolder);
            ClearLogsCommand = new RelayCommand(OnClearLogs);
            ResetDefaultsCommand = new RelayCommand(OnResetDefaults);
            ReloadHotkeysCommand = new RelayCommand(OnReloadHotkeys);

            _settingsStore.SettingsChanged += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() => LoadSettings());
            };

            LoadSettings();
        }

        public void LoadSettings()
        {
            var s = _settingsStore.Load();

            _selectedTheme = s.Theme ?? "System";
            OnPropertyChanged(nameof(SelectedTheme));
            OnPropertyChanged(nameof(IsThemeSystem));
            OnPropertyChanged(nameof(IsThemeLight));
            OnPropertyChanged(nameof(IsThemeDark));

            _startWithWindows = s.StartWithWindows;
            if (!_startWithWindows && _startupManager.IsStartWithWindowsEnabled())
            {
                _startupManager.SetStartWithWindows(false);
            }
            OnPropertyChanged(nameof(StartWithWindows));

            _startMinimized = s.StartMinimized;
            OnPropertyChanged(nameof(StartMinimized));

            _minimizeToTray = s.MinimizeToTray;
            OnPropertyChanged(nameof(MinimizeToTray));

            _rememberLastProfile = s.RememberLastProfile;
            OnPropertyChanged(nameof(RememberLastProfile));

            _restoreOnExit = s.RestoreOnExit;
            OnPropertyChanged(nameof(RestoreOnExit));

            _softwareDimFallback = s.SoftwareDimFallback;
            OnPropertyChanged(nameof(SoftwareDimFallback));

            _ddcCiEnabled = s.DdcCiEnabled;
            OnPropertyChanged(nameof(DdcCiEnabled));

            _gammaFallbackEnabled = s.GammaFallbackEnabled;
            OnPropertyChanged(nameof(GammaFallbackEnabled));

            _debugLogging = s.DebugLogging;
            OnPropertyChanged(nameof(DebugLogging));

            PopulateHotkeys();
        }

        private void PopulateHotkeys()
        {
            Hotkeys.Clear();
            var s = _settingsStore.Load();
            foreach (var b in s.Hotkeys)
            {
                Hotkeys.Add(new HotkeyItemViewModel(b));
            }
        }

        private void SaveSettings()
        {
            _settingsStore.Update(s =>
            {
                s.Theme = SelectedTheme;
                s.StartWithWindows = StartWithWindows;
                s.StartMinimized = StartMinimized;
                s.MinimizeToTray = MinimizeToTray;
                s.RememberLastProfile = RememberLastProfile;
                s.RestoreOnExit = RestoreOnExit;
                s.SoftwareDimFallback = SoftwareDimFallback;
                s.DdcCiEnabled = DdcCiEnabled;
                s.GammaFallbackEnabled = GammaFallbackEnabled;
                s.DebugLogging = DebugLogging;
            });
        }

        private void OnOpenLogFolder()
        {
            try
            {
                var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EyeFocus", "Logs");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                Process.Start(new ProcessStartInfo { FileName = logDir, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                SnackbarService.Instance.Show($"Could not open logs folder: {ex.Message}");
            }
        }

        private void OnClearLogs()
        {
            LogService.ClearLogs();
            SnackbarService.Instance.Show("Logs cleared successfully");
        }

        private void OnResetDefaults()
        {
            _settingsStore.ResetToDefaults();
            LoadSettings();
            SnackbarService.Instance.Show("Settings reset to defaults");
        }

        private void OnReloadHotkeys()
        {
            _hotkeyManager.ReloadHotkeys();
            PopulateHotkeys();
            SnackbarService.Instance.Show("Global shortcuts reloaded");
        }
    }

    public class HotkeyItemViewModel : ViewModelBase
    {
        public HotkeyBinding Binding { get; }

        public string ActionName => FormatActionName(Binding.Action);
        public string DisplayShortcut => Binding.DisplayShortcut;

        public bool IsEnabled
        {
            get => Binding.IsEnabled;
            set
            {
                if (Binding.IsEnabled != value)
                {
                    Binding.IsEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public HotkeyItemViewModel(HotkeyBinding binding)
        {
            Binding = binding;
        }

        private static string FormatActionName(HotkeyAction action) => action switch
        {
            HotkeyAction.BrightnessUp => "Increase Brightness (+5%)",
            HotkeyAction.BrightnessDown => "Decrease Brightness (-5%)",
            HotkeyAction.TemperatureWarmer => "Increase Warmth (-250K)",
            HotkeyAction.TemperatureCooler => "Decrease Warmth (+250K)",
            HotkeyAction.NextProfile => "Switch to Next Profile",
            HotkeyAction.PreviousProfile => "Switch to Previous Profile",
            HotkeyAction.ToggleNightMode => "Quick Toggle Night Mode",
            HotkeyAction.ToggleAutoMode => "Quick Toggle Day/Night Mode",
            HotkeyAction.ResetDisplay => "Reset Display to Default",
            _ => action.ToString()
        };
    }
}
