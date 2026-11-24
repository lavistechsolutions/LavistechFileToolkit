using System.Windows;
using System.Windows.Threading;

namespace LavistechFileToolkit;

public partial class App : Application
{
    bool showingError;

    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex) ErrorLog.Write("Unhandled background exception", ex);
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ErrorLog.Write("Unobserved task exception", args.Exception);
            args.SetObserved();
        };
        DispatcherUnhandledException += OnUnhandledException;
        base.OnStartup(e);
    }

    void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        ErrorLog.Write("Unhandled UI exception", e.Exception);
        if (showingError) return;

        showingError = true;
        try
        {
            MessageBox.Show($"{e.Exception.Message}\n\nDetails were saved to:\n{ErrorLog.FilePath}",
                "Lavistech File Toolkit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            showingError = false;
        }
    }
}
