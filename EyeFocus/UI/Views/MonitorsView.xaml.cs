using System.Windows.Controls;

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
    }
}
