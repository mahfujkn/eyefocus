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
                }
            }
        }

        public string PauseButtonText => IsPaused ? "Resume" : "Pause";
        public string PauseButtonToolTip => IsPaused ? "Resume EyeFocus display effects" : "Temporarily pause EyeFocus display effects";

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
                    if (ActiveProfile != null) ActiveProfile.Kelvin = value;
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
                    if (ActiveProfile != null) ActiveProfile.Brightness = value;
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
                    if (ActiveProfile != null) ActiveProfile.SoftwareDim = value;
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
        public ICommand RestoreDisplayCommand { get; }
        public ICommand ResetToProfileDefaultCommand { get; }
        public ICommand ResetKelvinCommand { get; }
        public ICommand ResetBrightnessCommand { get; }
        public ICommand EditProfileCommand { get; }
        public ICommand DuplicateProfileCommand { get; }

        public event Action? RequestOpenProfileEditor;
        public event Action? RequestOpenAutoDayNightConfig;
        public event Action<DisplayProfile>? RequestEditProfile;

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

            _activeProfile = _profileManager.GetActiveProfile();

            SelectProfileCommand = new RelayCommand<string>(OnSelectProfile);
            SelectDayModeCommand = new RelayCommand(OnSelectDayMode);
            SelectNightModeCommand = new RelayCommand(OnSelectNightMode);
            ConfigureAutoDayNightCommand = new RelayCommand(() => RequestOpenAutoDayNightConfig?.Invoke());
            TogglePauseCommand = new RelayCommand(OnTogglePause);
            RestoreDisplayCommand = new RelayCommand(OnRestoreDisplay);
            ResetToProfileDefaultCommand = new RelayCommand(OnResetToProfileDefault);
            ResetKelvinCommand = new RelayCommand(() => Kelvin = ActiveProfile.Kelvin);
            ResetBrightnessCommand = new RelayCommand(() => Brightness = ActiveProfile.Brightness);
            EditProfileCommand = new RelayCommand<string>(OnEditProfile);
            DuplicateProfileCommand = new RelayCommand<string>(OnDuplicateProfile);

            _profileManager.ActiveProfileChanged += (s, p) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    SyncFromProfile(p);
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
            foreach (var p in _profileManager.GetAllProfiles())
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
            if (IsPaused) IsPaused = false;

            if (IsAutomaticDayNightEnabled)
            {
                _autoDayNightService.SetEnabled(false);
                OnPropertyChanged(nameof(IsAutomaticDayNightEnabled));
                OnPropertyChanged(nameof(AutoDayNightStatusText));
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
            if (IsAutomaticDayNightEnabled)
            {
                SnackbarService.Instance.Show("Automatic Day/Night is active. Toggle off to select manually.");
                return;
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
            if (IsAutomaticDayNightEnabled)
            {
                SnackbarService.Instance.Show("Automatic Day/Night is active. Toggle off to select manually.");
                return;
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
                IsPaused = false;
                var reset = _profileManager.ResetProfileToDefault(ActiveProfile.Id);
                SyncFromProfile(reset);
                _displayEngine.ApplyProfile(reset);
                SnackbarService.Instance.Show($"Reset {ActiveProfile.Name} to default parameters");
            }
        }

        private void OnEditProfile(string? profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return;
            var profile = _profileManager.GetProfile(profileId);
            if (profile != null)
            {
                RequestEditProfile?.Invoke(profile);
            }
        }

        private void OnDuplicateProfile(string? profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return;
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
                ProfileDefaults.IdComfort => "IconComfort",
                ProfileDefaults.IdGame => "IconGame",
                ProfileDefaults.IdMovie => "IconMovie",
                ProfileDefaults.IdOffice => "IconOffice",
                ProfileDefaults.IdEditing => "IconPalette",
                ProfileDefaults.IdReading => "IconBook",
                ProfileDefaults.IdCoding => "IconCode",
                ProfileDefaults.IdNight => "IconNight",
                ProfileDefaults.IdCustom => "IconTune",
                _ => !string.IsNullOrWhiteSpace(profile.IconKey) ? profile.IconKey : "IconComfort"
            };
        }
    }
}
