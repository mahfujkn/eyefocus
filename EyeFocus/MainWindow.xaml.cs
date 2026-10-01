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

        private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            UpdateMaximizeRestoreButton();
        }

        private void UpdateMaximizeRestoreButton()
        {
            if (BtnMaximizeRestore == null) return;
            bool isMax = WindowState == WindowState.Maximized;
            BtnMaximizeRestore.Tag = FindResource(isMax ? "IconRestore" : "IconMaximize");
            BtnMaximizeRestore.ToolTip = isMax ? "Restore Down" : "Maximize / Fullscreen";
            if (RootBorder != null)
            {
                RootBorder.CornerRadius = isMax ? new CornerRadius(0) : new CornerRadius(12);
                RootBorder.BorderThickness = isMax ? new Thickness(0) : new Thickness(1);
            }
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
            var settings = _settingsStore?.Load();
            if (settings != null && settings.MinimizeToTray && !_isExplicitExit)
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
