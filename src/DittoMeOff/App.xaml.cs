using System.Windows;
using DittoMeOff.Services;
using DittoMeOff.ViewModels;

namespace DittoMeOff;

public partial class App : Application
{
    private ConfigService? _configService;
    private DatabaseService? _databaseService;
    private ClipboardMonitorService? _clipboardMonitor;
    private HotkeyService? _hotkeyService;
    private ThemeService? _themeService;
    private MainViewModel? _mainViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        _configService = new ConfigService();
        _themeService = new ThemeService(_configService);
        _themeService.LoadSavedTheme();
        
        _databaseService = new DatabaseService();
        _databaseService.Initialize();
        
        _clipboardMonitor = new ClipboardMonitorService(_configService, _databaseService);
        _hotkeyService = new HotkeyService();
        
        _mainViewModel = new MainViewModel(_databaseService, _configService, _clipboardMonitor, _themeService, _hotkeyService);

        // Create and show main window
        var mainWindow = new MainWindow();
        mainWindow.Initialize(_mainViewModel, _hotkeyService, _configService, _themeService);
        MainWindow = mainWindow;
        mainWindow.Show();

        // Start clipboard monitoring
        _clipboardMonitor.Start();

        // Handle auto-start registration
        UpdateAutoStart(_configService.Config.AutoStart);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _clipboardMonitor?.Dispose();
        _hotkeyService?.Dispose();
        _databaseService?.Dispose();
        
        base.OnExit(e);
    }

    private void UpdateAutoStart(bool enable)
    {
        try
        {
            var keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath, true);
            
            if (key != null)
            {
                if (enable)
                {
                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("DittoMeOff", $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue("DittoMeOff", false);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating auto-start: {ex.Message}");
        }
    }
}
