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
    }
}
