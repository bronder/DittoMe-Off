using System.Windows;
using System.Windows.Threading;
using NLog;

namespace DittoMeOff.Services;

public static class GlobalExceptionHandler
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public static void Initialize(Application application)
    {
        // Handle UI thread exceptions
        application.DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        // Handle non-UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        
        // Handle task scheduler exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        
        _logger.Info("Global exception handlers initialized");
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.Fatal(e.Exception, "Unhandled UI thread exception");
        e.Handled = true;
        
        if (System.Diagnostics.Debugger.IsAttached)
        {
            MessageBox.Show(
                $"An unexpected error occurred: {e.Exception.Message}\n\nThe application will continue running.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        _logger.Fatal(exception, "Unhandled non-UI thread exception");
        
        if (e.IsTerminating)
        {
            _logger.Fatal("Application is terminating due to unhandled exception");
            LogManager.Shutdown();
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }
}