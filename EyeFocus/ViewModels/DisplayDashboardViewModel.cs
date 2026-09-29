using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using EyeFocus.Automation;
using EyeFocus.Display;
using EyeFocus.Models;
using EyeFocus.Profiles;
using EyeFocus.Services;
using EyeFocus.Storage;
using EyeFocus.UI.Themes;

namespace EyeFocus.ViewModels
{
    public class DisplayDashboardViewModel : ViewModelBase
    {
        private readonly IDisplayEngine _displayEngine;
        private readonly IProfileManager _profileManager;
        private readonly ISettingsStore _settingsStore;
        private readonly IAutoDayNightService _autoDayNightService;

        private DisplayProfile _activeProfile;
        private int _kelvin;
        private int _brightness;
        private int _softwareDim;
        private bool _isUpdatingFromInternal;
        private bool _isPaused;

        private string _hardwareControlBadgeText = "DDC/CI Hardware";
        private string _hardwareStatusText = "DDC/CI ✓ • Gamma Fallback Ready";
        private string _selectedMonitorTitle = "All Displays";
        private string _selectedMonitorDisplayDetail = "Synchronized Control";

        private string _dayModeDetails = "5000K · 70%";
        private string _nightModeDetails = "3000K · 40%";
        private string _dayStartTimeDisplay = "06:00";
        private string _nightStartTimeDisplay = "21:00";

        private readonly System.Windows.Threading.DispatcherTimer? _timelineClockTimer;
        private string _currentTimeText = DateTime.Now.ToString("h:mm tt");
        private GridLength _timelineLeftWeight = new(50, GridUnitType.Star);
        private GridLength _timelineRightWeight = new(50, GridUnitType.Star);
        private GridLength _timelineCol0Weight = new(29, GridUnitType.Star);
        private GridLength _timelineCol1Weight = new(46, GridUnitType.Star);
        private GridLength _timelineCol2Weight = new(25, GridUnitType.Star);
        private Color _currentTimePinColor = Color.FromRgb(245, 158, 11);
        private Brush _currentTimePinBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));

        private bool _isDayModeActive;
        private bool _isNightModeActive;

        public ObservableCollection<ProfileCardItemViewModel> ProfileCards { get; } = new();

        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                if (SetProperty(ref _isPaused, value))
                {
                    OnPropertyChanged(nameof(PauseButtonText));
                    OnPropertyChanged(nameof(PauseButtonToolTip));
                    OnPropertyChanged(nameof(StatusPillText));
                    OnPropertyChanged(nameof(StatusPillDotBrush));
                }
            }
        }

        public string PauseButtonText => IsPaused ? "Resume" : "Pause";
        public string PauseButtonToolTip => IsPaused ? "Resume EyeFocus display effects" : "Temporarily pause EyeFocus display effects";
        public string StatusPillText => IsPaused ? "EyeFocus is paused" : "EyeFocus is active";
        public string StatusPillDotBrush => IsPaused ? "#F59E0B" : "#10B981";

        private bool _isAutomaticDayNightEnabled;
        public bool IsAutomaticDayNightEnabled
        {
            get => _isAutomaticDayNightEnabled;
            set
            {
                if (SetProperty(ref _isAutomaticDayNightEnabled, value))
                {
                    _autoDayNightService.SetEnabled(value);
                    OnPropertyChanged(nameof(AutoDayNightStatusText));
                    UpdateDayNightActiveStates();

                    if (value)
                    {
                        var period = _autoDayNightService.CurrentPeriod;
                        SnackbarService.Instance.Show($"Automatic Day/Night enabled ({period} Mode active)");
                    }
                    else
                    {
                        SnackbarService.Instance.Show("Automatic Day/Night disabled (Manual Mode active)");
                    }
                    OnPropertyChanged(nameof(ScheduleToggleText));
                }
            }
        }

        public string ScheduleToggleText => IsAutomaticDayNightEnabled ? "Schedule enabled" : "Schedule disabled";

        public string AutoDayNightStatusText
        {
            get
            {
                if (!IsAutomaticDayNightEnabled) return "Manual Selection";
                return _autoDayNightService.CurrentPeriod == DayNightPeriod.Day
                    ? "Auto: Day Mode active"
                    : "Auto: Night Mode active";
            }
        }

        public bool IsDayModeActive
        {
            get => _isDayModeActive;
            set => SetProperty(ref _isDayModeActive, value);
        }

        public bool IsNightModeActive
        {
            get => _isNightModeActive;
            set => SetProperty(ref _isNightModeActive, value);
        }

        public string DayStartTimeDisplay
        {
            get => _dayStartTimeDisplay;
            set
            {
                if (SetProperty(ref _dayStartTimeDisplay, value))
                {
                    UpdateTimelineColumnWeights();
                }
            }
        }

        public string NightStartTimeDisplay
        {
            get => _nightStartTimeDisplay;
            set
            {
                if (SetProperty(ref _nightStartTimeDisplay, value))
                {
                    UpdateTimelineColumnWeights();
                }
            }
        }

        public string CurrentTimeText
        {
            get => _currentTimeText;
            private set => SetProperty(ref _currentTimeText, value);
        }

        public GridLength TimelineLeftWeight
        {
            get => _timelineLeftWeight;
            private set => SetProperty(ref _timelineLeftWeight, value);
        }

        public GridLength TimelineRightWeight
        {
            get => _timelineRightWeight;
            private set => SetProperty(ref _timelineRightWeight, value);
        }

        public GridLength TimelineCol0Weight
        {
            get => _timelineCol0Weight;
            private set => SetProperty(ref _timelineCol0Weight, value);
        }

        public GridLength TimelineCol1Weight
        {
            get => _timelineCol1Weight;
            private set => SetProperty(ref _timelineCol1Weight, value);
        }

        public GridLength TimelineCol2Weight
        {
            get => _timelineCol2Weight;
            private set => SetProperty(ref _timelineCol2Weight, value);
        }

        public Color CurrentTimePinColor
        {
            get => _currentTimePinColor;
            private set => SetProperty(ref _currentTimePinColor, value);
        }

        public Brush CurrentTimePinBrush
        {
            get => _currentTimePinBrush;
            private set => SetProperty(ref _currentTimePinBrush, value);
        }

        public string DayTimeFormatted => FormatTime12H(DayStartTimeDisplay);
        public string NightTimeFormatted => FormatTime12H(NightStartTimeDisplay);
        public string DayScheduleRange => $"{DayTimeFormatted} – {NightTimeFormatted}";
        public string NightScheduleRange => $"{NightTimeFormatted} – {DayTimeFormatted}";

        public DisplayProfile ActiveProfile
        {
            get => _activeProfile;
            set => SetProperty(ref _activeProfile, value);
        }

        public int Kelvin
        {
            get => _kelvin;
            set
            {
                if (SetProperty(ref _kelvin, value) && !_isUpdatingFromInternal)
                {
                    if (IsPaused) IsPaused = false;
                    _displayEngine.SetColorTemperature(value);
                    if (ActiveProfile != null)
                    {
                        ActiveProfile.Kelvin = value;
                        ScheduleDebouncedProfileSave();
                    }
                }
            }
        }

        public int Brightness
        {
            get => _brightness;
            set
            {
                if (SetProperty(ref _brightness, value) && !_isUpdatingFromInternal)
                {
                    if (IsPaused) IsPaused = false;
                    _displayEngine.SetBrightness(value);
                    if (ActiveProfile != null)
                    {
                        ActiveProfile.Brightness = value;
                        ScheduleDebouncedProfileSave();
                    }
                }
            }
        }

        public int SoftwareDim
        {
            get => _softwareDim;
            set
            {
                if (SetProperty(ref _softwareDim, value) && !_isUpdatingFromInternal)
                {
                    if (IsPaused) IsPaused = false;
                    _displayEngine.SetSoftwareDim(value);
                    if (ActiveProfile != null)
                    {
                        ActiveProfile.SoftwareDim = value;
                        ScheduleDebouncedProfileSave();
                    }
                }
            }
        }

        public string HardwareControlBadgeText
        {
            get => _hardwareControlBadgeText;
            set => SetProperty(ref _hardwareControlBadgeText, value);
        }

        public string HardwareStatusText
        {
            get => _hardwareStatusText;
            set => SetProperty(ref _hardwareStatusText, value);
        }

        public string SelectedMonitorTitle
        {
            get => _selectedMonitorTitle;
            set => SetProperty(ref _selectedMonitorTitle, value);
        }

        public string SelectedMonitorDisplayDetail
        {
            get => _selectedMonitorDisplayDetail;
            set => SetProperty(ref _selectedMonitorDisplayDetail, value);
        }

        public string DayModeDetails
        {
            get => _dayModeDetails;
            set => SetProperty(ref _dayModeDetails, value);
        }

        public string NightModeDetails
        {
            get => _nightModeDetails;
            set => SetProperty(ref _nightModeDetails, value);
        }

        public ICommand SelectProfileCommand { get; }
        public ICommand SelectDayModeCommand { get; }
        public ICommand SelectNightModeCommand { get; }
        public ICommand ConfigureAutoDayNightCommand { get; }
        public ICommand TogglePauseCommand { get; }
        public ICommand CreateCustomProfileCommand { get; }
        public ICommand RestoreDisplayCommand { get; }
        public ICommand ResetToProfileDefaultCommand { get; }
        public ICommand ResetKelvinCommand { get; }
        public ICommand ResetBrightnessCommand { get; }
        public ICommand EditProfileCommand { get; }
        public ICommand DuplicateProfileCommand { get; }

        public event Action? RequestOpenProfileEditor;
        public event Action? RequestOpenAutoDayNightConfig;
        public event Action<DisplayProfile>? RequestEditProfile;

        private readonly System.Windows.Threading.DispatcherTimer _profileSaveDebounceTimer;

        public void TriggerOpenProfileEditor() => RequestOpenProfileEditor?.Invoke();

        public DisplayDashboardViewModel(
            IDisplayEngine displayEngine,
            IProfileManager profileManager,
            ISettingsStore settingsStore,
            IAutoDayNightService autoDayNightService)
        {
            _displayEngine = displayEngine;
            _profileManager = profileManager;
            _settingsStore = settingsStore;
            _autoDayNightService = autoDayNightService;

            _profileSaveDebounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(350)
            };
            _profileSaveDebounceTimer.Tick += (s, e) =>
            {
                _profileSaveDebounceTimer.Stop();
                if (ActiveProfile != null)
                {
                    _profileManager.SaveProfile(ActiveProfile);
                }
            };

            _activeProfile = _profileManager.GetActiveProfile();

            SelectProfileCommand = new RelayCommand<string>(OnSelectProfile);
            SelectDayModeCommand = new RelayCommand(OnSelectDayMode);
            SelectNightModeCommand = new RelayCommand(OnSelectNightMode);
            ConfigureAutoDayNightCommand = new RelayCommand(() => RequestOpenAutoDayNightConfig?.Invoke());
            TogglePauseCommand = new RelayCommand(OnTogglePause);
            CreateCustomProfileCommand = new RelayCommand(TriggerOpenProfileEditor);
            RestoreDisplayCommand = new RelayCommand(OnRestoreDisplay);
            ResetToProfileDefaultCommand = new RelayCommand(OnResetToProfileDefault);
            ResetKelvinCommand = new RelayCommand(OnResetKelvin);
            ResetBrightnessCommand = new RelayCommand(OnResetBrightness);
            EditProfileCommand = new RelayCommand<string>(OnEditProfile);
            DuplicateProfileCommand = new RelayCommand<string>(OnDuplicateProfile);

            _profileManager.ActiveProfileChanged += (s, p) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    SyncFromProfile(p);
                    if (!IsPaused)
                    {
                        _displayEngine.ApplyProfile(p);
                    }
                    UpdateDayNightActiveStates();
                });
            };

            _profileManager.ProfilesListChanged += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() => RefreshProfilesList());
            };

            _autoDayNightService.PeriodChanged += period =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    _isAutomaticDayNightEnabled = _autoDayNightService.IsEnabled;
                    OnPropertyChanged(nameof(IsAutomaticDayNightEnabled));
                    OnPropertyChanged(nameof(AutoDayNightStatusText));
                    OnPropertyChanged(nameof(ScheduleToggleText));
                    UpdateDayNightActiveStates();
                });
            };

            _autoDayNightService.EnabledChanged += enabled =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    _isAutomaticDayNightEnabled = enabled;
                    OnPropertyChanged(nameof(IsAutomaticDayNightEnabled));
                    OnPropertyChanged(nameof(AutoDayNightStatusText));
                    OnPropertyChanged(nameof(ScheduleToggleText));
                    UpdateDayNightActiveStates();
                });
            };

            _settingsStore.SettingsChanged += (s, newSettings) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    DayStartTimeDisplay = string.IsNullOrWhiteSpace(newSettings.DayStartTime) ? "06:00" : newSettings.DayStartTime;
                    NightStartTimeDisplay = string.IsNullOrWhiteSpace(newSettings.NightStartTime) ? "21:00" : newSettings.NightStartTime;

                    var dayP = _profileManager.GetProfile(newSettings.DayProfileId);
                    if (dayP != null) DayModeDetails = $"{dayP.Kelvin}K · {dayP.Brightness}%";

                    var nightP = _profileManager.GetProfile(newSettings.NightProfileId);
                    if (nightP != null) NightModeDetails = $"{nightP.Kelvin}K · {nightP.Brightness}%";

                    _isAutomaticDayNightEnabled = newSettings.AutomaticDayNightEnabled;
                    OnPropertyChanged(nameof(IsAutomaticDayNightEnabled));
                    OnPropertyChanged(nameof(AutoDayNightStatusText));
                    OnPropertyChanged(nameof(ScheduleToggleText));
                    UpdateDayNightActiveStates();
                });
            };

            RefreshProfilesList();
            LoadState();

            try
            {
                _timelineClockTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _timelineClockTimer.Tick += (s, e) => UpdateLiveTimeline();
                _timelineClockTimer.Start();
            }
            catch
            {
                // Fallback for headless test runners
            }
            UpdateLiveTimeline();
        }

        public void LoadState()
        {
            var settings = _settingsStore.Load();

            DayStartTimeDisplay = string.IsNullOrWhiteSpace(settings.DayStartTime) ? "06:00" : settings.DayStartTime;
            NightStartTimeDisplay = string.IsNullOrWhiteSpace(settings.NightStartTime) ? "21:00" : settings.NightStartTime;

            var dayP = _profileManager.GetProfile(settings.DayProfileId);
            if (dayP != null)
            {
                DayModeDetails = $"{dayP.Kelvin}K · {dayP.Brightness}%";
            }

            var nightP = _profileManager.GetProfile(settings.NightProfileId);
            if (nightP != null)
            {
                NightModeDetails = $"{nightP.Kelvin}K · {nightP.Brightness}%";
            }

            _isAutomaticDayNightEnabled = _autoDayNightService.IsEnabled;
            OnPropertyChanged(nameof(IsAutomaticDayNightEnabled));
            OnPropertyChanged(nameof(AutoDayNightStatusText));
            OnPropertyChanged(nameof(ScheduleToggleText));

            var active = _profileManager.GetActiveProfile();
            SyncFromProfile(active);
            UpdateDayNightActiveStates();
        }

        private void UpdateDayNightActiveStates()
        {
            var settings = _settingsStore.Load();
            var activeId = ActiveProfile?.Id ?? _profileManager.GetActiveProfile().Id;

            IsDayModeActive = (activeId == settings.DayProfileId);
            IsNightModeActive = (activeId == settings.NightProfileId);
        }

        private void RefreshProfilesList()
        {
            var activeId = ActiveProfile?.Id ?? _profileManager.GetActiveProfile().Id;
            ProfileCards.Clear();
            foreach (var p in _profileManager.GetAllProfiles().Where(p => p.Id != ProfileDefaults.IdNight))
            {
                ProfileCards.Add(new ProfileCardItemViewModel(p, p.Id == activeId));
            }
        }

        public void SyncFromProfile(DisplayProfile profile)
        {
            _isUpdatingFromInternal = true;
            try
            {
                ActiveProfile = profile;
                Kelvin = profile.Kelvin;
                Brightness = profile.Brightness;
                SoftwareDim = profile.SoftwareDim;

                foreach (var card in ProfileCards)
                {
                    card.IsActive = (card.Id == profile.Id);
                }

                UpdateStatus();
                UpdateDayNightActiveStates();
            }
            finally
            {
                _isUpdatingFromInternal = false;
            }
        }

        private void OnTogglePause()
        {
            FlushPendingProfileSave();
            IsPaused = !IsPaused;
            if (IsPaused)
            {
                _displayEngine.ResetDisplay();
                SnackbarService.Instance.Show("EyeFocus paused (Display effects temporarily bypassed)");
            }
            else
            {
                var active = ActiveProfile ?? _profileManager.GetActiveProfile();
                _displayEngine.ApplyProfile(active);
                SnackbarService.Instance.Show($"EyeFocus resumed ({active.Name} profile restored)");
            }
        }

        private void OnSelectProfile(string? profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return;
            FlushPendingProfileSave();
            if (IsPaused) IsPaused = false;

            if (IsAutomaticDayNightEnabled)
            {
                IsAutomaticDayNightEnabled = false;
            }

            _profileManager.SetActiveProfile(profileId);
            var profile = _profileManager.GetProfile(profileId);
            if (profile != null)
            {
                _displayEngine.ApplyProfile(profile);
                SnackbarService.Instance.Show($"{profile.Name} profile applied");
            }
        }

        private void OnSelectDayMode()
        {
            FlushPendingProfileSave();
            if (IsAutomaticDayNightEnabled)
            {
                IsAutomaticDayNightEnabled = false;
            }

            if (IsPaused) IsPaused = false;
            var settings = _settingsStore.Load();
            _profileManager.SetActiveProfile(settings.DayProfileId);
            var p = _profileManager.GetProfile(settings.DayProfileId);
            if (p != null)
            {
                _displayEngine.ApplyProfile(p);
                SnackbarService.Instance.Show($"Day Mode ({p.Kelvin}K · {p.Brightness}%) applied");
            }
        }

        private void OnSelectNightMode()
        {
            FlushPendingProfileSave();
            if (IsAutomaticDayNightEnabled)
            {
                IsAutomaticDayNightEnabled = false;
            }

            if (IsPaused) IsPaused = false;
            var settings = _settingsStore.Load();
            _profileManager.SetActiveProfile(settings.NightProfileId);
            var p = _profileManager.GetProfile(settings.NightProfileId);
            if (p != null)
            {
                _displayEngine.ApplyProfile(p);
                SnackbarService.Instance.Show($"Night Mode ({p.Kelvin}K · {p.Brightness}%) applied");
            }
        }

        private void OnRestoreDisplay()
        {
            FlushPendingProfileSave();
            IsPaused = false;
            _displayEngine.ResetDisplay();
            var comfort = _profileManager.GetProfile(ProfileDefaults.IdComfort) ?? ProfileDefaults.GetDefaultProfiles().First();
            _profileManager.SetActiveProfile(comfort.Id);
            _displayEngine.ApplyProfile(comfort);
            SnackbarService.Instance.Show("Display settings restored to comfort default");
        }

        private void OnResetToProfileDefault()
        {
            if (ActiveProfile != null)
            {
                FlushPendingProfileSave();
                IsPaused = false;
                var reset = _profileManager.ResetProfileToDefault(ActiveProfile.Id);
                SyncFromProfile(reset);
                _displayEngine.ApplyProfile(reset);
                SnackbarService.Instance.Show($"Reset {ActiveProfile.Name} to default parameters");
            }
        }

        private void ScheduleDebouncedProfileSave()
        {
            _profileSaveDebounceTimer.Stop();
            _profileSaveDebounceTimer.Start();
        }

        public void FlushPendingProfileSave()
        {
            if (_profileSaveDebounceTimer.IsEnabled)
            {
                _profileSaveDebounceTimer.Stop();
                if (ActiveProfile != null)
                {
                    _profileManager.SaveProfile(ActiveProfile);
                }
            }
        }

        private void OnResetKelvin()
        {
            var activeId = ActiveProfile?.Id ?? _profileManager.GetActiveProfile().Id;
            var defaultProfile = ProfileDefaults.GetDefaultProfiles()
                .FirstOrDefault(p => string.Equals(p.Id, activeId, StringComparison.OrdinalIgnoreCase));

            int targetKelvin = defaultProfile != null ? defaultProfile.Kelvin : 4200;
            Kelvin = targetKelvin;
            if (ActiveProfile != null)
            {
                ActiveProfile.Kelvin = targetKelvin;
                _profileManager.SaveProfile(ActiveProfile);
            }
            FlushPendingProfileSave();
            _displayEngine.SetColorTemperature(targetKelvin);
            SnackbarService.Instance.Show($"Reset color temperature to default ({targetKelvin}K)");
        }

        private void OnResetBrightness()
        {
            var activeId = ActiveProfile?.Id ?? _profileManager.GetActiveProfile().Id;
            var defaultProfile = ProfileDefaults.GetDefaultProfiles()
                .FirstOrDefault(p => string.Equals(p.Id, activeId, StringComparison.OrdinalIgnoreCase));

            int targetBrightness = defaultProfile != null ? defaultProfile.Brightness : 40;
            Brightness = targetBrightness;
            if (ActiveProfile != null)
            {
                ActiveProfile.Brightness = targetBrightness;
                _profileManager.SaveProfile(ActiveProfile);
            }
            FlushPendingProfileSave();
            _displayEngine.SetBrightness(targetBrightness);
            SnackbarService.Instance.Show($"Reset brightness to default ({targetBrightness}%)");
        }

        private void OnEditProfile(string? profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return;
            FlushPendingProfileSave();
            var profile = _profileManager.GetProfile(profileId);
            if (profile != null)
            {
                RequestEditProfile?.Invoke(profile);
            }
        }

        private void OnDuplicateProfile(string? profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return;
            FlushPendingProfileSave();
            var duplicated = _profileManager.DuplicateProfile(profileId);
            if (duplicated != null)
            {
                SnackbarService.Instance.Show($"Created copy of profile: {duplicated.Name}");
            }
        }

        public void UpdateStatus()
        {
            var primary = _displayEngine.MonitorManager.GetPrimaryMonitor();
            if (primary != null)
            {
                if (primary.SupportsDdcCi && primary.SupportsHardwareBrightness)
                {
                    HardwareControlBadgeText = "DDC/CI Hardware";
                    HardwareStatusText = "DDC/CI ✓ • Hardware Control";
                }
                else if (primary.SupportsWmiBrightness || primary.IsInternal)
                {
                    HardwareControlBadgeText = "WMI Laptop";
                    HardwareStatusText = "WMI ✓ • Laptop Internal";
                }
                else
                {
                    HardwareControlBadgeText = "Software Dimming";
                    HardwareStatusText = "Software Dimming ✓ • Gamma Ready";
                }

                SelectedMonitorTitle = primary.FriendlyName;
                SelectedMonitorDisplayDetail = primary.IsPrimary ? "Primary Display" : "Secondary Display";
            }
            else
            {
                HardwareControlBadgeText = "All Synced";
                HardwareStatusText = "DDC/CI ✓ • Synchronized Control";
                SelectedMonitorTitle = "All Displays";
                SelectedMonitorDisplayDetail = "Synchronized Multi-Monitor";
            }
        }

        private void UpdateLiveTimeline()
        {
            var now = DateTime.Now;
            CurrentTimeText = now.ToString("h:mm tt");

            double currentMins = now.TimeOfDay.TotalMinutes;
            double progress = Math.Clamp(currentMins / 1440.0, 0.005, 0.995);

            TimelineLeftWeight = new GridLength(progress * 1000.0, GridUnitType.Star);
            TimelineRightWeight = new GridLength((1.0 - progress) * 1000.0, GridUnitType.Star);

            bool isDay = IsDayTime(now.TimeOfDay);
            CurrentTimePinColor = isDay ? Color.FromRgb(245, 158, 11) : Color.FromRgb(56, 189, 248);
            CurrentTimePinBrush = new SolidColorBrush(CurrentTimePinColor);

            UpdateTimelineColumnWeights();
        }

        private void UpdateTimelineColumnWeights()
        {
            double dayStartMin = ParseTimeToMinutes(DayStartTimeDisplay, 420); // 7:00 AM default
            double nightStartMin = ParseTimeToMinutes(NightStartTimeDisplay, 1080); // 6:00 PM default

            if (nightStartMin <= dayStartMin)
            {
                nightStartMin = Math.Min(dayStartMin + 600, 1380);
            }

            double col0 = Math.Max(dayStartMin, 60);
            double col1 = Math.Max(nightStartMin - dayStartMin, 120);
            double col2 = Math.Max(1440 - nightStartMin, 60);

            TimelineCol0Weight = new GridLength(col0, GridUnitType.Star);
            TimelineCol1Weight = new GridLength(col1, GridUnitType.Star);
            TimelineCol2Weight = new GridLength(col2, GridUnitType.Star);

            OnPropertyChanged(nameof(DayTimeFormatted));
            OnPropertyChanged(nameof(NightTimeFormatted));
            OnPropertyChanged(nameof(DayScheduleRange));
            OnPropertyChanged(nameof(NightScheduleRange));
        }

        private static double ParseTimeToMinutes(string timeStr, double defaultMinutes)
        {
            if (TimeSpan.TryParse(timeStr, out var ts))
            {
                return ts.TotalMinutes;
            }
            if (DateTime.TryParse(timeStr, out var dt))
            {
                return dt.TimeOfDay.TotalMinutes;
            }
            return defaultMinutes;
        }

        private static string FormatTime12H(string timeStr)
        {
            if (DateTime.TryParseExact(timeStr, new[] { "HH:mm", "H:mm", "h:mm tt", "hh:mm tt" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt.ToString("h:mm tt");
            }
            if (DateTime.TryParse(timeStr, out var dt2))
            {
                return dt2.ToString("h:mm tt");
            }
            return string.IsNullOrWhiteSpace(timeStr) ? "7:00 AM" : timeStr;
        }

        private bool IsDayTime(TimeSpan currentTime)
        {
            double cur = currentTime.TotalMinutes;
            double dayStart = ParseTimeToMinutes(DayStartTimeDisplay, 420);
            double nightStart = ParseTimeToMinutes(NightStartTimeDisplay, 1080);
            if (nightStart > dayStart)
            {
                return cur >= dayStart && cur < nightStart;
            }
            return cur >= dayStart || cur < nightStart;
        }
    }

    public class ProfileCardItemViewModel : ViewModelBase
    {
        private bool _isActive;
        public DisplayProfile Profile { get; }

        public string Id => Profile.Id;
        public string Name => Profile.Name;
        public string IconKey => GetEffectiveIconKey(Profile);
        public int Kelvin => Profile.Kelvin;
        public int Brightness => Profile.Brightness;
        public string Description => Profile.Description;
        public string FormattedParams => $"{Profile.Kelvin}K · {Profile.Brightness}%";

        public string IconBgBrush
        {
            get
            {
                bool isDark = ThemeService.ActiveTheme == "Dark";
                if (isDark)
                {
                    return Profile.Id.ToLowerInvariant() switch
                    {
                        ProfileDefaults.IdComfort => "#0D332D",
                        ProfileDefaults.IdGame => "#2E1B4E",
                        ProfileDefaults.IdMovie => "#3A141E",
                        ProfileDefaults.IdOffice => "#0E2C4A",
                        ProfileDefaults.IdEditing => "#362106",
                        ProfileDefaults.IdReading => "#0F2E1E",
                        ProfileDefaults.IdCoding => "#1E1B4B",
                        ProfileDefaults.IdCustom => "#1E293B",
                        _ => "#0D332D"
                    };
                }
                return Profile.Id.ToLowerInvariant() switch
                {
                    ProfileDefaults.IdComfort => "#E6F7F2",
                    ProfileDefaults.IdGame => "#F3E8FF",
                    ProfileDefaults.IdMovie => "#FFE4E6",
                    ProfileDefaults.IdOffice => "#E0F2FE",
                    ProfileDefaults.IdEditing => "#FEF3C7",
                    ProfileDefaults.IdReading => "#DCFCE7",
                    ProfileDefaults.IdCoding => "#EDE9FE",
                    ProfileDefaults.IdCustom => "#F1F5F9",
                    _ => "#E6F7F2"
                };
            }
        }

        public string IconFgBrush
        {
            get
            {
                bool isDark = ThemeService.ActiveTheme == "Dark";
                if (isDark)
                {
                    return Profile.Id.ToLowerInvariant() switch
                    {
                        ProfileDefaults.IdComfort => "#2DD4BF",
                        ProfileDefaults.IdGame => "#C084FC",
                        ProfileDefaults.IdMovie => "#FB7185",
                        ProfileDefaults.IdOffice => "#38BDF8",
                        ProfileDefaults.IdEditing => "#FBBF24",
                        ProfileDefaults.IdReading => "#4ADE80",
                        ProfileDefaults.IdCoding => "#818CF8",
                        ProfileDefaults.IdCustom => "#94A3B8",
                        _ => "#2DD4BF"
                    };
                }
                return Profile.Id.ToLowerInvariant() switch
                {
                    ProfileDefaults.IdComfort => "#00A88F",
                    ProfileDefaults.IdGame => "#8B5CF6",
                    ProfileDefaults.IdMovie => "#F43F5E",
                    ProfileDefaults.IdOffice => "#0284C7",
                    ProfileDefaults.IdEditing => "#D97706",
                    ProfileDefaults.IdReading => "#16A34A",
                    ProfileDefaults.IdCoding => "#6366F1",
                    ProfileDefaults.IdCustom => "#64748B",
                    _ => "#00A88F"
                };
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public ProfileCardItemViewModel(DisplayProfile profile, bool isActive)
        {
            Profile = profile;
            _isActive = isActive;
            ThemeService.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(string theme)
        {
            OnPropertyChanged(nameof(IconBgBrush));
            OnPropertyChanged(nameof(IconFgBrush));
        }

        public static string GetEffectiveIconKey(DisplayProfile profile)
        {
            return profile.Id.ToLowerInvariant() switch
            {
                ProfileDefaults.IdComfort => "IconLeaf",
                ProfileDefaults.IdGame => "IconGame",
                ProfileDefaults.IdMovie => "IconMovie",
                ProfileDefaults.IdOffice => "IconBriefcase",
                ProfileDefaults.IdEditing => "IconPalette",
                ProfileDefaults.IdReading => "IconBook",
                ProfileDefaults.IdCoding => "IconCode",
                ProfileDefaults.IdNight => "IconNight",
                ProfileDefaults.IdCustom => "IconTune",
                _ => !string.IsNullOrWhiteSpace(profile.IconKey) ? profile.IconKey : "IconLeaf"
            };
        }
    }
}
