using System;
using System.IO;
using System.Linq;
using EyeFocus.Models;
using EyeFocus.Profiles;
using EyeFocus.Storage;
using EyeFocus.Services;
using EyeFocus.SystemIntegration;
using Xunit;

namespace EyeFocus.Tests
{
    public class ThemeAndSettingsTests
    {
        [Fact]
        public void SettingsStore_SavesAndLoadsMaterial3Theme()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"EyeFocus_Test_{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var store = new SettingsStore(settingsPath);

            var settings = store.Load();
            Assert.Equal("System", settings.Theme);

            settings.Theme = "Dark";
            store.Save(settings);

            var reloaded = store.Load();
            Assert.Equal("Dark", reloaded.Theme);

            // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public void ProfileDefaults_ContainsAll9BuiltInProfiles_WithComfortAsDefault()
        {
            var defaults = ProfileDefaults.GetDefaultProfiles();
            Assert.Equal(9, defaults.Count);

            var comfort = defaults.FirstOrDefault(p => p.Id == ProfileDefaults.IdComfort);
            Assert.NotNull(comfort);
            Assert.Equal("Comfort", comfort.Name);
            Assert.Equal(4200, comfort.Kelvin);
            Assert.Equal(40, comfort.Brightness);
            Assert.Equal(5, comfort.SoftwareDim);

            // Check other profiles
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdGame && p.Kelvin == 6500 && p.Brightness == 100);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdMovie && p.Kelvin == 4500 && p.Brightness == 65);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdOffice && p.Kelvin == 5000 && p.Brightness == 70);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdEditing && p.Kelvin == 6500 && p.Brightness == 70);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdReading && p.Kelvin == 4000 && p.Brightness == 55);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdCoding && p.Kelvin == 4500 && p.Brightness == 60);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdNight && p.Kelvin == 3000 && p.Brightness == 40);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdCustom && p.Kelvin == 5000 && p.Brightness == 70);
        }

        [Fact]
        public void MainWindow_Xaml_CanBeInitialized()
        {
            Exception? threadEx = null;
            var thread = new System.Threading.Thread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    new System.Windows.Application();
                }
                
                // Merge resources as in App.xaml
                var app = System.Windows.Application.Current;
                app.Resources.MergedDictionaries.Clear();
                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Colors.xaml") });
                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Icons.xaml") });
                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Styles.xaml") });
                app.Resources["IconKeyToGeometryConverter"] = new EyeFocus.UI.Converters.IconKeyToGeometryConverter();
                app.Resources["TabToBooleanConverter"] = new EyeFocus.UI.Converters.TabToBooleanConverter();

                try
                {
                    var window = new EyeFocus.MainWindow(null!, null!, null!);
                    Assert.NotNull(window);
                    window.Measure(new System.Windows.Size(1000, 800));
                    window.Arrange(new System.Windows.Rect(0, 0, 1000, 800));
                    window.ApplyTemplate();

                    // Verify Light & Dark theme token switching
                    EyeFocus.UI.Themes.ThemeService.ApplyTheme("Light");
                    Assert.NotNull(app.Resources["DayModeCardBackgroundBrush"]);
                    Assert.NotNull(app.Resources["NightModeCardBackgroundBrush"]);
                    Assert.NotNull(app.Resources["BrightnessBadgeBackgroundBrush"]);
                    Assert.NotNull(app.Resources["KelvinBadgeBackgroundBrush"]);
                    Assert.NotNull(app.Resources["BrightnessHeaderCircleBgBrush"]);
                    Assert.NotNull(app.Resources["ThemePillTrackBackgroundBrush"]);
                    Assert.NotNull(app.Resources["ThemePillThumbBackgroundBrush"]);

                    EyeFocus.UI.Themes.ThemeService.ApplyTheme("Dark");
                    Assert.NotNull(app.Resources["DayModeCardBackgroundBrush"]);
                    Assert.NotNull(app.Resources["NightModeCardBackgroundBrush"]);
                    Assert.NotNull(app.Resources["BrightnessBadgeBackgroundBrush"]);
                    Assert.NotNull(app.Resources["KelvinBadgeBackgroundBrush"]);
                    Assert.NotNull(app.Resources["BrightnessHeaderCircleBgBrush"]);
                    Assert.NotNull(app.Resources["ThemePillTrackBackgroundBrush"]);
                    Assert.NotNull(app.Resources["ThemePillThumbBackgroundBrush"]);
                    window.Close();
                    app.Dispatcher.InvokeShutdown();
                }
                catch (Exception ex)
                {
                    var inner = ex;
                    while (inner.InnerException != null) inner = inner.InnerException;
                    threadEx = new Exception($"XAML Error: {inner.Message} | Stack: {inner.StackTrace}", ex);
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            bool finished = thread.Join(8000);
            Assert.True(finished, "STA thread timed out");
            if (threadEx != null) throw threadEx;
        }

        [Fact]
        public void ThemeService_AppliesAndSwitchesThemePillTokens()
        {
            EyeFocus.UI.Themes.ThemeService.ApplyTheme("Light");
            Assert.Equal("Light", EyeFocus.UI.Themes.ThemeService.ActiveTheme);

            EyeFocus.UI.Themes.ThemeService.ApplyTheme("Dark");
            Assert.Equal("Dark", EyeFocus.UI.Themes.ThemeService.ActiveTheme);
        }

        [Fact]
        public void MonitorsView_And_AboutView_CanBeInitializedAndRendered()
        {
            var thread = new System.Threading.Thread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    new System.Windows.Application();
                }

                var app = System.Windows.Application.Current;
                app.Resources.MergedDictionaries.Clear();
                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Colors.xaml") });
                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Icons.xaml") });
                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Styles.xaml") });
                app.Resources["IconKeyToGeometryConverter"] = new EyeFocus.UI.Converters.IconKeyToGeometryConverter();
                app.Resources["TabToBooleanConverter"] = new EyeFocus.UI.Converters.TabToBooleanConverter();

                EyeFocus.UI.Themes.ThemeService.ApplyTheme("Light");

                // 1. Monitors View
                var monitorsView = new EyeFocus.UI.Views.MonitorsView();
                monitorsView.DataContext = new TestMonitorsVm();

                var monWindow = new System.Windows.Window
                {
                    Width = 840,
                    Height = 650,
                    Content = monitorsView,
                    Background = (System.Windows.Media.Brush)app.Resources["SurfaceBrush"]
                };
                monWindow.Measure(new System.Windows.Size(840, 650));
                monWindow.Arrange(new System.Windows.Rect(0, 0, 840, 650));
                monWindow.UpdateLayout();
                Assert.NotNull(monWindow);

                // 2. About View
                var aboutView = new EyeFocus.UI.Views.AboutView();
                aboutView.DataContext = new EyeFocus.ViewModels.AboutViewModel();

                var aboutWindow = new System.Windows.Window
                {
                    Width = 840,
                    Height = 650,
                    Content = aboutView,
                    Background = (System.Windows.Media.Brush)app.Resources["SurfaceBrush"]
                };
                aboutWindow.Measure(new System.Windows.Size(840, 650));
                aboutWindow.Arrange(new System.Windows.Rect(0, 0, 840, 650));
                aboutWindow.UpdateLayout();
                Assert.NotNull(aboutWindow);

                // Verify Dark theme switching
                EyeFocus.UI.Themes.ThemeService.ApplyTheme("Dark");
                monWindow.UpdateLayout();
                aboutWindow.UpdateLayout();

                monWindow.Close();
                aboutWindow.Close();
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            bool finished = thread.Join(10000);
            Assert.True(finished, "STA thread timed out rendering tabs");
        }

        public class TestMonitorsVm
        {
            public System.Collections.ObjectModel.ObservableCollection<EyeFocus.Models.MonitorInfo> Monitors { get; } = new();
            public System.Windows.Input.ICommand RefreshMonitorsCommand { get; } = new EyeFocus.ViewModels.RelayCommand(() => { });
            public System.Windows.Input.ICommand OpenDisplaySettingsCommand { get; } = new EyeFocus.ViewModels.RelayCommand<EyeFocus.Models.MonitorInfo>(_ => { });
            public System.Windows.Input.ICommand IdentifyMonitorCommand { get; } = new EyeFocus.ViewModels.RelayCommand<EyeFocus.Models.MonitorInfo>(_ => { });
            public System.Windows.Input.ICommand RedetectMonitorCommand { get; } = new EyeFocus.ViewModels.RelayCommand<EyeFocus.Models.MonitorInfo>(_ => { });
            public System.Windows.Input.ICommand CopySpecsCommand { get; } = new EyeFocus.ViewModels.RelayCommand<EyeFocus.Models.MonitorInfo>(_ => { });

            public TestMonitorsVm()
            {
                Monitors.Add(new EyeFocus.Models.MonitorInfo
                {
                    FriendlyName = "Generic PnP Monitor",
                    DeviceName = @"\\.\DISPLAY1",
                    IsPrimary = true,
                    Width = 2560,
                    Height = 1440,
                    RefreshRate = 144,
                    DpiScaleX = 1.0,
                    DpiScaleY = 1.0,
                    BitsPerPixel = 32,
                    ConnectionType = "DisplayPort",
                    SupportsDdcCi = true,
                    SupportsHardwareBrightness = true,
                    SupportsHardwareColorTemperature = true,
                    SupportsGamma = true,
                    SupportsSoftwareDimming = true,
                    Manufacturer = "Generic",
                    Serial = "Unknown",
                    IsHdrActive = false
                });
            }
        }

        [Fact]
        public void MonitorsViewModel_MoreOptionsCommands_CanExecuteWithoutError()
        {
            var mockDisplay = new QuickActionsAndRedesignTests.FakeDisplayEngine();
            var mockProfile = new QuickActionsAndRedesignTests.FakeProfileManager();
            var vm = new EyeFocus.ViewModels.MonitorsViewModel(mockDisplay, mockProfile);

            Assert.NotNull(vm.OpenDisplaySettingsCommand);
            Assert.NotNull(vm.IdentifyMonitorCommand);
            Assert.NotNull(vm.RedetectMonitorCommand);
            Assert.NotNull(vm.CopySpecsCommand);

            var mon = vm.Monitors.FirstOrDefault();
            Assert.NotNull(mon);

            // Verify clean device name
            Assert.Equal("DISPLAY1", mon.CleanDeviceName);

            // Verify commands can execute
            Assert.True(vm.RedetectMonitorCommand.CanExecute(mon));
            vm.RedetectMonitorCommand.Execute(mon);

            Assert.True(vm.CopySpecsCommand.CanExecute(mon));
            // Run CopySpecs on STA thread
            var t = new System.Threading.Thread(() =>
            {
                vm.CopySpecsCommand.Execute(mon);
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join();
        }

        [Fact]
        public void MainWindow_WithAllServices_CanInstantiateSuccessfully()
        {
            Exception? caught = null;
            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    var app = System.Windows.Application.Current ?? new System.Windows.Application();
                    app.Resources.MergedDictionaries.Clear();
                    app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Colors.xaml") });
                    app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Icons.xaml") });
                    app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Styles.xaml") });
                    app.Resources["IconKeyToGeometryConverter"] = new EyeFocus.UI.Converters.IconKeyToGeometryConverter();
                    app.Resources["TabToBooleanConverter"] = new EyeFocus.UI.Converters.TabToBooleanConverter();
                    
                    EyeFocus.UI.Themes.ThemeService.ApplyTheme("Light");
                    
                    var fakeEngine = new QuickActionsAndRedesignTests.FakeDisplayEngine();
                    var fakeProfile = new QuickActionsAndRedesignTests.FakeProfileManager();
                    var fakeStore = new QuickActionsAndRedesignTests.FakeSettingsStore();
                    var fakeHotkey = new HotkeyManager(fakeStore);
                    var fakeStartup = new StartupManager();
                    var fakeDayNight = new QuickActionsAndRedesignTests.FakeAutoDayNightService();
                    var mainVm = new EyeFocus.ViewModels.MainViewModel(fakeEngine, fakeProfile, fakeHotkey, fakeStore, fakeStartup, fakeDayNight);
                    
                    var win = new EyeFocus.MainWindow(mainVm, fakeStore, fakeHotkey);
                    Assert.NotNull(win);
                    win.Close();
                }
                catch (Exception ex)
                {
                    caught = ex;
                }
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            bool finished = t.Join(10000);
            Assert.True(finished, "STA thread timed out");
            if (caught != null) throw caught;
        }
    }
}
