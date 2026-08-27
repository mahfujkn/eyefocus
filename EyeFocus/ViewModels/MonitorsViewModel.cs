using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EyeFocus.Display;
using EyeFocus.Models;
using EyeFocus.Profiles;

namespace EyeFocus.ViewModels
{
    public class MonitorsViewModel : ViewModelBase
    {
        private readonly IDisplayEngine _displayEngine;
        private readonly IProfileManager _profileManager;
        private MonitorInfo? _selectedMonitor;

        public ObservableCollection<MonitorInfo> Monitors { get; } = new();
        public ObservableCollection<DisplayProfile> Profiles { get; } = new();

        public MonitorInfo? SelectedMonitor
        {
            get => _selectedMonitor;
            set => SetProperty(ref _selectedMonitor, value);
        }

        public ICommand RefreshMonitorsCommand { get; }

        public MonitorsViewModel(IDisplayEngine displayEngine, IProfileManager profileManager)
        {
            _displayEngine = displayEngine;
            _profileManager = profileManager;

            RefreshMonitorsCommand = new RelayCommand(OnRefreshMonitors);
            _displayEngine.MonitorManager.MonitorsChanged += (s, e) => LoadMonitors();
            _profileManager.ProfilesListChanged += (s, e) => LoadProfiles();

            LoadMonitors();
            LoadProfiles();
        }

        private void LoadMonitors()
        {
            Monitors.Clear();
            var list = _displayEngine.MonitorManager.GetMonitors();
            foreach (var m in list)
            {
                Monitors.Add(m);
            }
            SelectedMonitor = Monitors.FirstOrDefault(m => m.IsPrimary) ?? Monitors.FirstOrDefault();
        }

        private void LoadProfiles()
        {
            Profiles.Clear();
            foreach (var p in _profileManager.GetAllProfiles())
            {
                Profiles.Add(p);
            }
        }

        private void OnRefreshMonitors()
        {
            _displayEngine.MonitorManager.RefreshMonitors();
        }
    }
}
