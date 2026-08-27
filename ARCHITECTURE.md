# EyeFocus — Technical Architecture Deep Dive

## Overview

EyeFocus uses a layered, capability-based display control architecture designed for maximum reliability across heterogeneous hardware configurations.

```
┌─────────────────────────────────────────────────────────────┐
│                       EyeFocus UI (WPF)                     │
│    Dashboard  •  Auto Mode  •  Monitors  •  Profile Editor  │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│                    DisplayEngine Orchestrator               │
│                                                             │
│   Strategy Priority:                                        │
│   1. External Display: DDC/CI (DXVA2)                       │
│   2. Laptop Panel:     WMI (root\WMI WmiMonitorBrightness)  │
│   3. Color/Warmth:     GDI SetDeviceGammaRamp Fallback      │
│   4. Software Dim:     Click-Through Transparent Overlay    │
└──────────────────────────────┬──────────────────────────────┘
                               │
       ┌───────────────────────┼───────────────────────┐
       ▼                       ▼                       ▼
┌──────────────┐        ┌──────────────┐        ┌──────────────┐
│   DXVA2      │        │  WMI Driver  │        │   GDI DC     │
│   (DDC/CI)   │        │   (Laptop)   │        │ (Gamma Ramp) │
└──────────────┘        └──────────────┘        └──────────────┘
```

## Layered Display Strategy

1. **External Monitors (DDC/CI)**:
   - Uses `GetPhysicalMonitorsFromHMONITOR` and `SetMonitorBrightness`.
   - Rate-limited to max ~25 calls/sec per monitor to prevent bus contention.

2. **Internal Laptop Displays (WMI)**:
   - Queries `root\WMI` for `WmiMonitorBrightness` and calls `WmiSetBrightness` on `WmiMonitorBrightnessMethods`.

3. **Color Temperature & Gamma Fallback**:
   - Converts Kelvin (2500K - 7500K) to normalized Red, Green, and Blue multipliers using Tanner Helland's empirical blackbody radiation curves.
   - Generates a 256-step monotonic non-decreasing ramp table.
   - Applies the ramp to the target display's Device Context via `SetDeviceGammaRamp`.

4. **Software Dimmer Fallback**:
   - Spawns a dedicated WPF window per monitor styled with `WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`.
   - Transparently adjusts opacity over monitor bounds (supporting mixed DPI) without consuming CPU cycles or intercepting clicks.

5. **Safety Baseline Capture**:
   - Captures original gamma state on startup and restores on normal exit.
