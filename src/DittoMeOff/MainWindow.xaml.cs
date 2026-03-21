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
    private HotkeyService? _hotkeyService;
    private ConfigService? _configService;
    private bool _isExiting;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOP = IntPtr.Zero;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void Initialize(MainViewModel viewModel, HotkeyService hotkeyService, ConfigService configService, ThemeService themeService)
    {
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
        _configService = configService;
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

        Width = config.WindowWidth > 0 ? config.WindowWidth : 450;
        Height = config.WindowHeight > 0 ? config.WindowHeight : 600;

        // Apply saved splitter position after window size is set
        ApplySplitterPosition(config.SplitterPosition);
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // If SearchBox doesn't have focus and the key is a printable character,
        // redirect it to the SearchBox to start filtering
        if (!SearchBox.IsFocused)
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
                    _viewModel?.CopyItemCommand.Execute(item);
                    Hide();
                }
                e.Handled = true;
                break;

            case Key.Escape:
                Hide();
                e.Handled = true;
                break;

            case Key.Delete:
                if (ClipboardListView.SelectedItem is ClipboardItem deleteItem)
                {
                    _viewModel?.DeleteItemCommand.Execute(deleteItem);
                }
                e.Handled = true;
                break;
        }
    }

    private void ClipboardListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClipboardListView.SelectedItem != null)
        {
            ClipboardListView.ScrollIntoView(ClipboardListView.SelectedItem);
        }
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilterCombo.SelectedItem is ComboBoxItem item && _viewModel != null)
        {
            _viewModel.SelectedTypeFilter = item.Tag?.ToString() ?? string.Empty;
        }
    }

    private void ClipboardListView_KeyDown(object sender, KeyEventArgs e)
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
                    _viewModel?.CopyItemCommand.Execute(item);
                    Hide();
                }
                e.Handled = true;
                break;

            case Key.Escape:
                Hide();
                e.Handled = true;
                break;

            case Key.Delete:
                if (ClipboardListView.SelectedItem is ClipboardItem deleteItem)
                {
                    _viewModel?.DeleteItemCommand.Execute(deleteItem);
                }
                e.Handled = true;
                break;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _hotkeyService?.Initialize(this);
        _hotkeyService?.RegisterHotkey(_configService?.Config.Hotkey ?? "Ctrl+Shift+V");
        _hotkeyService!.HotkeyPressed += OnHotkeyPressed;
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
        // Reset window state before showing
        WindowState = WindowState.Normal;
        
        // Force manual positioning - must be set before Show()
        WindowStartupLocation = WindowStartupLocation.Manual;
        
        // Get cursor position
        var cursorPos = System.Windows.Forms.Cursor.Position;
        
        // Position window before showing
        Left = cursorPos.X;
        Top = cursorPos.Y;
        
        // Adjust if window would be off-screen
        EnsureWindowOnScreen();
        
        Show();
        
        // Use SetWindowPos to force the position AND size after show
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                // Get current config values to ensure we're using the right size
                var config = _configService?.Config;
                int width = config != null && config.WindowWidth > 0 ? (int)config.WindowWidth : (int)Width;
                int height = config != null && config.WindowHeight > 0 ? (int)config.WindowHeight : (int)Height;
                
                SetWindowPos(hwnd, HWND_TOP, (int)Left, (int)Top, width, height, SWP_NOZORDER | SWP_SHOWWINDOW);
                
                // Also update WPF properties to match
                Width = width;
                Height = height;
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
        
        Activate();
        ClipboardListView.Focus();
    }
    
    private void EnsureWindowOnScreen()
    {
        // Get the screen containing the window's top-left corner
        var windowRect = new Rect(Left, Top, Width, Height);
        
        // Get working area of the screen containing the window
        var screenBounds = GetScreenWorkingArea((int)Left, (int)Top);
        
        // Adjust Left if window extends beyond right edge
        if (Left + Width > screenBounds.Right)
        {
            Left = screenBounds.Right - Width;
        }
        
        // Adjust Top if window extends beyond bottom edge
        if (Top + Height > screenBounds.Bottom)
        {
            Top = screenBounds.Bottom - Height;
        }
        
        // Adjust Left if window extends beyond left edge
        if (Left < screenBounds.Left)
        {
            Left = screenBounds.Left;
        }
        
        // Adjust Top if window extends beyond top edge
        if (Top < screenBounds.Top)
        {
            Top = screenBounds.Top;
        }
    }

    private void PositionWindowCenterScreen()
    {
        try
        {
            // Get cursor position to determine which screen to use
            var cursorPos = System.Windows.Forms.Cursor.Position;
            var screenBounds = GetScreenWorkingArea(cursorPos.X, cursorPos.Y);
            Left = (screenBounds.Width - Width) / 2 + screenBounds.Left;
            Top = (screenBounds.Height - Height) / 2 + screenBounds.Top;
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
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is FrameworkElement element && element.Tag is ClipboardItem item)
        {
            _viewModel?.CopyItemCommand.Execute(item);
        }
    }

    private void ContextMenuPin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is FrameworkElement element && element.Tag is ClipboardItem item)
        {
            _viewModel?.TogglePinCommand.Execute(item);
        }
    }

    private void ContextMenuDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is FrameworkElement element && element.Tag is ClipboardItem item)
        {
            _viewModel?.DeleteItemCommand.Execute(item);
        }
    }

    private void ClipboardItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is ClipboardItem item)
        {
            var contextMenu = new ContextMenu();
            
            var copyItem = new MenuItem { Header = "Copy" };
            copyItem.Click += (s, args) => _viewModel?.CopyItemCommand.Execute(item);
            
            var pinItem = new MenuItem 
            { 
                Header = item.IsPinned ? "Unpin" : "Pin" 
            };
            pinItem.Click += (s, args) => _viewModel?.TogglePinCommand.Execute(item);
            
            var deleteItem = new MenuItem { Header = "Delete" };
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
        HotkeyHint.Text = "Press your hotkey combination...";
    }

    private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        HotkeyHint.Text = "Click here, then press your hotkey combination...";
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
        
        HotkeyHint.Text = $"Recording: {hotkeyString}";
    }

    private void ClearHistoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClearHistoryCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            switch (tag)
            {
                case "all":
                    _viewModel?.ClearHistoryCommand.Execute(null);
                    break;
                case "day":
                    _viewModel?.ClearOlderThanDayCommand.Execute(null);
                    break;
                case "week":
                    _viewModel?.ClearOlderThanWeekCommand.Execute(null);
                    break;
                case "month":
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
}
