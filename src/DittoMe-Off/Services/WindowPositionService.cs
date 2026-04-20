using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DittoMeOff.Services;

public class WindowPositionService : IWindowPositionService
{
    private static readonly IntPtr HWND_TOP = IntPtr.Zero;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

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

    /// <summary>
    /// Positions the window near the target window (the window that was active before the hotkey was pressed).
    /// Places the clipboard window to the right of the target window if possible,
    /// otherwise below, with on-screen validation.
    /// Note: GetWindowRect returns physical pixels, but WPF uses DIPs, so we need to scale.
    /// </summary>
    public void PositionWindowNearWindow(Window window, IConfigService configService, IntPtr targetWindowHandle)
    {
        var config = configService.Config;
        double windowWidth = config.WindowWidth > 0 ? config.WindowWidth : window.Width;
        double windowHeight = config.WindowHeight > 0 ? config.WindowHeight : window.Height;

        // Default to cursor position if target window is invalid
        GetCursorPos(out var cursorPos);

        // Get the DPI scaling factor to convert physical pixels to DIPs
        double dpiScale = GetDpiScale(window);

        // Convert screen bounds from pixels to DIPs
        var screenBoundsPixel = GetScreenWorkingArea(cursorPos.X, cursorPos.Y);
        var screenBounds = new Rect(
            screenBoundsPixel.Left / dpiScale,
            screenBoundsPixel.Top / dpiScale,
            screenBoundsPixel.Width / dpiScale,
            screenBoundsPixel.Height / dpiScale);

        if (targetWindowHandle != IntPtr.Zero && GetWindowRect(targetWindowHandle, out RECT targetRect))
        {
            // Convert physical pixel coordinates to DIPs
            double targetLeftDips = targetRect.Left / dpiScale;
            double targetTopDips = targetRect.Top / dpiScale;
            double targetRightDips = targetRect.Right / dpiScale;
            double targetBottomDips = targetRect.Bottom / dpiScale;

            // Try to position to the right of the target window
            double rightPos = targetRightDips + 10;
            double topPos = targetTopDips;

            if (DoesWindowFitOnScreen(rightPos, topPos, windowWidth, windowHeight, screenBounds))
            {
                window.Left = rightPos;
                window.Top = topPos;
                EnsureWindowOnScreen(window, screenBounds, windowWidth, windowHeight);
                return;
            }

            // Try to position below the target window (aligned to left edge)
            double belowLeft = targetLeftDips;
            double belowTop = targetBottomDips + 10;

            if (DoesWindowFitOnScreen(belowLeft, belowTop, windowWidth, windowHeight, screenBounds))
            {
                window.Left = belowLeft;
                window.Top = belowTop;
                EnsureWindowOnScreen(window, screenBounds, windowWidth, windowHeight);
                return;
            }

            // Try to position to the left of the target window
            double leftPos = targetLeftDips - windowWidth - 10;

            if (DoesWindowFitOnScreen(leftPos, targetTopDips, windowWidth, windowHeight, screenBounds))
            {
                window.Left = leftPos;
                window.Top = targetTopDips;
                EnsureWindowOnScreen(window, screenBounds, windowWidth, windowHeight);
                return;
            }

            // Try to position above the target window
            double aboveTop = targetTopDips - windowHeight - 10;

            if (DoesWindowFitOnScreen(targetLeftDips, aboveTop, windowWidth, windowHeight, screenBounds))
            {
                window.Left = targetLeftDips;
                window.Top = aboveTop;
                EnsureWindowOnScreen(window, screenBounds, windowWidth, windowHeight);
                return;
            }
        }

        // Fallback: position centered at cursor (convert cursor pos from pixels to DIPs)
        double centeredLeft = (cursorPos.X / dpiScale) - (windowWidth / 2);
        double centeredTop = (cursorPos.Y / dpiScale) - (windowHeight / 2);

        window.Left = centeredLeft;
        window.Top = centeredTop;
        EnsureWindowOnScreen(window, screenBounds, windowWidth, windowHeight);
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

    /// <summary>
    /// Gets the DPI scaling factor for the window.
    /// At 100% DPI (96 DPI), this returns 1.0. At 150% DPI (144 DPI), this returns 1.5, etc.
    /// </summary>
    private static double GetDpiScale(Window window)
    {
        var source = PresentationSource.FromVisual(window);
        if (source?.CompositionTarget != null)
        {
            return source.CompositionTarget.TransformToDevice.M11;
        }
        return 1.0; // Default to no scaling if we can't determine the DPI
    }

    /// <summary>
    /// Positions the window centered horizontally at the top of the screen with a 100px buffer.
    /// </summary>
    public void PositionWindowAtTopCenter(Window window, IConfigService configService)
    {
        try
        {
            GetCursorPos(out var cursorPos);
            var config = configService.Config;
            double windowWidth = config.WindowWidth > 0 ? config.WindowWidth : window.Width;
            double windowHeight = config.WindowHeight > 0 ? config.WindowHeight : window.Height;

            // Get the DPI scaling factor to convert physical pixels to DIPs
            double dpiScale = GetDpiScale(window);

            // Convert screen bounds from pixels to DIPs
            var screenBoundsPixel = GetScreenWorkingArea(cursorPos.X, cursorPos.Y);
            var screenBounds = new Rect(
                screenBoundsPixel.Left / dpiScale,
                screenBoundsPixel.Top / dpiScale,
                screenBoundsPixel.Width / dpiScale,
                screenBoundsPixel.Height / dpiScale);

            // Position centered horizontally, with 100px buffer from top
            const double TopBuffer = 100;
            window.Left = (screenBounds.Width - windowWidth) / 2 + screenBounds.Left;
            window.Top = screenBounds.Top + TopBuffer;

            // Ensure window fits on screen (height might exceed available space)
            EnsureWindowOnScreen(window, screenBounds, windowWidth, windowHeight);
        }
        catch
        {
            // Fallback: use WPF's built-in positioning
            window.Left = double.NaN;
            window.Top = double.NaN;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
