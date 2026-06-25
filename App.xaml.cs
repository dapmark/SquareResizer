using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace ImageSquareResizer;

public partial class App : Application
{
    private const string ManualStartupFlag = "--manual";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            AppSettings settings = AppSettings.Load();

            if (e.Args.Length > 0 && IsManualStartup(e.Args))
            {
                ShutdownMode = ShutdownMode.OnMainWindowClose;

                var manualWindow = new MainWindow(settings);
                MainWindow = manualWindow;
                manualWindow.Show();
                manualWindow.OpenManualStartupFiles(GetManualStartupPaths(e.Args));
                return;
            }

            if (e.Args.Length > 0)
            {
                ImageProcessor.ProcessFiles(
                    e.Args,
                    settings.Quality,
                    settings.ResizeMode,
                    settings.SmartMode,
                    settings.SharpMode,
                    settings.JpegMode,
                    settings.Language,
                    settings.SmartPaddingPercent,
                    settings.SmartPaddingMaxPx,
                    settings.AutoSizeStep);

                Shutdown();
                return;
            }

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var window = new MainWindow(settings);
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            string errorText = ex.ToString();

            try
            {
                string errorPath = Path.Combine(AppContext.BaseDirectory, "startup_error.txt");
                File.WriteAllText(errorPath, errorText);
            }
            catch
            {
                // If the log cannot be written, show the error below.
            }

            MessageBox.Show(
                errorText,
                Localization.For(AppSettings.DefaultLanguage).StartupErrorTitle(AppVersion.ProductName),
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    private static bool IsManualStartup(string[] args)
    {
        return args.Any(IsManualStartupFlag);
    }

    private static string[] GetManualStartupPaths(string[] args)
    {
        return args.Where(arg => !IsManualStartupFlag(arg)).ToArray();
    }

    private static bool IsManualStartupFlag(string arg)
    {
        return string.Equals(arg, ManualStartupFlag, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(arg, "/manual", StringComparison.OrdinalIgnoreCase);
    }

}
