using System.Diagnostics;
using System.Windows.Input;

namespace EyeFocus.ViewModels
{
    public class AboutViewModel : ViewModelBase
    {
        public string AppName => "EyeFocus";
        public string Version => "v1.0.0 Stable";
        public string Tagline => "Your display. Your comfort. Your control.";
        public string Description => "EyeFocus is a privacy-first, open-source Windows display comfort utility for controlling brightness, color temperature, and display profiles locally with Material Design 3 Expressive UI.";

        public string RepositoryUrl => "https://github.com/mahfujkn/EyeFocus";
        public string ReleasesUrl => "https://github.com/mahfujkn/EyeFocus/releases";
        public string DeveloperName => "Mahfuj Khan Rafsan";
        public string DeveloperUrl => "https://github.com/mahfujkn";

        public ICommand OpenRepositoryCommand { get; }
        public ICommand OpenReleasesCommand { get; }
        public ICommand OpenDeveloperCommand { get; }

        public AboutViewModel()
        {
            OpenRepositoryCommand = new RelayCommand(() => OpenUrl(RepositoryUrl));
            OpenReleasesCommand = new RelayCommand(() => OpenUrl(ReleasesUrl));
            OpenDeveloperCommand = new RelayCommand(() => OpenUrl(DeveloperUrl));
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Fallback ignore if browser fails to launch
            }
        }
    }
}
