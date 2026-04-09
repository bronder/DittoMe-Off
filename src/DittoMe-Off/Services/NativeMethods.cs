using System.Runtime.InteropServices;

namespace DittoMeOff.Services;

/// <summary>
/// Centralized P/Invoke declarations for Win32 APIs used throughout the application.
/// All native method signatures are consolidated here to avoid duplication.
/// </summary>
internal static class NativeMethods
{
    // Window management
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    internal static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    // Keyboard input
    [DllImport("user32.dll")]
    internal static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    // Cursor
    [DllImport("user32.dll")]
    internal static extern bool GetCursorPos(out POINT lpPoint);

    // Clipboard
    [DllImport("user32.dll")]
    internal static extern IntPtr GetClipboardSequenceNumber();

    // Hotkey
    [DllImport("user32.dll")]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Constants
    internal const int SW_RESTORE = 9;
    internal const uint KEYEVENTF_KEYDOWN = 0x0000;
    internal const uint KEYEVENTF_KEYUP = 0x0002;
    internal const uint INPUT_KEYBOARD = 1;
    internal const ushort VK_CONTROL = 0x11;
    internal const ushort VK_V = 0x56;

    internal static readonly IntPtr HWND_TOP = IntPtr.Zero;
    internal const uint SWP_NOZORDER = 0x0004;
    internal const uint SWP_SHOWWINDOW = 0x0040;

    // WM_NCHITTEST hit test values for borderless window resizing
    internal const int WM_NCHITTEST = 0x0084;
    internal const int HTLEFT = 10;
    internal const int HTRIGHT = 11;
    internal const int HTTOP = 12;
    internal const int HTTOPLEFT = 13;
    internal const int HTTOPRIGHT = 14;
    internal const int HTBOTTOM = 15;
    internal const int HTBOTTOMLEFT = 16;
    internal const int HTBOTTOMRIGHT = 17;
    internal const int HTCLIENT = 1;

    // Structs
    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
