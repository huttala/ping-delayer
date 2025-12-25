using System;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PingDelayer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Handle all unhandled exceptions to prevent application crash
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            HandleException(ex, "AppDomain.UnhandledException");
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException(e.Exception, "Dispatcher.UnhandledException");
        e.Handled = true; // Prevent application shutdown
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved(); // Prevent application shutdown
    }

    private void HandleException(Exception ex, string source)
    {
        // Suppress AccessViolationException from overlapped I/O during shutdown
        // This is a known race condition when closing WinDivert handles
        if (ex is AccessViolationException)
        {
            System.Diagnostics.Debug.WriteLine($"[{source}] Suppressed AccessViolationException during WinDivert cleanup: {ex.Message}");
            return;
        }

        // Log other exceptions but don't crash the application
        System.Diagnostics.Debug.WriteLine($"[{source}] Unhandled exception: {ex}");
        
        // Show error to user for non-AV exceptions
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{ex.Message}\n\nThe application will continue running.",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}

