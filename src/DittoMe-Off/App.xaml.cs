using System.Windows;
using DittoMeOff.Services;
using DittoMeOff.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DittoMeOff;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Initialize services
        var configService = _serviceProvider.GetRequiredService<IConfigService>();
        var themeService = _serviceProvider.GetRequiredService<IThemeService>();
        themeService.LoadSavedTheme();
        
        var databaseService = _serviceProvider.GetRequiredService<IDatabaseService>();
        databaseService.Initialize();
        
        var clipboardMonitor = _serviceProvider.GetRequiredService<IClipboardMonitorService>();
        var hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
        
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();

        // Create and show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Initialize(mainViewModel, hotkeyService, configService, themeService);
        MainWindow = mainWindow;
        mainWindow.Show();

        // Start clipboard monitoring
        clipboardMonitor.Start();

        // Handle auto-start registration
        UpdateAutoStart(configService.Config.AutoStart);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IClipboardMonitorService, ClipboardMonitorService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();
        
        // Register ViewModels
        services.AddSingleton<MainViewModel>();
        
        // Register Windows
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dispose all disposable services
        if (_serviceProvider != null)
        {
            var disposableServices = new[]
            {
                typeof(IClipboardMonitorService),
                typeof(IHotkeyService),
                typeof(IDatabaseService)
            };

            foreach (var serviceType in disposableServices)
            {
                var service = _serviceProvider.GetService(serviceType) as IDisposable;
                service?.Dispose();
            }
            
            _serviceProvider.Dispose();
        }
        
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
                        key.SetValue("DittoMe-Off", $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue("DittoMe-Off", false);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating auto-start: {ex.Message}");
        }
    }
}
