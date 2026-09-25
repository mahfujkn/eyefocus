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
            set => SetProperty(ref _snackbarMessage, value);
        }

        public string? SnackbarActionText
        {
            get => _snackbarActionText;
            set => SetProperty(ref _snackbarActionText, value);
        }

        public ICommand NavigateCommand { get; }
        public ICommand OpenProfileEditorForActiveCommand { get; }
        public ICommand ManageMonitorsCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand DismissSnackbarCommand { get; }
        public ICommand ExecuteSnackbarActionCommand { get; }

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
                OnPropertyChanged(nameof(ThemeIconKey));
                OnPropertyChanged(nameof(ThemeTooltip));
            };
            OnPropertyChanged(nameof(CurrentTheme));
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
