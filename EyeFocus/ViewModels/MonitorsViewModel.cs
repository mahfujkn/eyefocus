using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EyeFocus.Display;
using EyeFocus.Models;
using EyeFocus.Profiles;
using EyeFocus.Storage;

namespace EyeFocus.ViewModels
{
    public class MonitorsViewModel : ViewModelBase
    {
        private readonly IDisplayEngine _displayEngine;
        private readonly IProfileManager _profileManager;
        private readonly SystemIntegration.IDisplayStateGuard? _displayStateGuard;
        private MonitorInfo? _selectedMonitor;

        public ObservableCollection<MonitorInfo> Monitors { get; } = new();
        public ObservableCollection<DisplayProfile> Profiles { get; } = new();

        public MonitorInfo? SelectedMonitor
        {
            get => _selectedMonitor;
            set => SetProperty(ref _selectedMonitor, value);
        }

        public ICommand RefreshMonitorsCommand { get; }
        public ICommand OpenDisplaySettingsCommand { get; }
        public ICommand IdentifyMonitorCommand { get; }
        public ICommand RedetectMonitorCommand { get; }
        public ICommand CopySpecsCommand { get; }

        public MonitorsViewModel(IDisplayEngine displayEngine, IProfileManager profileManager, SystemIntegration.IDisplayStateGuard? displayStateGuard = null)
        {
            _displayEngine = displayEngine;
            _profileManager = profileManager;
            _displayStateGuard = displayStateGuard;

            RefreshMonitorsCommand = new RelayCommand(OnRefreshMonitors);
            OpenDisplaySettingsCommand = new RelayCommand<MonitorInfo>(OnOpenDisplaySettings);
            IdentifyMonitorCommand = new RelayCommand<MonitorInfo>(OnIdentifyMonitor);
            RedetectMonitorCommand = new RelayCommand<MonitorInfo>(OnRedetectMonitor);
            CopySpecsCommand = new RelayCommand<MonitorInfo>(OnCopySpecs);

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

        private void OnOpenDisplaySettings(MonitorInfo? monitor)
        {
            try
            {
                Services.SnackbarService.Instance.Show("Opening Windows Display Settings...");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:display")
                {
                    UseShellExecute = true
                });

                // Engage active recovery guard to seamlessly auto-restore profile as Windows probes displays
                _displayStateGuard?.NotifySettingsOpened();
            }
            catch (System.Exception ex)
            {
                LogService.Debug($"Error opening display settings: {ex.Message}");
            }
        }

        private void OnIdentifyMonitor(MonitorInfo? monitor)
        {
            if (monitor == null) return;
            Services.SnackbarService.Instance.Show($"Identifying {monitor.FriendlyName} (Display {monitor.DisplayIndex})...");

            var app = System.Windows.Application.Current;
            if (app == null) return;

            app.Dispatcher.Invoke(() =>
            {
                try
                {
                    double scaleX = monitor.DpiScaleX > 0 ? monitor.DpiScaleX : 1.0;
                    double scaleY = monitor.DpiScaleY > 0 ? monitor.DpiScaleY : 1.0;

                    var win = new System.Windows.Window
                    {
                        WindowStyle = System.Windows.WindowStyle.None,
                        AllowsTransparency = true,
                        Background = System.Windows.Media.Brushes.Transparent,
                        Topmost = true,
                        ShowInTaskbar = false,
                        WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
                        Left = monitor.Left / scaleX,
                        Top = monitor.Top / scaleY,
                        Width = monitor.Width / scaleX,
                        Height = monitor.Height / scaleY
                    };

                    var border = new System.Windows.Controls.Border
                    {
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(240, 15, 23, 42)),
                        BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 212, 191)),
                        BorderThickness = new System.Windows.Thickness(2),
                        CornerRadius = new System.Windows.CornerRadius(18),
                        Padding = new System.Windows.Thickness(42, 28, 42, 28),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            BlurRadius = 36,
                            ShadowDepth = 6,
                            Color = System.Windows.Media.Colors.Black,
                            Opacity = 0.45
                        }
                    };

                    var stack = new System.Windows.Controls.StackPanel
                    {
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                    };

                    stack.Children.Add(new System.Windows.Controls.TextBlock
                    {
                        Text = monitor.DisplayIndex.ToString(),
                        FontSize = 76,
                        FontWeight = System.Windows.FontWeights.Bold,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 212, 191)),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                    });

                    stack.Children.Add(new System.Windows.Controls.TextBlock
                    {
                        Text = monitor.FriendlyName,
                        FontSize = 18,
                        FontWeight = System.Windows.FontWeights.SemiBold,
                        Foreground = System.Windows.Media.Brushes.White,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new System.Windows.Thickness(0, 6, 0, 0)
                    });

                    stack.Children.Add(new System.Windows.Controls.TextBlock
                    {
                        Text = monitor.CleanDeviceName,
                        FontSize = 13,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new System.Windows.Thickness(0, 4, 0, 0)
                    });

                    border.Child = stack;
                    win.Content = border;
                    win.Show();

                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = System.TimeSpan.FromSeconds(2.5)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        var anim = new System.Windows.Media.Animation.DoubleAnimation(0, System.TimeSpan.FromMilliseconds(350));
                        anim.Completed += (s2, e2) => win.Close();
                        win.BeginAnimation(System.Windows.UIElement.OpacityProperty, anim);
                    };
                    timer.Start();
                }
                catch (System.Exception ex)
                {
                    LogService.Debug($"Identify display error: {ex.Message}");
                }
            });
        }

        private void OnRedetectMonitor(MonitorInfo? monitor)
        {
            try
            {
                _displayEngine.MonitorManager.RefreshMonitors();
                LoadMonitors();
                if (monitor != null)
                {
                    Services.SnackbarService.Instance.Show($"Re-detected capabilities for {monitor.FriendlyName}.");
                }
                else
                {
                    Services.SnackbarService.Instance.Show("Re-detected display capabilities.");
                }
            }
            catch (System.Exception ex)
            {
                LogService.Debug($"Error redetecting monitor: {ex.Message}");
            }
        }

        private void OnCopySpecs(MonitorInfo? monitor)
        {
            if (monitor == null) return;
            try
            {
                var text = $"Monitor: {monitor.FriendlyName} (Display {monitor.DisplayIndex})\n" +
                           $"Device: {monitor.CleanDeviceName}\n" +
                           $"Active Resolution: {monitor.ResolutionDisplay}\n" +
                           $"Refresh Rate: {monitor.RefreshRateDisplay}\n" +
                           $"Scaling (DPI): {monitor.ScalingPercentageDisplay}\n" +
                           $"Connection Type: {monitor.ConnectionType}\n" +
                           $"HDR Support: {monitor.HdrDisplay}\n" +
                           $"Color Depth: {monitor.ColorDepthDisplay}\n" +
                           $"Manufacturer: {monitor.ManufacturerDisplay}\n" +
                           $"Serial Number: {monitor.SerialDisplay}\n" +
                           $"DDC/CI Brightness: {monitor.DdcCiDisplay}\n" +
                           $"Hardware Color: {monitor.HardwareColorDisplay}\n" +
                           $"Windows Gamma: {monitor.WindowsGammaDisplay}\n" +
                           $"Software Dim: {monitor.SoftwareDimDisplay}";

                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        System.Windows.Clipboard.SetDataObject(text, true);
                        break;
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(50);
                    }
                }
                Services.SnackbarService.Instance.Show($"Specifications copied for {monitor.FriendlyName}.");
            }
            catch (System.Exception ex)
            {
                LogService.Debug($"Error copying specs: {ex.Message}");
            }
        }
    }
}
