using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DittoMeOff.Services;

public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000;
    
    private HwndSource? _source;
    private IntPtr _windowHandle;
    private bool _isRegistered;

    public event EventHandler? HotkeyPressed;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [Flags]
    private enum KeyModifiers : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }

    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _windowHandle = helper.Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(HwndHook);
    }

    public bool RegisterHotkey(string hotkeyString)
    {
        if (_isRegistered)
        {
            UnregisterHotkey();
        }

        var (modifiers, key) = ParseHotkey(hotkeyString);
        if (key == 0) return false;

        _isRegistered = RegisterHotKey(_windowHandle, HOTKEY_ID, (uint)modifiers, key);
        return _isRegistered;
    }

    public void UnregisterHotkey()
    {
        if (_isRegistered)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            _isRegistered = false;
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private (KeyModifiers modifiers, uint key) ParseHotkey(string hotkeyString)
    {
        KeyModifiers modifiers = KeyModifiers.None;
        uint key = 0;

        var parts = hotkeyString.Split('+');
        foreach (var part in parts)
        {
            var trimmed = part.Trim().ToLower();
            switch (trimmed)
            {
                case "ctrl":
                case "control":
                    modifiers |= KeyModifiers.Control;
                    break;
                case "alt":
                    modifiers |= KeyModifiers.Alt;
                    break;
                case "shift":
                    modifiers |= KeyModifiers.Shift;
                    break;
                case "win":
                case "windows":
                    modifiers |= KeyModifiers.Win;
                    break;
                default:
                    if (trimmed.Length == 1)
                    {
                        key = (uint)char.ToUpper(trimmed[0]);
                    }
                    else if (Enum.TryParse<System.Windows.Input.Key>(trimmed, true, out var wpfKey))
                    {
                        key = (uint)KeyInterop.VirtualKeyFromKey(wpfKey);
                    }
                    break;
            }
        }

        return (modifiers, key);
    }

    public void Dispose()
    {
        UnregisterHotkey();
        _source?.RemoveHook(HwndHook);
    }
}
