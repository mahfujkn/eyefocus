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

        private string _hardwareControlBadgeText = "Hardware · DDC/CI ✓";
        private string _hardwareStatusText = "DDC/CI ✓ • Gamma Fallback Ready";
        private string _selectedMonitorTitle = "All Displays";
        private string _selectedMonitorDisplayDetail = "Synchronized Control";

        private string _dayModeDetails = "5000K · 70%";
        private string _nightModeDetails = "3000K · 40%";
        private string _dayStartTimeDisplay = "06:00";
        private string _nightStartTimeDisplay = "21:00";

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
                }
            }
        }

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
            set => SetProperty(ref _dayStartTimeDisplay, value);
        }

        public string NightStartTimeDisplay
        {
            get => _nightStartTimeDisplay;
            set => SetProperty(ref _nightStartTimeDisplay, value);
        }

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
                    UpdateDayNightActiveStates();
                });
            };

            RefreshProfilesList();
            LoadState();
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
                    HardwareControlBadgeText = "Hardware · DDC/CI ✓";
                    HardwareStatusText = "DDC/CI ✓ • Hardware Control";
                }
                else if (primary.SupportsWmiBrightness || primary.IsInternal)
                {
                    HardwareControlBadgeText = "Hardware · WMI Laptop ✓";
                    HardwareStatusText = "WMI ✓ • Laptop Internal";
                }
                else
                {
                    HardwareControlBadgeText = "Software Dimming · Active";
                    HardwareStatusText = "Software Dimming ✓ • Gamma Ready";
                }

                SelectedMonitorTitle = primary.FriendlyName;
                SelectedMonitorDisplayDetail = primary.IsPrimary ? "Primary Display" : "Secondary Display";
            }
            else
            {
                HardwareControlBadgeText = "All Displays Synced";
                HardwareStatusText = "DDC/CI ✓ • Synchronized Control";
                SelectedMonitorTitle = "All Displays";
                SelectedMonitorDisplayDetail = "Synchronized Multi-Monitor";
            }
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

        public string IconBgBrush => Profile.Id.ToLowerInvariant() switch
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

        public string IconFgBrush => Profile.Id.ToLowerInvariant() switch
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

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public ProfileCardItemViewModel(DisplayProfile profile, bool isActive)
        {
            Profile = profile;
            _isActive = isActive;
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
