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
- **Comprehensive Quality Assurance**:
  - 58 passing automated unit tests covering display gamma, brightness transitions, theme switching, schedule evaluation, and profile storage.

## [1.0.1] - 2026-08-18 (Final UI/UX & Production Refinement)

### Added & Improved
- **Default Profile Updated to "Comfort"**:
  - Replaced Health profile with **Comfort** (4200K / 40% brightness / 5% software dim / warm-balanced RGB).
  - Preserved non-medical, display-comfort product positioning.
- **Header Bar Redesign**:
  - Replaced ambiguous OFFLINE warning badge with a clean `● LOCAL` indicator badge.
  - Added live display dropdown selector with friendly monitor names and DDC/CI / WMI badges.
  - Added dedicated `[Manage]` action button navigating directly to monitor capabilities.
- **Display Dashboard Layout Refinements**:
  - Compact, 1080p laptop-optimized layout (minimal scrolling, consistent 8/12/16/20/24px spacing).
  - Clear hardware control badge distinguishing `Hardware · DDC/CI ✓` from `Software Dimming · Active`.
  - Day & Night cards now display actual profile parameters (`6500K · 85%` / `3000K · 40%`) and scheduled times.
  - 3x3 Profile card grid featuring vector Fluent/Lucide icons, active checkmarks (`✓`), accent borders, and subtle glows.
  - Compact 4-column active status footer displaying Active Profile, Display, Control Method, and Next Auto Mode event.
  - Replaced ambiguous "Reset Display" button with clear **"Restore Display"** action.
- **24-Hour Visual Timeline in Auto Mode**:
  - Integrated 24-hour visual distribution bar with hour markers (00, 06, 12, 18, 21, 24).
  - Global timezone selector supporting city, country, and UTC offset search alongside Windows system timezone detection.
- **"Your Privacy" Product Identity Card**:
  - Added dedicated card in Settings confirming zero accounts, zero telemetry, zero analytics, zero cloud, zero tracking, and 100% offline local operation.
- **Automated Verification**:
  - 22 passing automated tests across Kelvin calculations, gamma monotonicity, schedule resolution across midnight, and atomic storage recovery.

## [1.0.0] - 2026-08-18

### Initial Release
- Initial release of EyeFocus display comfort and automatic display mode utility for Windows.
