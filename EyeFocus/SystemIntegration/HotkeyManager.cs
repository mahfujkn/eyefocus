using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Interop;
using EyeFocus.Display.Native;
using EyeFocus.Models;
using EyeFocus.Storage;

namespace EyeFocus.SystemIntegration
{
    public class HotkeyManager : IHotkeyManager
    {
        private readonly ISettingsStore _settingsStore;
        private IntPtr _hwnd = IntPtr.Zero;
        private HwndSource? _hwndSource;
        private readonly Dictionary<int, HotkeyAction> _registeredHotkeys = new();
        private int _currentHotkeyId = 9000;

        public event EventHandler<HotkeyAction>? HotkeyTriggered;

        public HotkeyManager(ISettingsStore settingsStore)
        {
            _settingsStore = settingsStore;
        }

        public void Initialize(IntPtr windowHandle)
        {
            _hwnd = windowHandle;
            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(HwndHook);
            ReloadHotkeys();
            LogService.Info("HotkeyManager initialized.");
        }

        public void ReloadHotkeys()
        {
            UnregisterAll();
            var settings = _settingsStore.Load();
            foreach (var binding in settings.Hotkeys)
            {
                if (binding.IsEnabled)
                {
                    RegisterBinding(binding, out var error);
                    if (!string.IsNullOrEmpty(error))
                    {
                        LogService.Warn($"Failed to register hotkey {binding.DisplayShortcut}: {error}");
                    }
                }
            }
        }

        public bool RegisterBinding(HotkeyBinding binding, out string? errorMessage)
        {
            errorMessage = null;
            if (_hwnd == IntPtr.Zero || !binding.IsEnabled || binding.Key == "None")
                return false;

            if (!Enum.TryParse<Key>(binding.Key, true, out var wpfKey))
            {
                errorMessage = $"Invalid key: {binding.Key}";
                return false;
            }

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(wpfKey);
            uint modifiers = User32Native.MOD_NOREPEAT;

            if (binding.ModifiersAlt) modifiers |= User32Native.MOD_ALT;
            if (binding.ModifiersCtrl) modifiers |= User32Native.MOD_CONTROL;
            if (binding.ModifiersShift) modifiers |= User32Native.MOD_SHIFT;
            if (binding.ModifiersWin) modifiers |= User32Native.MOD_WIN;

            int hotkeyId = ++_currentHotkeyId;
            bool success = User32Native.RegisterHotKey(_hwnd, hotkeyId, modifiers, vk);

            if (success)
            {
                _registeredHotkeys[hotkeyId] = binding.Action;
                LogService.Debug($"Registered hotkey [{binding.DisplayShortcut}] for {binding.ActionName}");
                return true;
            }
            else
            {
                errorMessage = "Shortcut unavailable. Another application may already be using it.";
                return false;
            }
        }

        public void UnregisterAll()
        {
            if (_hwnd != IntPtr.Zero)
            {
                foreach (var id in _registeredHotkeys.Keys)
                {
                    User32Native.UnregisterHotKey(_hwnd, id);
                }
            }
            _registeredHotkeys.Clear();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == User32Native.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_registeredHotkeys.TryGetValue(id, out var action))
                {
                    LogService.Info($"Hotkey pressed: {action}");
                    HotkeyTriggered?.Invoke(this, action);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterAll();
            _hwndSource?.RemoveHook(HwndHook);
            _hwndSource = null;
        }
    }
}
