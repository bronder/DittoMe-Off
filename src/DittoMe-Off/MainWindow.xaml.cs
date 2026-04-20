using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using DittoMeOff.Models;
using DittoMeOff.Services;
using DittoMeOff.ViewModels;

namespace DittoMeOff;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;
    private IHotkeyService? _hotkeyService;
    private IConfigService? _configService;
    private IWindowPositionService? _windowPositionService;
    private bool _isExiting;
    private IntPtr _previousForegroundWindow = IntPtr.Zero;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const int SW_RESTORE = 9;
    private const uint KEYEVENTF_KEYDOWN = 0x0000;

    // WM_NCHITTEST hit test values for borderless window resizing
    private const int WM_NCHITTEST = 0x0084;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;
    private const int HTCLIENT = 1;
    private const int ResizeBorderSize = 6;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void Initialize(MainViewModel viewModel, IHotkeyService hotkeyService, IConfigService configService, IThemeService themeService, IWindowPositionService windowPositionService)
    {
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
        _configService = configService;
        _windowPositionService = windowPositionService;
        DataContext = _viewModel;

        // Load the tray icon from the output directory
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "paste_icon.ico");
        if (System.IO.File.Exists(iconPath))
        {
            TrayIcon.Icon = new Icon(iconPath);
        }

        // Subscribe to keyboard navigation
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        ClipboardListView.SelectionChanged += ClipboardListView_SelectionChanged;

        // Restore window position
        var config = _configService.Config;
        if (config.WindowLeft >= 0 && config.WindowTop >= 0)
        {
            Left = config.WindowLeft;
            Top = config.WindowTop;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        // Validate window dimensions are within reasonable bounds
        const double MinWidth = 300, MaxWidth = 2000;
        const double MinHeight = 200, MaxHeight = 1500;
        Width = config.WindowWidth > 0 ? Math.Clamp(config.WindowWidth, MinWidth, MaxWidth) : 450;
        Height = config.WindowHeight > 0 ? Math.Clamp(config.WindowHeight, MinHeight, MaxHeight) : 600;

        // Apply saved splitter position after window size is set
        ApplySplitterPosition(config.SplitterPosition);

        // Initialize the history count input box
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (HistoryCountInput != null && _viewModel != null)
            {
                HistoryCountInput.Text = _viewModel.MaxHistoryCount.ToString();
            }
            if (PreviewLinesInput != null && _viewModel != null)
            {
                PreviewLinesInput.Text = _viewModel.PreviewLinesCount.ToString();
            }
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // If SearchBox doesn't have focus and the key is a printable character,
        // redirect it to the SearchBox to start filtering
        // But don't intercept if settings panel is open and user is typing in an input field
        var focusedElement = FocusManager.GetFocusedElement(this);
        bool isInSettingsPanel = IsInSettingsPanel(focusedElement as FrameworkElement);
        
        if (!SearchBox.IsFocused && !isInSettingsPanel)
        {
            // Check if the key is a printable character (A-Z, 0-9, space, punctuation, etc.)
            if (e.Key >= Key.A && e.Key <= Key.Z ||
                e.Key >= Key.D0 && e.Key <= Key.D9 ||
                e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9 ||
                e.Key == Key.Space ||
                e.Key == Key.OemPeriod ||
                e.Key == Key.OemComma ||
                e.Key == Key.OemQuestion ||
                e.Key == Key.OemQuotes ||
                e.Key == Key.OemMinus ||
                e.Key == Key.OemPlus ||
                e.Key == Key.OemOpenBrackets ||
                e.Key == Key.OemCloseBrackets)
            {
                SearchBox.Focus();
                // Let the event propagate to SearchBox
            }
        }

        HandleClipboardListNavigation(e);
    }

    private void ClipboardListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClipboardListView.SelectedItem != null)
        {
            ClipboardListView.ScrollIntoView(ClipboardListView.SelectedItem);
        }
    }

    /// <summary>
    /// Handles keyboard navigation in the clipboard list. Shared between PreviewKeyDown and ClipboardListView_KeyDown.
    /// </summary>
    private void HandleClipboardListNavigation(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (ClipboardListView.SelectedIndex < ClipboardListView.Items.Count - 1)
                {
                    ClipboardListView.SelectedIndex++;
                    ClipboardListView.ScrollIntoView(ClipboardListView.SelectedItem);
                }
                e.Handled = true;
                break;

            case Key.Up:
                if (ClipboardListView.SelectedIndex > 0)
                {
                    ClipboardListView.SelectedIndex--;
                    ClipboardListView.ScrollIntoView(ClipboardListView.SelectedItem);
                }
                e.Handled = true;
                break;

            case Key.Enter:
                if (ClipboardListView.SelectedItem is ClipboardItem item)
                {
                    // Move selected item to the top of the list
                    if (_viewModel?.ClipboardItems != null && _viewModel.ClipboardItems.Count > 1)
                    {
                        int currentIndex = _viewModel.ClipboardItems.IndexOf(item);
                        if (currentIndex > 0)
                        {
                            _viewModel.ClipboardItems.RemoveAt(currentIndex);
                            _viewModel.ClipboardItems.Insert(0, item);
                            ClipboardListView.SelectedIndex = 0;
                        }
                    }
                    
                    var previousWindow = _previousForegroundWindow;
                    _viewModel?.CopyItemCommand.Execute(item);
                    Hide();
                    
                    // If we have a previous window, paste to it asynchronously
                    PasteToWindowAsync(previousWindow);
                }
                e.Handled = true;
                break;

            case Key.Escape:
                Hide();
                e.Handled = true;
                break;

            case Key.Delete:
                // Delete all selected items
                var itemsToDelete = ClipboardListView.SelectedItems.Cast<ClipboardItem>().ToList();
                foreach (var deleteItem in itemsToDelete)
                {
                    _viewModel?.DeleteItemCommand.Execute(deleteItem);
                }
                e.Handled = true;
                break;

            case Key.P:
                // Toggle pin for all selected items (Ctrl+P)
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    var itemsToToggle = ClipboardListView.SelectedItems.Cast<ClipboardItem>().ToList();
                    foreach (var pinItem in itemsToToggle)
                    {
                        _viewModel?.TogglePinCommand.Execute(pinItem);
                    }
                    e.Handled = true;
                }
                break;
        }
    }

    /// <summary>
    /// Sends a single key press (down + up) using SendInput.
    /// </summary>
    private static void SendKeyPress(ushort vk)
    {
        INPUT[] inputs = new INPUT[2];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].ki = new KEYBDINPUT { wVk = vk };
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP };
        SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Sends a key combination (modifier + key) using SendInput.
    /// </summary>
    private static void SendKeyCombo(ushort modifierVk, ushort keyVk)
    {
        INPUT[] inputs = new INPUT[4];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].ki = new KEYBDINPUT { wVk = modifierVk };
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].ki = new KEYBDINPUT { wVk = keyVk };
        inputs[2].type = INPUT_KEYBOARD;
        inputs[2].ki = new KEYBDINPUT { wVk = keyVk, dwFlags = KEYEVENTF_KEYUP };
        inputs[3].type = INPUT_KEYBOARD;
        inputs[3].ki = new KEYBDINPUT { wVk = modifierVk, dwFlags = KEYEVENTF_KEYUP };
        SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Asynchronously pastes clipboard content to the target window using SendInput.
    /// Uses Task.Delay instead of Thread.Sleep to avoid blocking the UI thread.
    /// </summary>
    private async void PasteToWindowAsync(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine("PasteToWindowAsync: targetWindow is Zero, skipping paste.");
            return;
        }

        try
        {
            // Delay to ensure clipboard is updated and our window is fully hidden
            await Task.Delay(150);

            // Validate the window still exists before attempting paste
            if (!IsWindow(targetWindow))
            {
                System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync: target window {targetWindow} no longer exists.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync: attempting paste to window {targetWindow}");

            // Attach our thread input to the target window's thread so SetForegroundWindow works reliably
            uint foregroundThreadId = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            uint targetThreadId = GetWindowThreadProcessId(targetWindow, IntPtr.Zero);
            uint currentThreadId = GetCurrentThreadId();

            bool attached = false;
            if (foregroundThreadId != targetThreadId)
            {
                // Detach from current foreground thread if needed
                if (foregroundThreadId != currentThreadId)
                {
                    AttachThreadInput(currentThreadId, foregroundThreadId, false);
                }
                // Attach to target window's thread
                attached = AttachThreadInput(currentThreadId, targetThreadId, true);
                System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync: AttachThreadInput result = {attached}");
            }

            try
            {
                // Restore the target window if it was minimized
                ShowWindow(targetWindow, SW_RESTORE);

                // Bring to foreground
                bool setForeground = SetForegroundWindow(targetWindow);
                System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync: SetForegroundWindow result = {setForeground}");

                // Brief pause to let the target window process the focus change
                await Task.Delay(100);

                // Verify we successfully set the foreground window
                var currentForeground = GetForegroundWindow();
                System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync: current foreground = {currentForeground}, target = {targetWindow}");

                if (currentForeground == targetWindow)
                {
                    // Send Ctrl+V using keybd_event (more reliable for some apps than SendInput)
                    keybd_event((byte)VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    keybd_event((byte)VK_V, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    keybd_event((byte)VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    keybd_event((byte)VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    System.Diagnostics.Debug.WriteLine("PasteToWindowAsync: Ctrl+V sent successfully.");
                }
                else
                {
                    // Fallback: try SendInput approach
                    System.Diagnostics.Debug.WriteLine("PasteToWindowAsync: SetForegroundWindow didn't work, trying SendInput fallback.");
                    SendKeyCombo(VK_CONTROL, VK_V);
                }
            }
            finally
            {
                // Always detach thread input
                if (attached)
                {
                    AttachThreadInput(currentThreadId, targetThreadId, false);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PasteToWindowAsync failed: {ex.Message}");
        }
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilterCombo.SelectedItem is ComboBoxItem item && _viewModel != null)
        {
            _viewModel.SelectedTypeFilter = item.Tag?.ToString() ?? string.Empty;
        }
    }

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.SearchText = string.Empty;
        }
        SearchBox.Focus();
    }

    private void ClipboardListView_KeyDown(object sender, KeyEventArgs e)
    {
        HandleClipboardListNavigation(e);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _hotkeyService?.Initialize(this);
        var result = _hotkeyService?.RegisterHotkey(_configService?.Config.Hotkey ?? AppConstants.DefaultHotkey) ?? HotkeyRegistrationResult.InvalidHotkey;
        if (result != HotkeyRegistrationResult.Success)
        {
            System.Diagnostics.Debug.WriteLine($"Hotkey registration failed: {result}");
        }
        _hotkeyService!.HotkeyPressed += OnHotkeyPressed;

        // Add resize edge detection hook for borderless window
        var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        hwndSource?.AddHook(WndProcResizeHook);
    }

    /// <summary>
    /// Handles WM_NCHITTEST to enable edge resizing for the borderless window.
    /// When the mouse is near a window edge, returns the appropriate hit test value
    /// so Windows handles the resize cursor and drag behavior automatically.
    /// </summary>
    private IntPtr WndProcResizeHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_NCHITTEST)
            return IntPtr.Zero;

        // Get mouse position in screen coordinates from lParam
        int mouseX = lParam.ToInt32() & 0xFFFF;
        int mouseY = lParam.ToInt32() >> 16;

        // Get window position and size in screen coordinates
        var windowRect = new Rect(PointToScreen(new System.Windows.Point(0, 0)), new System.Windows.Size(ActualWidth, ActualHeight));

        // Check if mouse is within the resize border zone
        bool onLeft = mouseX >= windowRect.Left && mouseX <= windowRect.Left + ResizeBorderSize;
        bool onRight = mouseX >= windowRect.Right - ResizeBorderSize && mouseX <= windowRect.Right;
        bool onTop = mouseY >= windowRect.Top && mouseY <= windowRect.Top + ResizeBorderSize;
        bool onBottom = mouseY >= windowRect.Bottom - ResizeBorderSize && mouseY <= windowRect.Bottom;

        // Return appropriate hit test value for corners and edges
        if (onTop && onLeft) { handled = true; return (IntPtr)HTTOPLEFT; }
        if (onTop && onRight) { handled = true; return (IntPtr)HTTOPRIGHT; }
        if (onBottom && onLeft) { handled = true; return (IntPtr)HTBOTTOMLEFT; }
        if (onBottom && onRight) { handled = true; return (IntPtr)HTBOTTOMRIGHT; }
        if (onLeft) { handled = true; return (IntPtr)HTLEFT; }
        if (onRight) { handled = true; return (IntPtr)HTRIGHT; }
        if (onTop) { handled = true; return (IntPtr)HTTOP; }
        if (onBottom) { handled = true; return (IntPtr)HTBOTTOM; }

        return IntPtr.Zero;
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (Visibility == Visibility.Visible && WindowState != WindowState.Minimized)
            {
                Hide();
            }
            else
            {
                // Store the foreground window before we take focus
                _previousForegroundWindow = GetForegroundWindow();
                ShowWindow();
            }
        });
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            Hide();
            SaveWindowPosition();
        }
        else
        {
            _hotkeyService?.Dispose();
            _viewModel?.Cleanup();
            TrayIcon.Dispose();
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Save size when window is resized
        if (_configService != null && WindowState == WindowState.Normal)
        {
            _configService.UpdateConfig(config =>
            {
                config.WindowWidth = Width;
                config.WindowHeight = Height;
            });
        }
    }

    private void ShowWindow()
    {
        // Clear the search bar when bringing up the window
        if (_viewModel != null)
        {
            _viewModel.SearchText = string.Empty;
        }
        
        // Reset window state before showing
        WindowState = WindowState.Normal;
        
        // Force manual positioning - must be set before Show()
        WindowStartupLocation = WindowStartupLocation.Manual;

        // Apply configured size using WPF properties (DIPs, not physical pixels).
        var config = _configService?.Config;
        if (config != null)
        {
            if (config.WindowWidth > 0) Width = config.WindowWidth;
            if (config.WindowHeight > 0) Height = config.WindowHeight;
        }

        // Position the window at the top center of the screen with 100px buffer
        _windowPositionService?.PositionWindowAtTopCenter(this, _configService!);

        Show();
        Activate();
        
        // Select the top item when window is shown
        if (_viewModel?.ClipboardItems is { Count: > 0 })
        {
            ClipboardListView.SelectedIndex = 0;
            _viewModel.SelectedItem = _viewModel.ClipboardItems[0];
        }
        
        ClipboardListView.Focus();
    }

    /// <summary>
    /// Positions the window centered at the mouse cursor with smart fallback
    /// positioning when the window would extend beyond screen boundaries.
    /// Fallback hierarchy: centered → below → above → nearest edge
    /// </summary>
    private void PositionWindowAtCursor()
    {
        // Get cursor position using Win32 API (more reliable than WinForms)
        GetCursorPos(out var cursorPos);
        
        // Get the window dimensions (prefer config values if available)
        var config = _configService?.Config;
        double windowWidth = config != null && config.WindowWidth > 0 ? config.WindowWidth : Width;
        double windowHeight = config != null && config.WindowHeight > 0 ? config.WindowHeight : Height;
        
        // Get the working area of the screen containing the cursor
        var screenBounds = GetScreenWorkingArea(cursorPos.X, cursorPos.Y);
        
        // Calculate centered position (window center at cursor)
        double centeredLeft = cursorPos.X - (windowWidth / 2);
        double centeredTop = cursorPos.Y - (windowHeight / 2);
        
        // Priority 1: Try centered at cursor
        if (DoesWindowFitOnScreen(centeredLeft, centeredTop, windowWidth, windowHeight, screenBounds))
        {
            Left = centeredLeft;
            Top = centeredTop;
            EnsureWindowOnScreen(screenBounds, windowWidth, windowHeight);
            return;
        }
        
        // Priority 2: Try positioned below cursor (top-left at cursor)
        double belowLeft = cursorPos.X;
        double belowTop = cursorPos.Y;
        if (DoesWindowFitOnScreen(belowLeft, belowTop, windowWidth, windowHeight, screenBounds))
        {
            Left = belowLeft;
            Top = belowTop;
            EnsureWindowOnScreen(screenBounds, windowWidth, windowHeight);
            return;
        }
        
        // Priority 3: Try positioned above cursor
        double aboveLeft = cursorPos.X - windowWidth;
        double aboveTop = cursorPos.Y - windowHeight;
        if (DoesWindowFitOnScreen(aboveLeft, aboveTop, windowWidth, windowHeight, screenBounds))
        {
            Left = aboveLeft;
            Top = aboveTop;
            EnsureWindowOnScreen(screenBounds, windowWidth, windowHeight);
            return;
        }
        
        // Priority 4: Try centered horizontally, below cursor vertically
        double hCenterBelowLeft = cursorPos.X - (windowWidth / 2);
        double hCenterBelowTop = cursorPos.Y;
        if (DoesWindowFitOnScreen(hCenterBelowLeft, hCenterBelowTop, windowWidth, windowHeight, screenBounds))
        {
            Left = hCenterBelowLeft;
            Top = hCenterBelowTop;
            EnsureWindowOnScreen(screenBounds, windowWidth, windowHeight);
            return;
        }
        
        // Priority 5: Try centered horizontally, above cursor vertically
        double hCenterAboveLeft = cursorPos.X - (windowWidth / 2);
        double hCenterAboveTop = cursorPos.Y - windowHeight;
        if (DoesWindowFitOnScreen(hCenterAboveLeft, hCenterAboveTop, windowWidth, windowHeight, screenBounds))
        {
            Left = hCenterAboveLeft;
            Top = hCenterAboveTop;
            EnsureWindowOnScreen(screenBounds, windowWidth, windowHeight);
            return;
        }
        
        // Fallback: Position at cursor and let EnsureWindowOnScreen clamp to nearest edge
        Left = centeredLeft;
        Top = centeredTop;
        EnsureWindowOnScreen(screenBounds, windowWidth, windowHeight);
    }

    /// <summary>
    /// Tests whether a window at the given position fits entirely within screen bounds.
    /// </summary>
    private static bool DoesWindowFitOnScreen(double left, double top, double width, double height, Rect screenBounds)
    {
        return left >= screenBounds.Left
            && top >= screenBounds.Top
            && left + width <= screenBounds.Right
            && top + height <= screenBounds.Bottom;
    }

    /// <summary>
    /// Ensures the window is fully visible on screen by clamping position to screen bounds.
    /// This is the final safety net - it adjusts the window position to guarantee full visibility.
    /// </summary>
    private void EnsureWindowOnScreen(Rect screenBounds, double windowWidth, double windowHeight)
    {
        // Clamp Left to keep window within horizontal bounds
        if (Left + windowWidth > screenBounds.Right)
        {
            Left = screenBounds.Right - windowWidth;
        }
        if (Left < screenBounds.Left)
        {
            Left = screenBounds.Left;
        }
        
        // Clamp Top to keep window within vertical bounds
        if (Top + windowHeight > screenBounds.Bottom)
        {
            Top = screenBounds.Bottom - windowHeight;
        }
        if (Top < screenBounds.Top)
        {
            Top = screenBounds.Top;
        }
    }

    /// <summary>
    /// Centers the window on the screen containing the cursor.
    /// Used as a fallback for tray icon clicks and similar scenarios.
    /// </summary>
    private void PositionWindowCenterScreen()
    {
        try
        {
            // Get cursor position to determine which screen to use
            GetCursorPos(out var cursorPos);
            var screenBounds = GetScreenWorkingArea(cursorPos.X, cursorPos.Y);
            var config = _configService?.Config;
            double windowWidth = config != null && config.WindowWidth > 0 ? config.WindowWidth : Width;
            double windowHeight = config != null && config.WindowHeight > 0 ? config.WindowHeight : Height;
            Left = (screenBounds.Width - windowWidth) / 2 + screenBounds.Left;
            Top = (screenBounds.Height - windowHeight) / 2 + screenBounds.Top;
        }
        catch
        {
            // Fallback: use WPF's built-in centering (set Left/Top to NaN)
            Left = double.NaN;
            Top = double.NaN;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private Rect GetScreenWorkingArea(int x, int y)
    {
        // Find which screen contains the point
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
        
        // Return the working area of the screen (excludes taskbar)
        return new Rect(
            targetScreen!.WorkingArea.Left,
            targetScreen.WorkingArea.Top,
            targetScreen.WorkingArea.Width,
            targetScreen.WorkingArea.Height);
    }

    private void ShowWindow_Click(object sender, RoutedEventArgs e)
    {
        ShowWindow();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _isExiting = true;
        SaveWindowPosition();
        Application.Current.Shutdown();
    }

    private void ClipboardItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is FrameworkElement element && element.Tag is ClipboardItem item)
        {
            _viewModel?.CopyItemCommand.Execute(item);
            Hide();
        }
    }

    private void ContextMenuCopy_Click(object sender, RoutedEventArgs e)
    {
        // Use SelectedItem from the ListView for the context menu
        if (_viewModel?.SelectedItem is ClipboardItem item)
        {
            _viewModel.CopyItemCommand.Execute(item);
        }
    }

    private void ContextMenuPin_Click(object sender, RoutedEventArgs e)
    {
        // Use SelectedItem from the ListView for the context menu
        if (_viewModel?.SelectedItem is ClipboardItem item)
        {
            _viewModel.TogglePinCommand.Execute(item);
        }
    }

    private void ContextMenuDelete_Click(object sender, RoutedEventArgs e)
    {
        // Delete all selected items
        var itemsToDelete = ClipboardListView.SelectedItems.Cast<ClipboardItem>().ToList();
        foreach (var item in itemsToDelete)
        {
            _viewModel?.DeleteItemCommand.Execute(item);
        }
    }

    private void ClipboardItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is ClipboardItem item)
        {
            var contextMenu = new ContextMenu();
            
            var copyItem = new MenuItem { Header = AppConstants.ContextMenu.Copy };
            copyItem.Click += (s, args) => _viewModel?.CopyItemCommand.Execute(item);
            
            var pinItem = new MenuItem 
            { 
                Header = item.IsPinned ? AppConstants.ContextMenu.Unpin : AppConstants.ContextMenu.Pin 
            };
            pinItem.Click += (s, args) => _viewModel?.TogglePinCommand.Execute(item);
            
            var deleteItem = new MenuItem { Header = AppConstants.ContextMenu.Delete };
            deleteItem.Click += (s, args) => _viewModel?.DeleteItemCommand.Execute(item);
            
            contextMenu.Items.Add(copyItem);
            contextMenu.Items.Add(pinItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(deleteItem);
            
            contextMenu.IsOpen = true;
        }
    }

    private void SaveWindowPosition()
    {
        if (_configService != null && WindowState == WindowState.Normal)
        {
            _configService.UpdateConfig(config =>
            {
                // Don't save window position - always show at cursor
                config.WindowWidth = Width;
                config.WindowHeight = Height;
            });
        }
    }

    private void MainSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
        SaveSplitterPosition();
    }

    private void SaveSplitterPosition()
    {
        if (_configService != null && ListColumn.ActualWidth > 0)
        {
            _configService.UpdateConfig(config =>
            {
                config.SplitterPosition = ListColumn.Width.Value;
            });
        }
    }

    private void ApplySplitterPosition(double position)
    {
        if (position <= 0) return;
        if (MainContent.ActualWidth <= 0)
        {
            // If content width not yet available, defer to loaded
            Dispatcher.BeginInvoke(new Action(() => ApplySplitterPosition(position)), System.Windows.Threading.DispatcherPriority.Loaded);
            return;
        }

        ListColumn.Width = new GridLength(position, GridUnitType.Pixel);
    }

    private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        HotkeyHint.Text = AppConstants.HotkeyHints.Recording;
    }

    private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        HotkeyHint.Text = AppConstants.HotkeyHints.Idle;
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true; // Prevent the key from being typed into the TextBox
        
        // Build the hotkey string from the pressed keys
        var parts = new List<string>();
        
        // Check for modifiers
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
            parts.Add("Win");
        
        // Get the main key (exclude modifier keys themselves)
        var key = e.Key;
        if (key == Key.LeftCtrl || key == Key.RightCtrl || 
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            // Just a modifier key was pressed - wait for the main key
            return;
        }
        
        // Handle special keys
        if (key == Key.System)
            key = e.SystemKey;
        
        // Convert key to display string
        string keyStr = key.ToString();
        
        // Single letter keys should be just the letter
        if (keyStr.Length == 1)
        {
            keyStr = keyStr.ToUpper();
        }
        else if (keyStr.StartsWith("NumPad"))
        {
            keyStr = keyStr.Substring(6);
        }
        else
        {
            // Map special key names to friendly names
            switch (keyStr)
            {
                case "Space": keyStr = "Space"; break;
                case "Return": keyStr = "Enter"; break;
                case "Escape": keyStr = "Esc"; break;
                case "Back": keyStr = "Backspace"; break;
                case "Tab": keyStr = "Tab"; break;
                case "Delete": keyStr = "Delete"; break;
                case "Home": keyStr = "Home"; break;
                case "End": keyStr = "End"; break;
                case "PageUp": keyStr = "PageUp"; break;
                case "PageDown": keyStr = "PageDown"; break;
                case "Left": keyStr = "Left"; break;
                case "Up": keyStr = "Up"; break;
                case "Right": keyStr = "Right"; break;
                case "Down": keyStr = "Down"; break;
                case "Insert": keyStr = "Insert"; break;
                case "F1": case "F2": case "F3": case "F4":
                case "F5": case "F6": case "F7": case "F8":
                case "F9": case "F10": case "F11": case "F12":
                    // Keep F-keys as-is
                    break;
                default:
                    // Keep the key name as-is for other special keys
                    break;
            }
        }
        
        parts.Add(keyStr);
        
        // Update the hotkey in the ViewModel
        var hotkeyString = string.Join("+", parts);
        if (_viewModel != null)
        {
            _viewModel.CurrentHotkey = hotkeyString;
        }
        
        HotkeyHint.Text = AppConstants.HotkeyHints.RecordingFormat(hotkeyString);
    }

    private void ClearHistoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClearHistoryCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            switch (tag)
            {
                case AppConstants.ClearHistoryOptions.All:
                    _viewModel?.ClearHistoryCommand.Execute(null);
                    break;
                case AppConstants.ClearHistoryOptions.Day:
                    _viewModel?.ClearOlderThanDayCommand.Execute(null);
                    break;
                case AppConstants.ClearHistoryOptions.Week:
                    _viewModel?.ClearOlderThanWeekCommand.Execute(null);
                    break;
                case AppConstants.ClearHistoryOptions.Month:
                    _viewModel?.ClearOlderThanMonthCommand.Execute(null);
                    break;
            }
            
            // Reset the dropdown to "Choose an option..."
            ClearHistoryCombo.SelectedIndex = 0;
        }
    }

    // Custom title bar event handlers
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double-click to maximize/restore
            MaximizeButton_Click(sender, e);
        }
        else
        {
            // Drag the window
            if (WindowState == WindowState.Maximized)
            {
                // If maximized, restore first then allow dragging from new position
                var point = e.GetPosition(this);
                WindowState = WindowState.Normal;
                Left = point.X;
                Top = point.Y;
            }
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            if (MaximizeButton.Content is System.Windows.Controls.TextBlock tb)
                tb.Text = "□";
        }
        else
        {
            WindowState = WindowState.Maximized;
            if (MaximizeButton.Content is System.Windows.Controls.TextBlock tb)
                tb.Text = "❐";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        SaveWindowPosition();
    }

    private void PresetHistoryCount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string tagValue)
        {
            if (int.TryParse(tagValue, out int value))
            {
                if (_viewModel != null)
                {
                    _viewModel.MaxHistoryCount = value;
                    // Update the input box text
                    if (HistoryCountInput != null)
                    {
                        HistoryCountInput.Text = value.ToString();
                    }
                }
            }
        }
    }

    private void PresetPreviewLines_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Primitives.ToggleButton toggleButton && toggleButton.Tag is string tagValue)
        {
            if (int.TryParse(tagValue, out int value))
            {
                if (_viewModel != null)
                {
                    _viewModel.PreviewLinesCount = value;
                    // Update the input box text
                    if (PreviewLinesInput != null)
                    {
                        PreviewLinesInput.Text = value.ToString();
                    }
                }
            }
        }
    }

    private void HistoryCountInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow digits
        e.Handled = !IsTextAllowed(e.Text);
    }

    private void HistoryCountInput_LostFocus(object sender, RoutedEventArgs e)
    {
        ValidateAndClampHistoryCount();
    }

    private void HistoryCountInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ValidateAndClampHistoryCount();
            // Move focus away from the input
            Keyboard.ClearFocus();
        }
    }

    private void HistoryCountInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Allow manual text editing without immediate binding update
        // Value will be validated and applied on LostFocus or Enter key
    }

    private void PreviewLinesInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsTextAllowed(e.Text);
    }

    private void PreviewLinesInput_LostFocus(object sender, RoutedEventArgs e)
    {
        ValidateAndClampPreviewLinesCount();
    }

    private void PreviewLinesInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ValidateAndClampPreviewLinesCount();
            // Move focus away from the input
            Keyboard.ClearFocus();
        }
    }

    private void PreviewLinesInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Allow manual text editing without immediate binding update
        // Value will be validated and applied on LostFocus or Enter key
    }

    private void ValidateAndClampPreviewLinesCount()
    {
        if (PreviewLinesInput == null || _viewModel == null) return;

        if (int.TryParse(PreviewLinesInput.Text, out int value))
        {
            // Clamp to valid range (minimum 1)
            if (value < 1) value = 1;
            _viewModel.PreviewLinesCount = value;
            PreviewLinesInput.Text = value.ToString();
        }
        else
        {
            // Invalid input - reset to current value
            PreviewLinesInput.Text = _viewModel.PreviewLinesCount.ToString();
        }
    }

    private bool IsTextAllowed(string text)
    {
        foreach (char c in text)
        {
            if (!char.IsDigit(c))
                return false;
        }
        return true;
    }

    private void ValidateAndClampHistoryCount()
    {
        if (HistoryCountInput == null || _viewModel == null) return;

        if (int.TryParse(HistoryCountInput.Text, out int value))
        {
            // Clamp to valid range (minimum 1)
            if (value < 1) value = 1;
            _viewModel.MaxHistoryCount = value;
            HistoryCountInput.Text = value.ToString();
        }
        else
        {
            // Invalid input - reset to current value
            HistoryCountInput.Text = _viewModel.MaxHistoryCount.ToString();
        }
    }

    private bool IsInSettingsPanel(FrameworkElement? element)
    {
        if (element == null) return false;
        
        // Check if the element or any of its ancestors is within the SettingsPanel
        var current = element;
        while (current != null)
        {
            if (current == SettingsPanel)
                return true;
            current = current.Parent as FrameworkElement;
        }
        return false;
    }

    private void RefreshHistoryCountInput()
    {
        // Update the history count input box with current value
        if (HistoryCountInput != null && _viewModel != null)
        {
            HistoryCountInput.Text = _viewModel.MaxHistoryCount.ToString();
        }
    }
}
