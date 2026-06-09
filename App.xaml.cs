using System;
using System.IO;
using System.Windows;

namespace ImageSquareResizer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            AppSettings settings = AppSettings.Load();

            if (e.Args.Length > 0)
            {
                ImageProcessor.ProcessFiles(
                    e.Args,
                    settings.Quality,
                    settings.ResizeMode,
                    settings.SmartMode,
                    settings.SharpMode,
                    settings.JpegMode,
                    settings.Language);

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
}