using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace ImageSquareResizer;

public partial class MainWindow : Window
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private readonly AppSettings currentSettings;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    public MainWindow()
        : this(AppSettings.Load())
    {
    }

    internal MainWindow(AppSettings settings)
    {
        currentSettings = settings;

        InitializeComponent();

        QualityTextBox.Text = currentSettings.Quality.ToString();
        DataObject.AddPastingHandler(QualityTextBox, OnQualityPaste);

        ApplyTheme();
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        ApplyTitleBarTheme();
    }

    private void ApplyTheme()
    {
        if (currentSettings.IsDarkTheme)
        {
            ApplyDarkTheme();
        }
        else
        {
            ApplyLightTheme();
        }

        if (IsInitialized)
        {
            ApplyTitleBarTheme();
        }
    }

    private void ApplyLightTheme()
    {
        Resources["WindowBackgroundBrush"] = BrushFromRgb(240, 240, 240);
        Resources["DropAreaBackgroundBrush"] = BrushFromRgb(245, 245, 245);
        Resources["DropAreaBorderBrush"] = BrushFromRgb(130, 130, 130);
        Resources["MainTextBrush"] = BrushFromRgb(70, 70, 70);
        Resources["SecondaryTextBrush"] = BrushFromRgb(50, 50, 50);
        Resources["ButtonBackgroundBrush"] = BrushFromRgb(250, 250, 250);
        Resources["ButtonHoverBackgroundBrush"] = BrushFromRgb(245, 249, 255);
        Resources["ButtonPressedBackgroundBrush"] = BrushFromRgb(230, 242, 255);
        Resources["ButtonBorderBrush"] = BrushFromRgb(170, 170, 170);
        Resources["AccentBorderBrush"] = BrushFromRgb(0, 120, 215);
        Resources["InputBackgroundBrush"] = BrushFromRgb(255, 255, 255);
        Resources["StatusTextBrush"] = BrushFromRgb(80, 80, 80);
    }

    private void ApplyDarkTheme()
    {
        Resources["WindowBackgroundBrush"] = BrushFromRgb(30, 30, 30);
        Resources["DropAreaBackgroundBrush"] = BrushFromRgb(37, 37, 38);
        Resources["DropAreaBorderBrush"] = BrushFromRgb(63, 63, 70);
        Resources["MainTextBrush"] = BrushFromRgb(212, 212, 212);
        Resources["SecondaryTextBrush"] = BrushFromRgb(212, 212, 212);
        Resources["ButtonBackgroundBrush"] = BrushFromRgb(45, 45, 48);
        Resources["ButtonHoverBackgroundBrush"] = BrushFromRgb(62, 62, 66);
        Resources["ButtonPressedBackgroundBrush"] = BrushFromRgb(0, 122, 204);
        Resources["ButtonBorderBrush"] = BrushFromRgb(63, 63, 70);
        Resources["AccentBorderBrush"] = BrushFromRgb(0, 122, 204);
        Resources["InputBackgroundBrush"] = BrushFromRgb(30, 30, 30);
        Resources["StatusTextBrush"] = BrushFromRgb(200, 200, 200);
    }

    private static SolidColorBrush BrushFromRgb(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }

    private void ApplyTitleBarTheme()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        int useDarkMode = currentSettings.IsDarkTheme ? 1 : 0;

        var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;

        _ = DwmSetWindowAttribute(
            handle,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref useDarkMode,
            sizeof(int));
    }

    private void OnOpenButtonClick(object sender, RoutedEventArgs e)
    {
        if (!SaveQualityFromUi(showMessageOnError: true))
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Выберите изображение",
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.tif;*.tiff|Все файлы|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        ProcessSelectedFiles(dialog.FileNames);
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
        {
            return;
        }

        if (!SaveQualityFromUi(showMessageOnError: true))
        {
            return;
        }

        ProcessSelectedFiles(files);
    }

    private void OnQualityLostFocus(object sender, RoutedEventArgs e)
    {
        SaveQualityFromUi(showMessageOnError: false);
    }

    private void OnQualityPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        SaveQualityFromUi(showMessageOnError: true);
        QualityTextBox.CaretIndex = QualityTextBox.Text.Length;
    }

    private void OnQualityPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private void OnQualityPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        string? pastedText = e.DataObject.GetData(DataFormats.Text) as string;

        if (string.IsNullOrEmpty(pastedText) || !pastedText.All(char.IsDigit))
        {
            e.CancelCommand();
        }
    }

    private void OnQualityPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        string rawText = QualityTextBox.Text.Trim();

        if (!int.TryParse(rawText, out int quality))
        {
            quality = currentSettings.Quality;
        }

        if (e.Delta > 0)
        {
            quality++;
        }
        else if (e.Delta < 0)
        {
            quality--;
        }

        quality = AppSettings.NormalizeQuality(quality);

        currentSettings.Quality = quality;
        currentSettings.Save();

        QualityTextBox.Text = quality.ToString();
        QualityTextBox.CaretIndex = QualityTextBox.Text.Length;

        e.Handled = true;
    }

    private bool SaveQualityFromUi(bool showMessageOnError)
    {
        string rawText = QualityTextBox.Text.Trim();

        if (!int.TryParse(rawText, out int quality))
        {
            if (showMessageOnError)
            {
                MessageBox.Show(
                    this,
                    "Введите число качества от 1 до 100.",
                    "Некорректное значение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                QualityTextBox.Focus();
                QualityTextBox.SelectAll();
            }

            QualityTextBox.Text = currentSettings.Quality.ToString();
            return false;
        }

        quality = AppSettings.NormalizeQuality(quality);

        currentSettings.Quality = quality;
        currentSettings.Save();

        QualityTextBox.Text = currentSettings.Quality.ToString();
        return true;
    }

    private void ProcessSelectedFiles(string[] files)
    {
        StatusTextBlock.Text = "Обработка...";

        var results = ImageProcessor.ProcessFiles(files, currentSettings.Quality);

        foreach (ProcessResult result in results.Where(r => !r.Success && !r.AlreadyCorrectSize))
        {
            MessageBox.Show(
                this,
                result.ErrorMessage ?? "Неизвестная ошибка.",
                "Ошибка обработки",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        int created = results.Count(r => r.Success && !r.AlreadyCorrectSize);
        int alreadyCorrect = results.Count(r => r.AlreadyCorrectSize);
        int failed = results.Count(r => !r.Success);

        if (created == 1 && failed == 0)
        {
            string? output = results.FirstOrDefault(r => r.OutputPath != null)?.OutputPath;
            StatusTextBlock.Text = output == null
                ? "Готово."
                : $"Готово: {System.IO.Path.GetFileName(output)}";
            return;
        }

        if (created > 1 && failed == 0)
        {
            StatusTextBlock.Text = $"Готово. Создано файлов: {created}";
            return;
        }

        if (alreadyCorrect > 0 && created == 0 && failed == 0)
        {
            StatusTextBlock.Text = "Файл уже нужного размера.";
            return;
        }

        StatusTextBlock.Text = $"Создано: {created}, уже готово: {alreadyCorrect}, ошибок: {failed}";
    }
}
