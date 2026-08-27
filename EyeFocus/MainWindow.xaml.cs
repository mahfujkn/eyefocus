using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using EyeFocus.Storage;
using EyeFocus.SystemIntegration;
using EyeFocus.ViewModels;

namespace EyeFocus
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ISettingsStore _settingsStore;
        private readonly IHotkeyManager _hotkeyManager;
        private bool _isExplicitExit = false;

        public MainWindow(
            MainViewModel mainViewModel,
            ISettingsStore settingsStore,
            IHotkeyManager hotkeyManager)
        {
            InitializeComponent();
            _mainViewModel = mainViewModel;
            _settingsStore = settingsStore;
            _hotkeyManager = hotkeyManager;

            DataContext = _mainViewModel;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            _hotkeyManager.Initialize(helper.Handle);
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            var settings = _settingsStore.Load();
            if (settings.MinimizeToTray)
            {
                this.Hide();
            }
            else
            {
                Close();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var settings = _settingsStore.Load();
            if (settings.MinimizeToTray && !_isExplicitExit)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                base.OnClosing(e);
            }
        }

        public void ForceExit()
        {
            _isExplicitExit = true;
            Close();
            System.Windows.Application.Current?.Shutdown();
        }

        public void ShowAndRestore()
        {
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
        }
    }
}
