using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EyeFocus.Automation;
using EyeFocus.Display;
using EyeFocus.Models;
using EyeFocus.Profiles;
using EyeFocus.Services;
using EyeFocus.Storage;
using EyeFocus.SystemIntegration;
using EyeFocus.UI.Themes;

namespace EyeFocus.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public DisplayDashboardViewModel DashboardVM { get; }
        public ProfileEditorViewModel ProfileEditorVM { get; }
        public AutoDayNightConfigViewModel AutoDayNightConfigVM { get; }
        public MonitorsViewModel MonitorsVM { get; }
        public SettingsViewModel SettingsVM { get; }
        public AboutViewModel AboutVM { get; }

        private readonly IDisplayEngine _displayEngine;
        private readonly IProfileManager _profileManager;
        private readonly IHotkeyManager _hotkeyManager;
        private readonly ISettingsStore _settingsStore;
        private readonly IAutoDayNightService _autoDayNightService;

        private string _currentTab = "Dashboard"; // "Dashboard", "Monitors", "Settings", "Privacy", "About"
        private string _selectedMonitorId = "ALL";
        private string _currentTheme = "System";

        // Snackbar State
        private bool _isSnackbarVisible;
        private string _snackbarMessage = string.Empty;
        private string? _snackbarActionText;
        private Action? _snackbarActionCallback;

        public ObservableCollection<MonitorSelectorItem> MonitorSelectorItems { get; } = new();
        public string AppVersion => "Version 1.1.0";

        public string CurrentTab
        {
            get => _currentTab;
            set => SetProperty(ref _currentTab, value);
        }

        public string SelectedMonitorId
        {
            get => _selectedMonitorId;
            set
            {
                if (SetProperty(ref _selectedMonitorId, value))
                {
                    OnMonitorSelectionChanged(value);
                }
            }
        }

        public string CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (SetProperty(ref _currentTheme, value))
                {
                    OnPropertyChanged(nameof(IsDarkTheme));
                    OnPropertyChanged(nameof(ThemeIconKey));
                    OnPropertyChanged(nameof(ThemeTooltip));
                }
            }
        }

        public bool IsDarkTheme
        {
            get => string.Equals(ThemeService.ActiveTheme, "Dark", StringComparison.OrdinalIgnoreCase);
            set
            {
                var targetTheme = value ? "Dark" : "Light";
                if (!string.Equals(ThemeService.ActiveTheme, targetTheme, StringComparison.OrdinalIgnoreCase))
                {
                    CurrentTheme = targetTheme;
                    ThemeService.ApplyTheme(targetTheme);
                    _settingsStore.Update(s => s.Theme = targetTheme);
                    SnackbarService.Instance.Show($"Switched to {targetTheme} theme");
                    OnPropertyChanged(nameof(IsDarkTheme));
                    OnPropertyChanged(nameof(ThemeIconKey));
                    OnPropertyChanged(nameof(ThemeTooltip));
                }
            }
        }

        public string ThemeIconKey => ThemeService.ActiveTheme == "Dark" ? "IconSun" : "IconMoon";
        public string ThemeTooltip => ThemeService.ActiveTheme == "Dark" ? "Switch to Light Mode" : "Switch to Dark Mode";

        public bool IsSnackbarVisible
        {
            get => _isSnackbarVisible;
            set => SetProperty(ref _isSnackbarVisible, value);
        }

        public string SnackbarMessage
        {
            get => _snackbarMessage;
            set
            {
                if (SetProperty(ref _snackbarMessage, value))
                {
                    OnPropertyChanged(nameof(SnackbarIconKey));
                    OnPropertyChanged(nameof(SnackbarIconBrush));
                }
            }
        }

        public string SnackbarIconKey
        {
            get
            {
                var msg = _snackbarMessage?.ToLowerInvariant() ?? "";
                if (msg.Contains("light theme")) return "IconSun";
                if (msg.Contains("dark theme")) return "IconMoon";
                if (msg.Contains("pause")) return "IconPause";
                if (msg.Contains("resumed")) return "IconPlay";
                if (msg.Contains("dim")) return "IconScreenDim";
                if (msg.Contains("comfort")) return "IconLeaf";
                if (msg.Contains("reading")) return "IconBook";
                if (msg.Contains("game")) return "IconGame";
                if (msg.Contains("movie")) return "IconMovie";
                if (msg.Contains("office")) return "IconBriefcase";
                if (msg.Contains("editing")) return "IconPalette";
                if (msg.Contains("coding")) return "IconCode";
                if (msg.Contains("night")) return "IconNight";
                if (msg.Contains("exercise")) return "IconEyeExercise";
                if (msg.Contains("applied") || msg.Contains("saved") || msg.Contains("enabled")) return "IconCheck";
                return "IconInfo";
            }
        }

        private static readonly System.Windows.Media.Brush AmberBrush = CreateFrozenBrush(0xF5, 0x9E, 0x0B);
        private static readonly System.Windows.Media.Brush CyanBrush = CreateFrozenBrush(0x38, 0xBD, 0xF8);
        private static readonly System.Windows.Media.Brush GreenBrush = CreateFrozenBrush(0x10, 0xB9, 0x81);
        private static readonly System.Windows.Media.Brush IndigoBrush = CreateFrozenBrush(0x63, 0x66, 0xF1);
        private static readonly System.Windows.Media.Brush PurpleBrush = CreateFrozenBrush(0xA8, 0x55, 0xF7);
        private static readonly System.Windows.Media.Brush RoseBrush = CreateFrozenBrush(0xF4, 0x3F, 0x5E);

        private static System.Windows.Media.Brush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        public System.Windows.Media.Brush SnackbarIconBrush
        {
            get
            {
                var msg = _snackbarMessage?.ToLowerInvariant() ?? "";
                if (msg.Contains("light theme")) return AmberBrush;
                if (msg.Contains("dark theme")) return CyanBrush;
                if (msg.Contains("comfort") || msg.Contains("reading") || msg.Contains("applied") || msg.Contains("saved")) return GreenBrush;
                if (msg.Contains("coding")) return IndigoBrush;
                if (msg.Contains("game")) return PurpleBrush;
                if (msg.Contains("movie")) return RoseBrush;
                if (msg.Contains("dim")) return CyanBrush;
                return AmberBrush;
            }
        }

        public string? SnackbarActionText
        {
            get => _snackbarActionText;
            set => SetProperty(ref _snackbarActionText, value);
        }

        public EyeExerciseViewModel EyeExerciseVM { get; } = new();

        private readonly System.Windows.Threading.DispatcherTimer _pauseTimer;
        private int _pauseSecondsRemaining = 0;
        private string _pause20MinText = "Pause for 20 minutes";

        public string Pause20MinText
        {
            get => _pause20MinText;
            set => SetProperty(ref _pause20MinText, value);
        }

        public ICommand NavigateCommand { get; }
        public ICommand OpenProfileEditorForActiveCommand { get; }
        public ICommand ManageMonitorsCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand DismissSnackbarCommand { get; }
        public ICommand ExecuteSnackbarActionCommand { get; }
        public ICommand Pause20MinCommand { get; }
        public ICommand EyeExerciseCommand { get; }
        public ICommand ScreenDimCommand { get; }
        public ICommand FocusModeCommand { get; }

        public MainViewModel(
            IDisplayEngine displayEngine,
            IProfileManager profileManager,
            IHotkeyManager hotkeyManager,
            ISettingsStore settingsStore,
            IStartupManager startupManager,
            IAutoDayNightService autoDayNightService)
        {
            _displayEngine = displayEngine;
            _profileManager = profileManager;
            _hotkeyManager = hotkeyManager;
            _settingsStore = settingsStore;
            _autoDayNightService = autoDayNightService;

            DashboardVM = new DisplayDashboardViewModel(_displayEngine, _profileManager, _settingsStore, _autoDayNightService);
            ProfileEditorVM = new ProfileEditorViewModel(_profileManager, _displayEngine);
            AutoDayNightConfigVM = new AutoDayNightConfigViewModel(_settingsStore, _autoDayNightService);
            MonitorsVM = new MonitorsViewModel(_displayEngine, _profileManager);
            SettingsVM = new SettingsViewModel(_settingsStore, startupManager, _hotkeyManager);
            AboutVM = new AboutViewModel();

            DashboardVM.RequestOpenProfileEditor += OpenProfileEditorForActive;
            DashboardVM.RequestEditProfile += profile => ProfileEditorVM.OpenForProfile(profile);
            DashboardVM.RequestOpenAutoDayNightConfig += () => AutoDayNightConfigVM.Open();
            AutoDayNightConfigVM.ConfigurationSaved += () => DashboardVM.LoadState();

            NavigateCommand = new RelayCommand<string>(tab => CurrentTab = tab ?? "Dashboard");
            OpenProfileEditorForActiveCommand = new RelayCommand(OpenProfileEditorForActive);
            ManageMonitorsCommand = new RelayCommand(() => CurrentTab = "Monitors");
            ToggleThemeCommand = new RelayCommand(OnToggleTheme);
            DismissSnackbarCommand = new RelayCommand(() => IsSnackbarVisible = false);
            ExecuteSnackbarActionCommand = new RelayCommand(() =>
            {
                _snackbarActionCallback?.Invoke();
                IsSnackbarVisible = false;
            });

            _pauseTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _pauseTimer.Tick += OnPauseTimerTick;

            Pause20MinCommand = new RelayCommand(OnTogglePause20Min);
            EyeExerciseCommand = new RelayCommand(() => EyeExerciseVM.Open());
            ScreenDimCommand = new RelayCommand(OnToggleScreenDim);
            FocusModeCommand = new RelayCommand(OnToggleFocusMode);

            // Wire Snackbar Service
            SnackbarService.Instance.MessageRequested += (msg, actionText, actionCb) =>
            {
                SnackbarMessage = msg;
                SnackbarActionText = actionText;
                _snackbarActionCallback = actionCb;
                IsSnackbarVisible = true;
            };
            SnackbarService.Instance.DismissRequested += () =>
            {
                IsSnackbarVisible = false;
            };

            _displayEngine.MonitorManager.MonitorsChanged += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(PopulateMonitorSelector);
            };
            _hotkeyManager.HotkeyTriggered += OnHotkeyTriggered;

            PopulateMonitorSelector();
            InitializeTheme();
            InitializeDisplayState();
        }

        private void InitializeTheme()
        {
            var settings = _settingsStore.Load();
            _currentTheme = settings.Theme ?? "System";
            ThemeService.ApplyTheme(_currentTheme);
            ThemeService.ThemeChanged += active =>
            {
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(ThemeIconKey));
                OnPropertyChanged(nameof(ThemeTooltip));
            };
            OnPropertyChanged(nameof(CurrentTheme));
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(ThemeIconKey));
            OnPropertyChanged(nameof(ThemeTooltip));
        }

        private void OnToggleTheme()
        {
            var nextTheme = ThemeService.ActiveTheme == "Dark" ? "Light" : "Dark";
            CurrentTheme = nextTheme;
            ThemeService.ApplyTheme(nextTheme);

            _settingsStore.Update(s => s.Theme = nextTheme);
            SnackbarService.Instance.Show($"Switched to {nextTheme} theme");
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(ThemeIconKey));
            OnPropertyChanged(nameof(ThemeTooltip));
        }

        private void InitializeDisplayState()
        {
            var settings = _settingsStore.Load();
            DisplayProfile activeProfile;
            if (settings.RememberLastProfile)
            {
                activeProfile = _profileManager.GetActiveProfile();
            }
            else
            {
                activeProfile = _profileManager.GetProfile(ProfileDefaults.IdComfort) 
                    ?? _profileManager.GetActiveProfile();
                _profileManager.SetActiveProfile(activeProfile.Id);
            }

            _displayEngine.ApplyProfile(activeProfile);
        }

        private void PopulateMonitorSelector()
        {
            MonitorSelectorItems.Clear();
            MonitorSelectorItems.Add(new MonitorSelectorItem { Id = "ALL", DisplayText = "All Displays" });

            var monitors = _displayEngine.MonitorManager.GetMonitors();
            foreach (var m in monitors)
            {
                string type = m.SupportsDdcCi ? "DDC/CI" : (m.IsInternal ? "WMI Laptop" : "Software");
                MonitorSelectorItems.Add(new MonitorSelectorItem
                {
                    Id = m.StableMonitorId,
                    DisplayText = $"{m.FriendlyName} ({type})"
                });
            }

            if (!MonitorSelectorItems.Any(i => i.Id == SelectedMonitorId))
            {
                SelectedMonitorId = "ALL";
            }
        }

        private void OnMonitorSelectionChanged(string monitorId)
        {
            DashboardVM.SelectedMonitorTitle = MonitorSelectorItems.FirstOrDefault(i => i.Id == monitorId)?.DisplayText ?? "All Displays";
            DashboardVM.UpdateStatus();
        }

        private void OpenProfileEditorForActive()
        {
            var currentProfile = _profileManager.GetActiveProfile();
            ProfileEditorVM.OpenForProfile(currentProfile);
        }

        private void OnTogglePause20Min()
        {
            if (_pauseSecondsRemaining > 0)
            {
                _pauseTimer.Stop();
                _pauseSecondsRemaining = 0;
                Pause20MinText = "Pause for 20 minutes";
                if (DashboardVM.IsPaused)
                {
                    DashboardVM.TogglePauseCommand.Execute(null);
                }
                SnackbarService.Instance.Show("EyeFocus resumed (Pause cancelled)");
            }
            else
            {
                if (!DashboardVM.IsPaused)
                {
                    DashboardVM.TogglePauseCommand.Execute(null);
                }
                _pauseSecondsRemaining = 20 * 60;
                Pause20MinText = "Paused (20:00)";
                _pauseTimer.Start();
                SnackbarService.Instance.Show("EyeFocus paused for 20 minutes");
            }
        }

        private void OnPauseTimerTick(object? sender, EventArgs e)
        {
            if (_pauseSecondsRemaining > 1)
            {
                _pauseSecondsRemaining--;
                int m = _pauseSecondsRemaining / 60;
                int s = _pauseSecondsRemaining % 60;
                Pause20MinText = $"Paused ({m:D2}:{s:D2})";
            }
            else
            {
                _pauseTimer.Stop();
                _pauseSecondsRemaining = 0;
                Pause20MinText = "Pause for 20 minutes";
                if (DashboardVM.IsPaused)
                {
                    DashboardVM.TogglePauseCommand.Execute(null);
                }
                SnackbarService.Instance.Show("20-minute pause completed. EyeFocus resumed!");
            }
        }

        private void OnToggleScreenDim()
        {
            if (DashboardVM.SoftwareDim > 0)
            {
                DashboardVM.SoftwareDim = 0;
                SnackbarService.Instance.Show("Screen dim disabled (0%)");
            }
            else
            {
                DashboardVM.SoftwareDim = 35;
                SnackbarService.Instance.Show("Screen dim enabled (35% software overlay)");
            }
        }

        private void OnToggleFocusMode()
        {
            var currentId = DashboardVM.ActiveProfile?.Id;
            if (currentId == ProfileDefaults.IdCoding)
            {
                DashboardVM.SelectProfileCommand.Execute(ProfileDefaults.IdComfort);
            }
            else
            {
                DashboardVM.SelectProfileCommand.Execute(ProfileDefaults.IdCoding);
            }
        }

        private void OnHotkeyTriggered(object? sender, HotkeyAction action)
        {
            switch (action)
            {
                case HotkeyAction.BrightnessUp:
                    DashboardVM.Brightness = Math.Min(100, DashboardVM.Brightness + 5);
                    break;
                case HotkeyAction.BrightnessDown:
                    DashboardVM.Brightness = Math.Max(0, DashboardVM.Brightness - 5);
                    break;
                case HotkeyAction.TemperatureWarmer:
                    DashboardVM.Kelvin = Math.Max(2500, DashboardVM.Kelvin - 250);
                    break;
                case HotkeyAction.TemperatureCooler:
                    DashboardVM.Kelvin = Math.Min(7500, DashboardVM.Kelvin + 250);
                    break;
                case HotkeyAction.NextProfile:
                    CycleProfile(1);
                    break;
                case HotkeyAction.PreviousProfile:
                    CycleProfile(-1);
                    break;
                case HotkeyAction.ToggleNightMode:
                    var settings = _settingsStore.Load();
                    DashboardVM.SelectProfileCommand.Execute(settings.NightProfileId);
                    break;
                case HotkeyAction.ToggleAutoMode:
                    // Toggle between Day and Night quick presets
                    var s = _settingsStore.Load();
                    var current = _profileManager.GetActiveProfile();
                    if (current.Id == s.NightProfileId)
                    {
                        DashboardVM.SelectProfileCommand.Execute(s.DayProfileId);
                    }
                    else
                    {
                        DashboardVM.SelectProfileCommand.Execute(s.NightProfileId);
                    }
                    break;
                case HotkeyAction.ResetDisplay:
                    DashboardVM.RestoreDisplayCommand.Execute(null);
                    break;
            }
        }

        private void CycleProfile(int step)
        {
            var all = _profileManager.GetAllProfiles();
            if (all.Count == 0) return;

            var current = _profileManager.GetActiveProfile();
            int index = all.ToList().FindIndex(p => p.Id == current.Id);
            if (index == -1) index = 0;

            int nextIndex = (index + step + all.Count) % all.Count;
            var nextProfile = all[nextIndex];
            _profileManager.SetActiveProfile(nextProfile.Id);
            _displayEngine.ApplyProfile(nextProfile);
            SnackbarService.Instance.Show($"{nextProfile.Name} profile applied");
        }
    }

    public class MonitorSelectorItem
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
    }
}
