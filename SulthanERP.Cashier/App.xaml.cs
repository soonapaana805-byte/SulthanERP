using System.Globalization;
using System.IO;
using System.Windows;

namespace SulthanERP.Cashier;

public partial class App : Application
{
    private bool _hasShownUnhandledError;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
                WriteErrorLog("AppDomain unhandled exception", exception);
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WriteErrorLog("Unobserved task exception", e.Exception);
            e.SetObserved();
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var culture = new CultureInfo("en-IN");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        WriteErrorLog("Dispatcher unhandled exception", e.Exception);
        e.Handled = true;

        // A repeated layout/binding failure should not trap the cashier behind many dialogs.
        if (_hasShownUnhandledError)
            return;

        _hasShownUnhandledError = true;
        MessageBox.Show(
            "An unexpected Cashier error was recorded. Please restart the Cashier window.\n\n" +
            $"Log file: {GetErrorLogPath()}",
            "Cashier error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void WriteErrorLog(string source, Exception exception)
    {
        try
        {
            var path = GetErrorLogPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(
                path,
                $"[{DateTime.Now:O}] {source}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never create another application exception.
        }
    }

    private static string GetErrorLogPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SulthanERP",
            "Cashier",
            "cashier-error.log");
    }
}
