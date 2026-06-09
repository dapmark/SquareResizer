using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ImageSquareResizer;

public partial class MainWindow : Window
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private readonly AppSettings currentSettings;
    private readonly Localization text;

    private bool isManualPreviewLoaded;
    private bool isDraggingCrop;

    private string? manualSourcePath;
    private int manualImageWidth;
    private int manualImageHeight;
    private int manualCropSize;
    private int manualCropX;
    private int manualCropY;

    private double manualPreviewLeft;
    private double manualPreviewTop;
    private double manualPreviewScale = 1.0;

    private Point dragStartPoint;
    private int dragStartCropX;
    private int dragStartCropY;

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
        text = Localization.For(settings.Language);

        InitializeComponent();

        Title = AppVersion.WindowTitle;
        ApplyLocalizedText();

        QualityTextBox.Text = currentSettings.Quality.ToString();
        SmartModeCheckBox.IsChecked = currentSettings.SmartMode;
        ManualModeCheckBox.IsChecked = currentSettings.ManualMode;

        SelectResizeMode(currentSettings.ResizeMode);
        SelectSharpMode(currentSettings.SharpMode);

        DataObject.AddPastingHandler(QualityTextBox, OnQualityPaste);

        PreviewHost.SizeChanged += OnPreviewHostSizeChanged;

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


    private void ApplyLocalizedText()
    {
        ResizeModeLabel.Text = text.ResizeModeLabel;
        ResizeModeLabel.ToolTip = text.ResizeModeToolTip;
        ResizeModeComboBox.ToolTip = text.ResizeModeToolTip;
        SetComboBoxItemContent(ResizeModeComboBox, "auto", text.ResizeAuto);
        SetComboBoxItemContent(ResizeModeComboBox, "music_cover", text.ResizeMusicCover);

        QualityLabel.Text = text.QualityLabel;

        SharpModeLabel.Text = text.SharpModeLabel;
        SetComboBoxItemContent(SharpModeComboBox, "standard", text.SharpStandard);
        SetComboBoxItemContent(SharpModeComboBox, "increased", text.SharpIncreased);
        SetComboBoxItemContent(SharpModeComboBox, "high", text.SharpHigh);
        SetComboBoxItemContent(SharpModeComboBox, "maximum", text.SharpMaximum);
        SharpModeComboBox.ToolTip = text.SharpModeToolTip;

        SmartModeCheckBox.Content = text.SmartMode;
        SmartModeCheckBox.ToolTip = text.SmartModeToolTip;

        ManualModeCheckBox.Content = text.ManualMode;
        ManualModeCheckBox.ToolTip = text.ManualModeToolTip;

        OpenButton.Content = isManualPreviewLoaded ? text.SaveButton : text.OpenFileButton;
        CenterCropButton.ToolTip = text.CenterCropButtonToolTip;
        CenterCropButton.Visibility = isManualPreviewLoaded ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void SetComboBoxItemContent(ComboBox comboBox, string tag, string content)
    {
        foreach (object item in comboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem &&
                string.Equals(comboBoxItem.Tag as string, tag, StringComparison.OrdinalIgnoreCase))
            {
                comboBoxItem.Content = content;
                return;
            }
        }
    }

    private void OnOpenButtonClick(object sender, RoutedEventArgs e)
    {
        if (isManualPreviewLoaded)
        {
            SaveManualPreview();
            return;
        }

        if (!SaveQualityFromUi(showMessageOnError: true))
        {
            return;
        }

        bool manualMode = ManualModeCheckBox.IsChecked == true;

        var dialog = new OpenFileDialog
        {
            Title = text.OpenDialogTitle,
            Filter = text.OpenDialogFilter,
            Multiselect = !manualMode
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (manualMode)
        {
            LoadManualPreview(dialog.FileName);
            return;
        }

        ProcessSelectedFiles(dialog.FileNames);
    }

    private void OnCenterCropButtonClick(object sender, RoutedEventArgs e)
    {
        if (!isManualPreviewLoaded)
        {
            return;
        }

        CenterManualCrop();
        PreviewHost.Focus();
        e.Handled = true;
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
        e.Handled = true;

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

        if (ManualModeCheckBox.IsChecked == true)
        {
            if (files.Length != 1)
            {
                MessageBox.Show(
                    this,
                    text.ManualSingleFileMessage,
                    text.ManualModeTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            LoadManualPreview(files[0]);
            return;
        }

        ProcessSelectedFiles(files);
    }

    private void OnQualityLostFocus(object sender, RoutedEventArgs e)
    {
        SaveQualityFromUi(showMessageOnError: false);
    }

    private void OnResizeModeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || ResizeModeComboBox.SelectedItem is not ComboBoxItem selectedItem)
        {
            return;
        }

        currentSettings.ResizeMode = AppSettings.NormalizeResizeMode(selectedItem.Tag as string);
        currentSettings.Save();
    }

    private void OnSharpModeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || SharpModeComboBox.SelectedItem is not ComboBoxItem selectedItem)
        {
            return;
        }

        currentSettings.SharpMode = AppSettings.NormalizeSharpMode(selectedItem.Tag as string);
        currentSettings.Save();
    }

    private void OnSmartModeChanged(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        currentSettings.SmartMode = SmartModeCheckBox.IsChecked == true;
        currentSettings.Save();
    }

    private void OnManualModeChanged(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        currentSettings.ManualMode = ManualModeCheckBox.IsChecked == true;
        currentSettings.Save();

        if (!currentSettings.ManualMode)
        {
            ResetManualPreview();
        }
    }

    private void SelectResizeMode(string resizeMode)
    {
        string normalizedResizeMode = AppSettings.NormalizeResizeMode(resizeMode);

        foreach (object item in ResizeModeComboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem &&
                string.Equals(comboBoxItem.Tag as string, normalizedResizeMode, StringComparison.OrdinalIgnoreCase))
            {
                ResizeModeComboBox.SelectedItem = comboBoxItem;
                return;
            }
        }

        ResizeModeComboBox.SelectedIndex = 0;
    }

    private void SelectSharpMode(string sharpMode)
    {
        string normalizedSharpMode = AppSettings.NormalizeSharpMode(sharpMode);

        foreach (object item in SharpModeComboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem &&
                string.Equals(comboBoxItem.Tag as string, normalizedSharpMode, StringComparison.OrdinalIgnoreCase))
            {
                SharpModeComboBox.SelectedItem = comboBoxItem;
                return;
            }
        }

        SharpModeComboBox.SelectedIndex = 0;
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
                    text.InvalidQualityMessage,
                    text.InvalidValueTitle,
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

    private void SetStatusText(string text, string? toolTip = null)
    {
        StatusTextBlock.Text = text;

        if (string.IsNullOrWhiteSpace(toolTip) || !IsStatusTextOverflowing(text))
        {
            StatusTextBlock.ToolTip = null;
            return;
        }

        StatusTextBlock.ToolTip = new ToolTip
        {
            Content = toolTip,
            Style = (Style)FindResource("StatusFileNameToolTipStyle")
        };
    }

    private bool IsStatusTextOverflowing(string text)
    {
        double availableWidth = StatusTextBlock.ActualWidth;

        if (availableWidth <= 0 && !double.IsNaN(StatusTextBlock.Width))
        {
            availableWidth = StatusTextBlock.Width;
        }

        if (availableWidth <= 0)
        {
            return false;
        }

        var typeface = new Typeface(
            StatusTextBlock.FontFamily,
            StatusTextBlock.FontStyle,
            StatusTextBlock.FontWeight,
            StatusTextBlock.FontStretch);

        var formattedText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentUICulture,
            StatusTextBlock.FlowDirection,
            typeface,
            StatusTextBlock.FontSize,
            StatusTextBlock.Foreground,
            VisualTreeHelper.GetDpi(StatusTextBlock).PixelsPerDip);

        return formattedText.WidthIncludingTrailingWhitespace > availableWidth;
    }

    private void ProcessSelectedFiles(string[] files)
    {
        SetStatusText(text.ProcessingStatus);

        var results = ImageProcessor.ProcessFiles(
            files,
            currentSettings.Quality,
            currentSettings.ResizeMode,
            currentSettings.SmartMode,
            currentSettings.SharpMode,
            currentSettings.JpegMode,
            currentSettings.Language);

        foreach (ProcessResult result in results.Where(r => !r.Success && !r.AlreadyCorrectSize))
        {
            MessageBox.Show(
                this,
                result.ErrorMessage ?? text.UnknownError,
                text.ProcessingErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        int created = results.Count(r => r.Success && !r.AlreadyCorrectSize);
        int alreadyCorrect = results.Count(r => r.AlreadyCorrectSize);
        int failed = results.Count(r => !r.Success);

        if (created == 1 && failed == 0)
        {
            string? output = results.FirstOrDefault(r => r.OutputPath != null)?.OutputPath;

            if (output == null)
            {
                SetStatusText(text.DoneStatus);
                return;
            }

            string fileName = Path.GetFileName(output);
            SetStatusText(text.DoneWithFile(fileName), fileName);
            return;
        }

        if (created > 1 && failed == 0)
        {
            SetStatusText(text.CreatedFilesStatus(created));
            return;
        }

        if (alreadyCorrect > 0 && created == 0 && failed == 0)
        {
            SetStatusText(text.FileAlreadyCorrectSize);
            return;
        }

        SetStatusText(text.ProcessingSummary(created, alreadyCorrect, failed));
    }

    private void LoadManualPreview(string sourcePath)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                MessageBox.Show(
                    this,
                    text.FileNotFound,
                    text.OpenErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            BitmapImage bitmap = LoadBitmapImage(sourcePath);

            manualSourcePath = sourcePath;
            manualImageWidth = bitmap.PixelWidth;
            manualImageHeight = bitmap.PixelHeight;
            manualCropSize = Math.Min(manualImageWidth, manualImageHeight);
            manualCropX = (manualImageWidth - manualCropSize) / 2;
            manualCropY = (manualImageHeight - manualCropSize) / 2;

            PreviewImage.Source = bitmap;
            PreviewImage.Visibility = Visibility.Visible;
            DropPlusIcon.Visibility = Visibility.Collapsed;
            CropCanvas.Visibility = Visibility.Visible;

            isManualPreviewLoaded = true;
            OpenButton.Content = text.SaveButton;
            CenterCropButton.Visibility = Visibility.Visible;

            SetStatusText(text.ManualPreviewStatus);

            PreviewHost.Focus();
            UpdateManualPreviewLayout();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                text.OpenImageErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static BitmapImage LoadBitmapImage(string sourcePath)
    {
        var bitmap = new BitmapImage();

        bitmap.BeginInit();
        bitmap.UriSource = new Uri(sourcePath);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        bitmap.EndInit();

        bitmap.Freeze();
        return bitmap;
    }

    private void SaveManualPreview()
    {
        if (!isManualPreviewLoaded || string.IsNullOrWhiteSpace(manualSourcePath))
        {
            return;
        }

        if (!SaveQualityFromUi(showMessageOnError: true))
        {
            return;
        }

        SetStatusText(text.SavingStatus);

        ProcessResult result = ImageProcessor.ProcessManualCropFile(
            manualSourcePath,
            currentSettings.Quality,
            currentSettings.ResizeMode,
            currentSettings.SharpMode,
            currentSettings.JpegMode,
            manualCropX,
            manualCropY,
            manualCropSize,
            currentSettings.Language);

        if (!result.Success)
        {
            MessageBox.Show(
                this,
                result.ErrorMessage ?? text.UnknownError,
                text.SaveErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            SetStatusText(text.SaveErrorStatus);
            return;
        }

        if (result.AlreadyCorrectSize || string.IsNullOrWhiteSpace(result.OutputPath))
        {
            ResetManualPreview();
            SetStatusText(text.FileAlreadyCorrectSize);
            return;
        }

        string fileName = Path.GetFileName(result.OutputPath);
        ResetManualPreview();
        SetStatusText(text.DoneWithFile(fileName), fileName);
    }

    private void ResetManualPreview()
    {
        isManualPreviewLoaded = false;
        isDraggingCrop = false;

        manualSourcePath = null;
        manualImageWidth = 0;
        manualImageHeight = 0;
        manualCropSize = 0;
        manualCropX = 0;
        manualCropY = 0;

        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        DropPlusIcon.Visibility = Visibility.Visible;
        CropCanvas.Visibility = Visibility.Collapsed;

        OpenButton.Content = text.OpenFileButton;
        CenterCropButton.Visibility = Visibility.Collapsed;
        PreviewHost.ReleaseMouseCapture();
    }

    private void OnPreviewHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateManualPreviewLayout();
    }

    private void UpdateManualPreviewLayout()
    {
        if (!isManualPreviewLoaded ||
            manualImageWidth <= 0 ||
            manualImageHeight <= 0 ||
            manualCropSize <= 0)
        {
            return;
        }

        double hostWidth = PreviewHost.ActualWidth;
        double hostHeight = PreviewHost.ActualHeight;

        if (hostWidth <= 0 || hostHeight <= 0)
        {
            return;
        }

        manualPreviewScale = Math.Min(
            hostWidth / manualImageWidth,
            hostHeight / manualImageHeight);

        double renderedWidth = manualImageWidth * manualPreviewScale;
        double renderedHeight = manualImageHeight * manualPreviewScale;

        manualPreviewLeft = (hostWidth - renderedWidth) / 2.0;
        manualPreviewTop = (hostHeight - renderedHeight) / 2.0;

        CropCanvas.Width = hostWidth;
        CropCanvas.Height = hostHeight;

        double cropLeft = manualPreviewLeft + manualCropX * manualPreviewScale;
        double cropTop = manualPreviewTop + manualCropY * manualPreviewScale;
        double cropSize = manualCropSize * manualPreviewScale;

        CropOverlay.Width = cropSize;
        CropOverlay.Height = cropSize;

        Canvas.SetLeft(CropOverlay, cropLeft);
        Canvas.SetTop(CropOverlay, cropTop);
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!isManualPreviewLoaded)
        {
            return;
        }

        isDraggingCrop = true;
        dragStartPoint = e.GetPosition(PreviewHost);
        dragStartCropX = manualCropX;
        dragStartCropY = manualCropY;

        PreviewHost.CaptureMouse();
        PreviewHost.Focus();

        e.Handled = true;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!isManualPreviewLoaded || !isDraggingCrop || manualPreviewScale <= 0)
        {
            return;
        }

        Point currentPoint = e.GetPosition(PreviewHost);
        double deltaX = currentPoint.X - dragStartPoint.X;
        double deltaY = currentPoint.Y - dragStartPoint.Y;

        if (manualImageWidth > manualImageHeight)
        {
            int offsetX = (int)Math.Round(deltaX / manualPreviewScale);
            manualCropX = ClampCropCoordinate(dragStartCropX + offsetX, manualImageWidth - manualCropSize);
        }
        else if (manualImageHeight > manualImageWidth)
        {
            int offsetY = (int)Math.Round(deltaY / manualPreviewScale);
            manualCropY = ClampCropCoordinate(dragStartCropY + offsetY, manualImageHeight - manualCropSize);
        }

        UpdateManualPreviewLayout();
        e.Handled = true;
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!isDraggingCrop)
        {
            return;
        }

        isDraggingCrop = false;
        PreviewHost.ReleaseMouseCapture();

        e.Handled = true;
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!isManualPreviewLoaded)
        {
            return;
        }

        if (Keyboard.FocusedElement == QualityTextBox)
        {
            return;
        }

        int step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
        bool handled = true;

        switch (e.Key)
        {
            case Key.Left:
                MoveManualCrop(-step, 0);
                break;

            case Key.Right:
                MoveManualCrop(step, 0);
                break;

            case Key.Up:
                MoveManualCrop(0, -step);
                break;

            case Key.Down:
                MoveManualCrop(0, step);
                break;

            case Key.Home:
                MoveManualCropToStart();
                break;

            case Key.End:
                MoveManualCropToEnd();
                break;

            default:
                handled = false;
                break;
        }

        if (handled)
        {
            e.Handled = true;
        }
    }

    private void CenterManualCrop()
    {
        if (manualCropSize <= 0)
        {
            return;
        }

        manualCropX = Math.Max(0, (manualImageWidth - manualCropSize) / 2);
        manualCropY = Math.Max(0, (manualImageHeight - manualCropSize) / 2);

        UpdateManualPreviewLayout();
    }

    private void MoveManualCrop(int deltaX, int deltaY)
    {
        if (manualImageWidth > manualImageHeight)
        {
            manualCropX = ClampCropCoordinate(manualCropX + deltaX, manualImageWidth - manualCropSize);
        }
        else if (manualImageHeight > manualImageWidth)
        {
            manualCropY = ClampCropCoordinate(manualCropY + deltaY, manualImageHeight - manualCropSize);
        }

        UpdateManualPreviewLayout();
    }

    private void MoveManualCropToStart()
    {
        if (manualImageWidth > manualImageHeight)
        {
            manualCropX = 0;
        }
        else if (manualImageHeight > manualImageWidth)
        {
            manualCropY = 0;
        }

        UpdateManualPreviewLayout();
    }

    private void MoveManualCropToEnd()
    {
        if (manualImageWidth > manualImageHeight)
        {
            manualCropX = manualImageWidth - manualCropSize;
        }
        else if (manualImageHeight > manualImageWidth)
        {
            manualCropY = manualImageHeight - manualCropSize;
        }

        UpdateManualPreviewLayout();
    }

    private static int ClampCropCoordinate(int value, int maxValue)
    {
        return Math.Clamp(value, 0, Math.Max(0, maxValue));
    }
}