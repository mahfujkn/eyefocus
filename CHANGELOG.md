# Changelog

All notable changes to **EyeFocus** will be documented in this file.

## [1.1.0] - 2026-10-01 (Modern Glassmorphism Redesign & Iconography Polish)

### Added & Improved
- **Modern Dual-Icon Sliding Theme Pill Switch**:
  - Replaced legacy toggle switch with a custom glassmorphism pill toggle featuring animated sliding thumb disk.
  - Symmetrical upright Sun and Moon icons with concentric circular alignment.
  - Smooth 220ms CubicEase animation transitioning between Light and Dark mode.
- **Dedicated Color Temperature & Brightness Iconography**:
  - **Color Temperature**: Distinct Snowflake (Cool light 6500K / Sky Blue `#38BDF8`) and Flame (Warm light 2700K / Amber `#F59E0B`) vector icons.
  - **Brightness**: Crisp, equalized 15x15 radiant Sun vector icons with signature EyeFocus primary teal theme accent.
- **Display Selector & Live Status Indicator**:
  - Replaced native combo box with a modern pill-shaped dropdown with monitor outline icon.
  - Added live glowing `● Connected` emerald status badge in the header bar.
- **Modern Pill Toast / Snackbar Notifications**:
  - Redesigned status messages to a floating capsule pill with dynamic leading accent icon (Sun, Moon, Shield, Sparkles) and clean frameless close button.
- **Single-Instance Enforcement & Smart Window Activation**:
  - System-wide named Mutex and Inter-Process Communication (IPC) prevent duplicate processes and duplicate tray icons when launching EyeFocus from Windows Search or Start Menu.
  - Automatically signals and restores the running application to the foreground.
- **Official Windows Installer Setup (.EXE)**:
  - Official setup wizard installer (`EyeFocus-v1.1.0-Setup.exe`) alongside standalone portable archive.
  - Clean Windows startup task integration with seamless in-app settings toggle synchronization and single Task Manager entry.
- **Comprehensive Quality Assurance**:
  - 58 passing automated unit tests covering display gamma, brightness transitions, theme switching, schedule evaluation, and profile storage.

## [1.0.0] - 2026-08-18 (Initial Release)

### Added & Improved
- **Default Profile Set to "Comfort"**:
  - Balanced everyday profile (4200K / 40% brightness / 5% software dim / warm-balanced RGB).
  - Preserved non-medical, display-comfort product positioning.
- **Header Bar & Display Selector**:
  - Live display dropdown selector with friendly monitor names and DDC/CI / WMI badges.
  - Emerald connected status badge in top bar.
- **Display Dashboard**:
  - Compact, 1080p laptop-optimized layout with consistent spacing.
  - Clear hardware control badge distinguishing `Hardware · DDC/CI ✓` from `Software Dimming · Active`.
  - Day & Night cards displaying profile parameters and scheduled times.
  - 3x3 Profile card grid featuring vector icons, active checkmarks (`✓`), and accent borders.
  - Dedicated "Restore Display" action button.
- **24-Hour Visual Timeline in Auto Mode**:
  - Integrated 24-hour visual distribution bar with hour markers.
  - Global timezone selector supporting city, country, and UTC offset search alongside Windows system timezone detection.
- **Privacy by Design Guarantees**:
  - 100% offline local operation: zero accounts, zero telemetry, zero analytics, zero cloud, zero tracking.
  - Atomic JSON storage strictly inside Windows local AppData.
- **Automated Verification**:
  - Passing automated unit tests across Kelvin calculations, gamma monotonicity, schedule resolution across midnight, and atomic storage recovery.
