using System;
using System.Windows;

namespace EyeFocus.Services
{
    public class SnackbarService
    {
        private static SnackbarService? _instance;
        public static SnackbarService Instance => _instance ??= new SnackbarService();

        private System.Timers.Timer? _dismissTimer;

        public event Action<string, string?, Action?>? MessageRequested;
        public event Action? DismissRequested;

        public void Show(string message, string? actionText = null, Action? actionCallback = null, int durationMs = 3500)
        {
            _dismissTimer?.Stop();
            _dismissTimer?.Dispose();

            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                MessageRequested?.Invoke(message, actionText, actionCallback);
            });

            _dismissTimer = new System.Timers.Timer(durationMs) { AutoReset = false };
            _dismissTimer.Elapsed += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    DismissRequested?.Invoke();
                });
            };
            _dismissTimer.Start();
        }

        public void Dismiss()
        {
            _dismissTimer?.Stop();
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                DismissRequested?.Invoke();
            });
        }
    }
}
