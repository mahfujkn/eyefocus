using System;
using System.Windows.Input;
using System.Windows.Threading;
using EyeFocus.Services;

namespace EyeFocus.ViewModels
{
    public class EyeExerciseViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _timer;
        private bool _isOpen;
        private bool _isActive;
        private int _secondsRemaining = 20;
        private string _instructionText = "Look away at an object at least 20 feet (6m) away.";
        private string _detailText = "Relax your focus, breathe deeply, and blink naturally to refresh your eyes.";
        private bool _isCompleted;

        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (SetProperty(ref _isOpen, value))
                {
                    if (value)
                    {
                        ResetExercise();
                    }
                    else
                    {
                        _timer.Stop();
                        IsActive = false;
                    }
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    OnPropertyChanged(nameof(StartButtonText));
                }
            }
        }

        public int SecondsRemaining
        {
            get => _secondsRemaining;
            set
            {
                if (SetProperty(ref _secondsRemaining, value))
                {
                    OnPropertyChanged(nameof(TimerDisplay));
                    OnPropertyChanged(nameof(ProgressRatio));
                }
            }
        }

        public string TimerDisplay => $"{SecondsRemaining}s";

        public double ProgressRatio => (20.0 - SecondsRemaining) / 20.0;

        public string InstructionText
        {
            get => _instructionText;
            set => SetProperty(ref _instructionText, value);
        }

        public string DetailText
        {
            get => _detailText;
            set => SetProperty(ref _detailText, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        public string StartButtonText
        {
            get
            {
                if (IsCompleted) return "Restart";
                return IsActive ? "Pause" : "Start Exercise";
            }
        }

        public ICommand StartCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CloseCommand { get; }

        public EyeExerciseViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;

            StartCommand = new RelayCommand(OnToggleStart);
            ResetCommand = new RelayCommand(ResetExercise);
            CloseCommand = new RelayCommand(() => IsOpen = false);
        }

        public void Open()
        {
            IsOpen = true;
            OnToggleStart(); // Auto start on opening for instant convenience
        }

        private void OnToggleStart()
        {
            if (IsCompleted)
            {
                ResetExercise();
            }

            if (IsActive)
            {
                _timer.Stop();
                IsActive = false;
            }
            else
            {
                _timer.Start();
                IsActive = true;
                InstructionText = "Breathe deeply and focus on the distant object.";
            }
        }

        private void ResetExercise()
        {
            _timer.Stop();
            IsActive = false;
            IsCompleted = false;
            SecondsRemaining = 20;
            InstructionText = "Look away at an object at least 20 feet (6m) away.";
            DetailText = "Relax your focus, breathe deeply, and blink naturally to refresh your eyes.";
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (SecondsRemaining > 1)
            {
                SecondsRemaining--;
            }
            else
            {
                SecondsRemaining = 0;
                _timer.Stop();
                IsActive = false;
                IsCompleted = true;
                InstructionText = "Great job! Your eyes are refreshed.";
                DetailText = "Regular 20-20-20 breaks help prevent digital eye strain and maintain long-term visual comfort.";
                SnackbarService.Instance.Show("Eye exercise completed! Eye strain reduced.");
            }
        }
    }
}
