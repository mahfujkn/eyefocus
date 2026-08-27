using System;
using System.Windows.Input;
using EyeFocus.Display;
using EyeFocus.Models;
using EyeFocus.Profiles;

namespace EyeFocus.ViewModels
{
    public class ProfileEditorViewModel : ViewModelBase
    {
        private readonly IProfileManager _profileManager;
        private readonly IDisplayEngine _displayEngine;

        private DisplayProfile? _originalProfile;
        private string _profileId = string.Empty;
        private string _profileName = string.Empty;
        private int _kelvin = 5500;
        private int _brightness = 75;
        private int _red = 100;
        private int _green = 100;
        private int _blue = 100;
        private int _softwareDim = 0;
        private int _contrast = 50;
        private int _transitionDuration = 5;
        private string _description = string.Empty;
        private bool _isBuiltIn = false;
        private bool _isLivePreviewEnabled = true;
        private bool _isOpen = false;

        public bool IsOpen
        {
            get => _isOpen;
            set => SetProperty(ref _isOpen, value);
        }

        public string ProfileName
        {
            get => _profileName;
            set => SetProperty(ref _profileName, value);
        }

        public int Kelvin
        {
            get => _kelvin;
            set
            {
                if (SetProperty(ref _kelvin, value))
                {
                    OnParametersChanged();
                }
            }
        }

        public int Brightness
        {
            get => _brightness;
            set
            {
                if (SetProperty(ref _brightness, value))
                {
                    OnParametersChanged();
                }
            }
        }

        public int Red
        {
            get => _red;
            set
            {
                if (SetProperty(ref _red, value))
                {
                    OnParametersChanged();
                }
            }
        }

        public int Green
        {
            get => _green;
            set
            {
                if (SetProperty(ref _green, value))
                {
                    OnParametersChanged();
                }
            }
        }

        public int Blue
        {
            get => _blue;
            set
            {
                if (SetProperty(ref _blue, value))
                {
                    OnParametersChanged();
                }
            }
        }

        public int SoftwareDim
        {
            get => _softwareDim;
            set
            {
                if (SetProperty(ref _softwareDim, value))
                {
                    OnParametersChanged();
                }
            }
        }

        public int Contrast
        {
            get => _contrast;
            set
            {
                if (SetProperty(ref _contrast, value))
                {
                    OnParametersChanged();
                }
            }
        }

        public int TransitionDuration
        {
            get => _transitionDuration;
            set => SetProperty(ref _transitionDuration, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool IsBuiltIn
        {
            get => _isBuiltIn;
            set => SetProperty(ref _isBuiltIn, value);
        }

        public bool IsLivePreviewEnabled
        {
            get => _isLivePreviewEnabled;
            set
            {
                if (SetProperty(ref _isLivePreviewEnabled, value))
                {
                    if (value)
                    {
                        OnParametersChanged();
                    }
                    else if (_originalProfile != null)
                    {
                        _displayEngine.ApplyProfile(_originalProfile);
                    }
                }
            }
        }

        public ICommand ApplyCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsNewCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action? CloseRequested;

        public ProfileEditorViewModel(IProfileManager profileManager, IDisplayEngine displayEngine)
        {
            _profileManager = profileManager;
            _displayEngine = displayEngine;

            ApplyCommand = new RelayCommand(OnApply);
            SaveCommand = new RelayCommand(OnSave);
            SaveAsNewCommand = new RelayCommand(OnSaveAsNew);
            ResetCommand = new RelayCommand(OnReset);
            CancelCommand = new RelayCommand(OnCancel);
        }

        public void OpenForProfile(DisplayProfile profile)
        {
            _originalProfile = profile.Clone();
            _profileId = profile.Id;
            ProfileName = profile.Name;
            Kelvin = profile.Kelvin;
            Brightness = profile.Brightness;
            Red = profile.Red;
            Green = profile.Green;
            Blue = profile.Blue;
            SoftwareDim = profile.SoftwareDim;
            Contrast = profile.Contrast ?? 50;
            TransitionDuration = profile.TransitionDurationSeconds;
            Description = profile.Description;
            IsBuiltIn = profile.IsBuiltIn;

            IsOpen = true;

            if (IsLivePreviewEnabled)
            {
                OnParametersChanged();
            }
        }

        private void OnParametersChanged()
        {
            if (IsOpen && IsLivePreviewEnabled)
            {
                var previewProfile = BuildCurrentProfile();
                _displayEngine.ApplyProfile(previewProfile);
            }
        }

        private DisplayProfile BuildCurrentProfile()
        {
            return new DisplayProfile
            {
                Id = _profileId,
                Name = ProfileName,
                Kelvin = Kelvin,
                Brightness = Brightness,
                Red = Red,
                Green = Green,
                Blue = Blue,
                SoftwareDim = SoftwareDim,
                Contrast = Contrast,
                TransitionDurationSeconds = TransitionDuration,
                Description = Description,
                IsBuiltIn = IsBuiltIn
            };
        }

        private void OnApply()
        {
            var profile = BuildCurrentProfile();
            _displayEngine.ApplyProfile(profile);
        }

        private void OnSave()
        {
            var profile = BuildCurrentProfile();
            _profileManager.SaveProfile(profile);
            _displayEngine.ApplyProfile(profile);
            Close();
        }

        private void OnSaveAsNew()
        {
            var profile = BuildCurrentProfile();
            var newName = $"{ProfileName} (Custom)";
            var saved = _profileManager.SaveAsNew(profile, newName);
            _profileManager.SetActiveProfile(saved.Id);
            _displayEngine.ApplyProfile(saved);
            Close();
        }

        private void OnReset()
        {
            if (IsBuiltIn)
            {
                var reset = _profileManager.ResetProfileToDefault(_profileId);
                OpenForProfile(reset);
            }
        }

        private void OnCancel()
        {
            if (_originalProfile != null)
            {
                _displayEngine.ApplyProfile(_originalProfile);
            }
            Close();
        }

        public void Close()
        {
            IsOpen = false;
            CloseRequested?.Invoke();
        }
    }
}
