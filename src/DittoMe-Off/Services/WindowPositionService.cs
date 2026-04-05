using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DittoMeOff.Services;

public class WindowPositionService
{
    private static readonly IntPtr HWND_TOP = IntPtr.Zero;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    public void PositionWindowAtCursor(Window window, IConfigService configService)
    {
        GetCursorPos(out var cursorPos);
        var config = configService.Config;
        double windowWidth = config.WindowWidth > 0 ? config.WindowWidth : window.Width;
        double windowHeight = config.WindowHeight > 0 ? config.WindowHeight : window.Height;
        var screenBounds = GetScreenWorkingArea(cursorPos.X, cursorPos.Y);

        double centeredLeft = cursorPos.X - (windowWidth / 2);
        double centeredTop = cursorPos.Y - (windowHeight / 2);

        if (DoesWindowFitOnScreen(centeredLeft, centeredTop, windowWidth, windowHeight, screenBounds))
        {
            window.Left = centeredLeft;
            window.Top = centeredTop;
            EnsureWindowOnScreen(window, screenBounds, windowWidth, windowHeight);
            return;
        }

        double belowLeft = cursorPos.X;
        double belowTop = cursorPos.Y;
        if (DoesWindowFitOnScreen(belowLeft, belowTop, windowWidth, windowHeight, screenBounds))
        {
            window.Left = belowLeft;
            window.Top = belowTop;
            EnsureWindowOnScreen(window, screenBounds, windowWidth, windowHeight);
            return;
        }

        double aboveLeft = cursorPos.X - windowWidth;
        double aboveTop = cursorPos.Y - windowHeight;
        if (DoesWindowFitOnScreen(aboveLeft, aboveTop, windowWidth, windowHeight, screenBounds))
        {
            window.Left = aboveLeft;
            window.Top = aboveTop;
            EnsureWindowOnScreen(window, screenBounds, windowWidth, windowHeight);
            return;
        }

        window.Left = centeredLeft;
        window.Top = centeredTop;
        EnsureWindowOnScreen(window, screenBounds, windowWidth, windowHeight);
    }

    public void PositionWindowCenterScreen(Window window, IConfigService configService)
    {
        try
        {
            GetCursorPos(out var cursorPos);
            var screenBounds = GetScreenWorkingArea(cursorPos.X, cursorPos.Y);
            var config = configService.Config;
            double windowWidth = config.WindowWidth > 0 ? config.WindowWidth : window.Width;
            double windowHeight = config.WindowHeight > 0 ? config.WindowHeight : window.Height;
            window.Left = (screenBounds.Width - windowWidth) / 2 + screenBounds.Left;
            window.Top = (screenBounds.Height - windowHeight) / 2 + screenBounds.Top;
        }
        catch
        {
            window.Left = double.NaN;
            window.Top = double.NaN;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    public void ForceWindowPosition(Window window, IConfigService configService)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        var config = configService.Config;
        int width = config.WindowWidth > 0 ? (int)config.WindowWidth : (int)window.Width;
        int height = config.WindowHeight > 0 ? (int)config.WindowHeight : (int)window.Height;

        NativeMethods.SetWindowPos(hwnd, HWND_TOP, (int)window.Left, (int)window.Top, width, height, SWP_NOZORDER | SWP_SHOWWINDOW);
        window.Width = width;
        window.Height = height;
    }

    private static bool DoesWindowFitOnScreen(double left, double top, double width, double height, Rect screenBounds)
    {
        return left >= screenBounds.Left
            && top >= screenBounds.Top
            && left + width <= screenBounds.Right
            && top + height <= screenBounds.Bottom;
    }

    private static void EnsureWindowOnScreen(Window window, Rect screenBounds, double windowWidth, double windowHeight)
    {
        if (window.Left + windowWidth > screenBounds.Right)
            window.Left = screenBounds.Right - windowWidth;
        if (window.Left < screenBounds.Left)
            window.Left = screenBounds.Left;
        if (window.Top + windowHeight > screenBounds.Bottom)
            window.Top = screenBounds.Bottom - windowHeight;
        if (window.Top < screenBounds.Top)
            window.Top = screenBounds.Top;
    }

    private static Rect GetScreenWorkingArea(int x, int y)
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        var targetScreen = System.Windows.Forms.Screen.PrimaryScreen;

        foreach (var screen in screens)
        {
            var rect = screen.WorkingArea;
            if (x >= rect.Left && x < rect.Right && y >= rect.Top && y < rect.Bottom)
            {
                targetScreen = screen;
                break;
            }
        }

        return new Rect(
            targetScreen!.WorkingArea.Left,
            targetScreen.WorkingArea.Top,
            targetScreen.WorkingArea.Width,
            targetScreen.WorkingArea.Height);
    }
}
