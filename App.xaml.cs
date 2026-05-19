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
                ImageProcessor.ProcessFiles(e.Args, settings.Quality);
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
                // Если даже лог записать нельзя, просто покажем ошибку ниже.
            }

            MessageBox.Show(
                errorText,
                "Ошибка запуска DapLine SquareResizer",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }
}
