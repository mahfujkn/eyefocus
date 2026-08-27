using System;
using System.Windows;
using System.Windows.Interop;
using EyeFocus.Display.Native;
using EyeFocus.Models;

namespace EyeFocus.UI.Overlays
{
    public partial class DimmerOverlayWindow : Window
    {
        public string StableMonitorId { get; set; } = string.Empty;

        public DimmerOverlayWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;

            // Make window click-through, tool window, transparent, no-activate
            var exStyle = User32Native.GetWindowLongPtr(hwnd, User32Native.GWL_EXSTYLE).ToInt64();
            exStyle |= User32Native.WS_EX_TRANSPARENT | User32Native.WS_EX_LAYERED | User32Native.WS_EX_TOOLWINDOW | User32Native.WS_EX_NOACTIVATE;
            User32Native.SetWindowLongPtr(hwnd, User32Native.GWL_EXSTYLE, new IntPtr(exStyle));

            // Place topmost without activating
            User32Native.SetWindowPos(hwnd, User32Native.HWND_TOPMOST, 0, 0, 0, 0,
                User32Native.SWP_NOMOVE | User32Native.SWP_NOSIZE | User32Native.SWP_NOACTIVATE | User32Native.SWP_SHOWWINDOW);
        }

        public void UpdateBoundsAndDpi(MonitorInfo monitor)
        {
            double scaleX = monitor.DpiScaleX > 0 ? monitor.DpiScaleX : 1.0;
            double scaleY = monitor.DpiScaleY > 0 ? monitor.DpiScaleY : 1.0;

            // Convert physical pixels to WPF DIPs
            this.Left = monitor.Left / scaleX;
            this.Top = monitor.Top / scaleY;
            this.Width = monitor.Width / scaleX;
            this.Height = monitor.Height / scaleY;
        }

        public void SetDimLevel(int dimPercent)
        {
            dimPercent = Math.Clamp(dimPercent, 0, 100);
            if (dimPercent <= 0)
            {
                OverlayGrid.Opacity = 0.0;
                this.Hide();
            }
            else
            {
                // Max dimming opacity 0.85 to avoid completely black screen
                double targetOpacity = (dimPercent / 100.0) * 0.85;
                OverlayGrid.Opacity = targetOpacity;

                if (!this.IsVisible)
                {
                    this.Show();
                }
            }
        }
    }
}
