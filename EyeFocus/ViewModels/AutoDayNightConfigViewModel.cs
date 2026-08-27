using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EyeFocus.Automation;
using EyeFocus.Models;
using EyeFocus.Services;
using EyeFocus.Storage;

namespace EyeFocus.ViewModels
{
    public class AutoDayNightConfigViewModel : ViewModelBase
    {
        private readonly ISettingsStore _settingsStore;
        private readonly IAutoDayNightService _autoDayNightService;

        private bool _isOpen;
        private bool _isSystemAutomatic = true;
        private string _selectedTimeZoneId = string.Empty;
        private string _dayStartTime = "06:00";
        private string _nightStartTime = "21:00";
        private string _dayHour = "06";
        private string _dayMinute = "00";
        private string _nightHour = "21";
        private string _nightMinute = "00";
        private string _validationMessage = string.Empty;

        public ObservableCollection<TimeZoneItem> TimeZones { get; } = new();
        public ObservableCollection<string> Hours { get; } = new();
        public ObservableCollection<string> Minutes { get; } = new();

        public bool IsOpen
        {
            get => _isOpen;
            set => SetProperty(ref _isOpen, value);
        }

        public bool IsSystemAutomatic
        {
            get => _isSystemAutomatic;
            set
            {
                if (SetProperty(ref _isSystemAutomatic, value))
                {
                    OnPropertyChanged(nameof(IsManual));
                    Validate();
                }
            }
        }

        public bool IsManual
        {
            get => !_isSystemAutomatic;
            set
            {
                IsSystemAutomatic = !value;
                OnPropertyChanged(nameof(IsManual));
            }
        }

        public string SelectedTimeZoneId
        {
            get => _selectedTimeZoneId;
            set
            {
                if (SetProperty(ref _selectedTimeZoneId, value))
                {
                    Validate();
                }
            }
        }

        public string DayHour
        {
            get => _dayHour;
            set
            {
                if (SetProperty(ref _dayHour, value))
                {
                    _dayStartTime = $"{_dayHour}:{_dayMinute}";
                    OnPropertyChanged(nameof(DayStartTime));
                    Validate();
                }
            }
        }

        public string DayMinute
        {
            get => _dayMinute;
            set
            {
                if (SetProperty(ref _dayMinute, value))
                {
                    _dayStartTime = $"{_dayHour}:{_dayMinute}";
                    OnPropertyChanged(nameof(DayStartTime));
                    Validate();
                }
            }
        }

        public string NightHour
        {
            get => _nightHour;
            set
            {
                if (SetProperty(ref _nightHour, value))
                {
                    _nightStartTime = $"{_nightHour}:{_nightMinute}";
                    OnPropertyChanged(nameof(NightStartTime));
                    Validate();
                }
            }
        }

        public string NightMinute
        {
            get => _nightMinute;
            set
            {
                if (SetProperty(ref _nightMinute, value))
                {
                    _nightStartTime = $"{_nightHour}:{_nightMinute}";
                    OnPropertyChanged(nameof(NightStartTime));
                    Validate();
                }
            }
        }

        public string DayStartTime
        {
            get => _dayStartTime;
            set
            {
                if (SetProperty(ref _dayStartTime, value))
                {
                    SyncHourMinuteFromDayString(value);
                    Validate();
                }
            }
        }

        public string NightStartTime
        {
            get => _nightStartTime;
            set
            {
                if (SetProperty(ref _nightStartTime, value))
                {
                    SyncHourMinuteFromNightString(value);
                    Validate();
                }
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                if (SetProperty(ref _validationMessage, value))
                {
                    OnPropertyChanged(nameof(HasValidationError));
                }
            }
        }

        public bool HasValidationError => !string.IsNullOrEmpty(ValidationMessage);

        public string SystemInfoText
        {
            get
            {
                var local = TimeZoneInfo.Local;
                var offset = local.GetUtcOffset(DateTime.UtcNow);
                var offsetStr = (offset >= TimeSpan.Zero ? "+" : "-") + offset.ToString(@"hh\:mm");
                return $"Using Windows system time\nTime zone: {local.DisplayName} (UTC{offsetStr})";
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action? ConfigurationSaved;

        public AutoDayNightConfigViewModel(
            ISettingsStore settingsStore,
            IAutoDayNightService autoDayNightService)
        {
            _settingsStore = settingsStore;
            _autoDayNightService = autoDayNightService;

            SaveCommand = new RelayCommand(OnSave);
            CancelCommand = new RelayCommand(OnCancel);

            for (int h = 0; h < 24; h++) Hours.Add(h.ToString("00"));
            for (int m = 0; m < 60; m += 5) Minutes.Add(m.ToString("00"));

            PopulateTimeZones();
        }

        public void Open()
        {
            var settings = _settingsStore.Load();
            _isSystemAutomatic = !string.Equals(settings.TimeDetectionMode, "Manual", StringComparison.OrdinalIgnoreCase);
            _selectedTimeZoneId = string.IsNullOrWhiteSpace(settings.ManualTimeZoneId)
                ? TimeZoneInfo.Local.Id
                : settings.ManualTimeZoneId;

            var dTime = string.IsNullOrWhiteSpace(settings.DayStartTime) ? "06:00" : settings.DayStartTime;
            var nTime = string.IsNullOrWhiteSpace(settings.NightStartTime) ? "21:00" : settings.NightStartTime;

            _dayStartTime = dTime;
            _nightStartTime = nTime;

            SyncHourMinuteFromDayString(dTime);
            SyncHourMinuteFromNightString(nTime);

            OnPropertyChanged(nameof(IsSystemAutomatic));
            OnPropertyChanged(nameof(IsManual));
            OnPropertyChanged(nameof(SelectedTimeZoneId));
            OnPropertyChanged(nameof(DayStartTime));
            OnPropertyChanged(nameof(NightStartTime));
            OnPropertyChanged(nameof(DayHour));
            OnPropertyChanged(nameof(DayMinute));
            OnPropertyChanged(nameof(NightHour));
            OnPropertyChanged(nameof(NightMinute));
            OnPropertyChanged(nameof(SystemInfoText));

            Validate();
            IsOpen = true;
        }

        private void SyncHourMinuteFromDayString(string val)
        {
            if (DayNightScheduleCalculator.TryParseTime(val, out var ts))
            {
                _dayHour = ts.Hours.ToString("00");
                _dayMinute = ts.Minutes.ToString("00");
                if (!Minutes.Contains(_dayMinute)) Minutes.Add(_dayMinute);
                OnPropertyChanged(nameof(DayHour));
                OnPropertyChanged(nameof(DayMinute));
            }
        }

        private void SyncHourMinuteFromNightString(string val)
        {
            if (DayNightScheduleCalculator.TryParseTime(val, out var ts))
            {
                _nightHour = ts.Hours.ToString("00");
                _nightMinute = ts.Minutes.ToString("00");
                if (!Minutes.Contains(_nightMinute)) Minutes.Add(_nightMinute);
                OnPropertyChanged(nameof(NightHour));
                OnPropertyChanged(nameof(NightMinute));
            }
        }

        private void PopulateTimeZones()
        {
            TimeZones.Clear();
            try
            {
                var systemTzs = TimeZoneInfo.GetSystemTimeZones();
                foreach (var tz in systemTzs.OrderBy(t => t.BaseUtcOffset).ThenBy(t => t.DisplayName))
                {
                    var offset = tz.BaseUtcOffset;
                    var offsetStr = (offset >= TimeSpan.Zero ? "+" : "-") + offset.ToString(@"hh\:mm");
                    TimeZones.Add(new TimeZoneItem
                    {
                        Id = tz.Id,
                        DisplayName = $"(UTC{offsetStr}) {tz.DisplayName}",
                        StandardName = tz.StandardName,
                        BaseUtcOffset = offset,
                        FormattedOffset = offsetStr
                    });
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to populate system timezones", ex);
            }
        }

        private bool Validate()
        {
            if (!DayNightScheduleCalculator.TryParseTime(DayStartTime, out var dayTime))
            {
                ValidationMessage = "Day start time must be in 24-hour format (e.g. 06:00).";
                return false;
            }

            if (!DayNightScheduleCalculator.TryParseTime(NightStartTime, out var nightTime))
            {
                ValidationMessage = "Night start time must be in 24-hour format (e.g. 21:00).";
                return false;
            }

            if (dayTime == nightTime)
            {
                ValidationMessage = "Day start time and Night start time cannot be identical.";
                return false;
            }

            if (IsManual && string.IsNullOrWhiteSpace(SelectedTimeZoneId))
            {
                ValidationMessage = "Please select a valid time zone for manual mode.";
                return false;
            }

            ValidationMessage = string.Empty;
            return true;
        }

        private void OnSave()
        {
            if (!Validate()) return;

            var settings = _settingsStore.Load();
            settings.TimeDetectionMode = IsSystemAutomatic ? "System" : "Manual";
            settings.ManualTimeZoneId = SelectedTimeZoneId;
            settings.DayStartTime = DayStartTime.Trim();
            settings.NightStartTime = NightStartTime.Trim();

            _settingsStore.Save(settings);

            _autoDayNightService.Evaluate(forceApply: settings.AutomaticDayNightEnabled);

            ConfigurationSaved?.Invoke();
            IsOpen = false;

            SnackbarService.Instance.Show("Automatic Day/Night settings saved");
        }

        private void OnCancel()
        {
            IsOpen = false;
        }
    }
}
