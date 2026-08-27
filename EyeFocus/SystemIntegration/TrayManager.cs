using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EyeFocus.Automation;
using EyeFocus.Display;
using EyeFocus.Models;
using EyeFocus.Profiles;
using EyeFocus.Storage;
using EyeFocus.UI.Themes;

namespace EyeFocus.SystemIntegration
{
    public class TrayManager : ITrayManager
    {
        private readonly IProfileManager _profileManager;
        private readonly IDisplayEngine _displayEngine;
        private readonly ISettingsStore _settingsStore;
        private readonly IAutoDayNightService? _autoDayNightService;

        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;

        private static readonly Font HeaderFont = new("Segoe UI", 10f, FontStyle.Bold);
        private static readonly Font PrimaryBoldFont = new("Segoe UI", 9.5f, FontStyle.Bold);
        private static readonly Font RegularFont = new("Segoe UI", 9f, FontStyle.Regular);
        private static readonly Font SubtitleFont = new("Segoe UI", 8.5f, FontStyle.Regular);

        public event EventHandler? OpenRequested;
        public event EventHandler? SettingsRequested;
        public event EventHandler? ExitRequested;

        public TrayManager(
            IProfileManager profileManager,
            IDisplayEngine displayEngine,
            ISettingsStore settingsStore,
            IAutoDayNightService? autoDayNightService = null)
        {
            _profileManager = profileManager;
            _displayEngine = displayEngine;
            _settingsStore = settingsStore;
            _autoDayNightService = autoDayNightService;

            _profileManager.ActiveProfileChanged += (s, p) => UpdateTrayState();
            _profileManager.ProfilesListChanged += (s, e) => UpdateTrayState();
        }

        public void Initialize()
        {
            _notifyIcon = new NotifyIcon
            {
                Text = "EyeFocus - Display Comfort Utility",
                Visible = true,
                Icon = CreateAppIcon()
            };

            _notifyIcon.DoubleClick += (s, e) => OpenRequested?.Invoke(this, EventArgs.Empty);
            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    OpenRequested?.Invoke(this, EventArgs.Empty);
                }
            };

            BuildContextMenu();
            LogService.Info("TrayManager initialized with modern Segoe UI typography and custom icon.");
        }

        private void BuildContextMenu()
        {
            _contextMenu = new ContextMenuStrip
            {
                Font = RegularFont,
                ShowImageMargin = true,
                RenderMode = ToolStripRenderMode.Professional,
                Renderer = new ModernMenuRenderer()
            };

            var settings = _settingsStore.Load();
            var activeProfile = _profileManager.GetActiveProfile();
            var dayProfile = _profileManager.GetProfile(settings.DayProfileId);
            var nightProfile = _profileManager.GetProfile(settings.NightProfileId);

            // 1. App Header
            var headerItem = new ToolStripMenuItem("EyeFocus")
            {
                Font = HeaderFont,
                ForeColor = Color.FromArgb(0, 104, 116),
                Padding = new Padding(6, 4, 6, 2)
            };
            _contextMenu.Items.Add(headerItem);

            // 2. Active Profile Info
            var currentProfileItem = new ToolStripMenuItem($"Current: {activeProfile.Name} ({activeProfile.Kelvin}K · {activeProfile.Brightness}%)")
            {
                Font = SubtitleFont,
                Enabled = false,
                Padding = new Padding(6, 0, 6, 4)
            };
            _contextMenu.Items.Add(currentProfileItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // 3. Quick Day / Night Presets
            string dayText = dayProfile != null ? $"Day Mode ({dayProfile.Kelvin}K · {dayProfile.Brightness}%)" : "Day Mode (5000K · 70%)";
            var dayItem = new ToolStripMenuItem(dayText)
            {
                Font = RegularFont,
                Checked = activeProfile.Id == settings.DayProfileId
            };
            dayItem.Click += (s, e) =>
            {
                if (_autoDayNightService != null && _autoDayNightService.IsEnabled)
                {
                    _autoDayNightService.SetEnabled(false);
                }
                _profileManager.SetActiveProfile(settings.DayProfileId);
                var p = _profileManager.GetProfile(settings.DayProfileId);
                if (p != null)
                {
                    _displayEngine.ApplyProfile(p);
                }
                UpdateTrayState();
            };
            _contextMenu.Items.Add(dayItem);

            string nightText = nightProfile != null ? $"Night Mode ({nightProfile.Kelvin}K · {nightProfile.Brightness}%)" : "Night Mode (3000K · 40%)";
            var nightItem = new ToolStripMenuItem(nightText)
            {
                Font = RegularFont,
                Checked = activeProfile.Id == settings.NightProfileId
            };
            nightItem.Click += (s, e) =>
            {
                if (_autoDayNightService != null && _autoDayNightService.IsEnabled)
                {
                    _autoDayNightService.SetEnabled(false);
                }
                _profileManager.SetActiveProfile(settings.NightProfileId);
                var p = _profileManager.GetProfile(settings.NightProfileId);
                if (p != null)
                {
                    _displayEngine.ApplyProfile(p);
                }
                UpdateTrayState();
            };
            _contextMenu.Items.Add(nightItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // 4. Profiles Submenu
            var profilesMenu = new ToolStripMenuItem("Display Profiles")
            {
                Font = RegularFont
            };
            var allProfiles = _profileManager.GetAllProfiles();
            foreach (var profile in allProfiles)
            {
                var pItem = new ToolStripMenuItem($"{profile.Name} ({profile.Kelvin}K · {profile.Brightness}%)")
                {
                    Font = RegularFont,
                    Checked = profile.Id == activeProfile.Id
                };
                var capturedId = profile.Id;
                pItem.Click += (s, e) =>
                {
                    if (_autoDayNightService != null && _autoDayNightService.IsEnabled)
                    {
                        _autoDayNightService.SetEnabled(false);
                    }
                    _profileManager.SetActiveProfile(capturedId);
                    var target = _profileManager.GetProfile(capturedId);
                    if (target != null)
                    {
                        _displayEngine.ApplyProfile(target);
                    }
                    UpdateTrayState();
                };
                profilesMenu.DropDownItems.Add(pItem);
            }
            _contextMenu.Items.Add(profilesMenu);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // 5. Open EyeFocus
            var openItem = new ToolStripMenuItem("Open EyeFocus", null, (s, e) => OpenRequested?.Invoke(this, EventArgs.Empty))
            {
                Font = PrimaryBoldFont
            };
            _contextMenu.Items.Add(openItem);

            // 6. Settings
            var settingsItem = new ToolStripMenuItem("Settings", null, (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty))
            {
                Font = RegularFont
            };
            _contextMenu.Items.Add(settingsItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // 7. Exit
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty))
            {
                Font = RegularFont
            };
            _contextMenu.Items.Add(exitItem);

            _notifyIcon!.ContextMenuStrip = _contextMenu;
        }

        public void UpdateTrayState()
        {
            if (_notifyIcon == null) return;
            var activeProfile = _profileManager.GetActiveProfile();
            var title = $"EyeFocus - {activeProfile.Name} ({activeProfile.Kelvin}K, {activeProfile.Brightness}%)";
            _notifyIcon.Text = title.Length > 63 ? title.Substring(0, 63) : title;
            BuildContextMenu();
        }

        public void ShowNotification(string title, string message)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }

        private Icon CreateAppIcon()
        {
            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath, new Size(32, 32));
                }

                var procPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(procPath) && File.Exists(procPath))
                {
                    var extracted = Icon.ExtractAssociatedIcon(procPath);
                    if (extracted != null) return extracted;
                }
            }
            catch (Exception ex)
            {
                LogService.Warn($"Could not load custom icon from Assets/app.ico: {ex.Message}");
            }

            // Procedural fallback with smooth antialiasing
            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using var bgBrush = new SolidBrush(Color.FromArgb(14, 28, 48));
                g.FillEllipse(bgBrush, 1, 1, 30, 30);

                using var pen = new Pen(Color.FromArgb(0, 210, 255), 2.5f);
                g.DrawArc(pen, 4, 8, 24, 16, 20, 140);
                g.DrawArc(pen, 4, 8, 24, 16, 200, 140);

                using var irisBrush = new SolidBrush(Color.FromArgb(255, 180, 50));
                g.FillEllipse(irisBrush, 11, 11, 10, 10);

                using var pupilBrush = new SolidBrush(Color.FromArgb(14, 28, 48));
                g.FillEllipse(pupilBrush, 14, 14, 4, 4);
            }

            var hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            _contextMenu?.Dispose();
        }
    }

    public class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        public ModernMenuRenderer() : base(new ModernColorTable())
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            base.OnRenderItemText(e);
        }
    }

    public class ModernColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(255, 255, 255);
        public override Color ImageMarginGradientBegin => Color.FromArgb(245, 248, 248);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(245, 248, 248);
        public override Color ImageMarginGradientEnd => Color.FromArgb(245, 248, 248);
        public override Color MenuItemSelected => Color.FromArgb(220, 240, 244);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(220, 240, 244);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(220, 240, 244);
        public override Color MenuItemBorder => Color.FromArgb(180, 220, 228);
        public override Color MenuBorder => Color.FromArgb(210, 220, 224);
        public override Color SeparatorDark => Color.FromArgb(225, 232, 235);
        public override Color SeparatorLight => Color.FromArgb(255, 255, 255);
    }
}
