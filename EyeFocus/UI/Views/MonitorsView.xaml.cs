using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EyeFocus.Models;
using EyeFocus.ViewModels;

namespace EyeFocus.UI.Views
{
    public partial class MonitorsView : System.Windows.Controls.UserControl
    {
        public MonitorsView()
        {
            InitializeComponent();
        }

        private void MoreOptionsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private MonitorInfo? GetTargetMonitor(object sender)
        {
            if (sender is MenuItem menuItem)
            {
                var ctx = menuItem.Parent as ContextMenu;
                if (ctx?.PlacementTarget is FrameworkElement target && target.DataContext is MonitorInfo monitor)
                {
                    return monitor;
                }
            }
            return GetViewModel()?.Monitors.FirstOrDefault();
        }

        private MonitorsViewModel? GetViewModel()
        {
            return DataContext as MonitorsViewModel;
        }

        private void MenuItem_OpenDisplaySettings_Click(object sender, RoutedEventArgs e)
        {
            var vm = GetViewModel();
            var mon = GetTargetMonitor(sender);
            vm?.OpenDisplaySettingsCommand.Execute(mon);
        }

        private void MenuItem_IdentifyDisplay_Click(object sender, RoutedEventArgs e)
        {
            var vm = GetViewModel();
            var mon = GetTargetMonitor(sender);
            vm?.IdentifyMonitorCommand.Execute(mon);
        }

        private void MenuItem_RedetectCapabilities_Click(object sender, RoutedEventArgs e)
        {
            var vm = GetViewModel();
            var mon = GetTargetMonitor(sender);
            vm?.RedetectMonitorCommand.Execute(mon);
        }

        private void MenuItem_CopySpecs_Click(object sender, RoutedEventArgs e)
        {
            var vm = GetViewModel();
            var mon = GetTargetMonitor(sender);
            vm?.CopySpecsCommand.Execute(mon);
        }
    }
}
