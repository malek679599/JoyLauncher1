using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using JoyLauncher.App.Diagnostics;

namespace JoyLauncher.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        _ = CrashLogWriter.Write("WPF Dispatcher unhandled exception", e.Exception);
        // Preserve WPF's normal crash behavior. The report is captured, but a possibly
        // corrupted UI is not silently allowed to continue.
    }

    private static void OnDomainUnhandledException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
            ?? new Exception($"Unhandled non-Exception object: {e.ExceptionObject ?? "null"}");

        _ = CrashLogWriter.Write(
            e.IsTerminating
                ? "AppDomain unhandled exception (process terminating)"
                : "AppDomain unhandled exception",
            exception);
    }

    private static void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        _ = CrashLogWriter.Write("Unobserved task exception", e.Exception);
    }
}
