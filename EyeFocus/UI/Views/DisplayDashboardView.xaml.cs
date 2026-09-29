using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace EyeFocus.UI.Views
{
    public partial class DisplayDashboardView : System.Windows.Controls.UserControl
    {
        public DisplayDashboardView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (DataContext is ViewModels.DisplayDashboardViewModel vm)
                {
                    vm.StartTimelineClock();
                }
            };
            Unloaded += (s, e) =>
            {
                if (DataContext is ViewModels.DisplayDashboardViewModel vm)
                {
                    vm.StopTimelineClock();
                }
            };
            DataContextChanged += (s, e) =>
            {
                if (e.NewValue is ViewModels.DisplayDashboardViewModel vm && IsLoaded)
                {
                    vm.StartTimelineClock();
                }
            };
        }

        private void OnBrightnessSliderMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is Slider slider)
            {
                slider.Value = Math.Clamp(slider.Value + (e.Delta > 0 ? 2 : -2), slider.Minimum, slider.Maximum);
                e.Handled = true;
            }
        }

        private void OnKelvinSliderMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is Slider slider)
            {
                slider.Value = Math.Clamp(slider.Value + (e.Delta > 0 ? 50 : -50), slider.Minimum, slider.Maximum);
                e.Handled = true;
            }
        }

        private void OnSliderPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                // If user clicked directly on the thumb, let default dragging handle it
                if (e.OriginalSource is System.Windows.DependencyObject d && FindParent<System.Windows.Controls.Primitives.Thumb>(d) != null)
                {
                    return;
                }

                // Clicked on track: jump directly to clicked position
                var pos = e.GetPosition(slider);
                double ratio = pos.X / slider.ActualWidth;
                double val = slider.Minimum + (slider.Maximum - slider.Minimum) * Math.Clamp(ratio, 0.0, 1.0);

                if (slider.SmallChange >= 1)
                {
                    val = Math.Round(val / slider.SmallChange) * slider.SmallChange;
                }

                slider.Value = Math.Clamp(val, slider.Minimum, slider.Maximum);
                slider.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnSliderMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Slider slider && slider.IsMouseCaptured)
            {
                var pos = e.GetPosition(slider);
                double ratio = pos.X / slider.ActualWidth;
                double val = slider.Minimum + (slider.Maximum - slider.Minimum) * Math.Clamp(ratio, 0.0, 1.0);

                if (slider.SmallChange >= 1)
                {
                    val = Math.Round(val / slider.SmallChange) * slider.SmallChange;
                }

                slider.Value = Math.Clamp(val, slider.Minimum, slider.Maximum);
            }
        }

        private void OnSliderPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider && slider.IsMouseCaptured)
            {
                slider.ReleaseMouseCapture();
            }
        }

        private static T? FindParent<T>(System.Windows.DependencyObject? child) where T : System.Windows.DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}
