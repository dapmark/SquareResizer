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
    private const double DarkBorderMixRatio = 0.03;
    private const double LightBorderMixRatio = 0.06;
    private const double CropCornerHitSize = 12.0;
    private const double CropSideHitSize = 8.0;
    private const int MinManualCropSize = 32;

    private enum ManualCropDragMode
    {
        None,
        Move,
        ResizeTopLeft,
        ResizeTopRight,
        ResizeBottomLeft,
        ResizeBottomRight,
        ResizeLeft,
        ResizeRight,
        ResizeTop,
        ResizeBottom
    }

    private readonly record struct ManualResultState(
        string SourcePath,
        int CropX,
        int CropY,
        int CropSize,
        int Quality,
        string ResizeMode,
        string SharpMode,
        int JpegMode,
        int AutoSizeStep);

    private AppSettings currentSettings;
    private Localization text;
    private bool isApplyingSettingsToUi;

    private bool isManualPreviewLoaded;
    private bool isDraggingCrop;
    private ManualCropDragMode manualCropDragMode = ManualCropDragMode.None;

    private string? manualSourcePath;
    private int manualImageWidth;
    private int manualImageHeight;
    private int manualCropSize;
    private int manualCropX;
    private int manualCropY;
    private ManualResultState? savedManualResultState;

    private double manualPreviewLeft;
    private double manualPreviewTop;
    private double manualPreviewScale = 1.0;

    private Point dragStartPoint;
    private int dragStartCropX;
    private int dragStartCropY;
    private int dragStartCropSize;

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
        ApplySettingsToUi();

        DataObject.AddPastingHandler(QualityTextBox, OnQualityPaste);

        PreviewHost.SizeChanged += OnPreviewHostSizeChanged;

        ApplyTheme();
        SourceInitialized += OnSourceInitialized;
    }

    internal void OpenManualStartupFiles(string[] paths)
    {
        OpenManualFiles(paths);
    }

    private void OpenManualFiles(string[] files)
    {
        EnableManualModeForCurrentSession();

        string[] supportedFiles = files
            .Where(path => ImageProcessor.IsSupportedInputFile(path) && File.Exists(path))
            .ToArray();

        if (supportedFiles.Length == 0)
        {
            SetStatusText(text.ManualNoSupportedFileStatus);
            PreviewHost.Focus();
            return;
        }

        if (LoadManualPreview(supportedFiles[0]) && supportedFiles.Length > 1)
        {
            SetStatusText(text.ManualFirstFileStatus);
        }

        PreviewHost.Focus();
    }

    private void EnableManualModeForCurrentSession()
    {
        if (ManualModeCheckBox.IsChecked == true)
        {
            return;
        }

        isApplyingSettingsToUi = true;

        try
        {
            ManualModeCheckBox.IsChecked = true;
        }
        finally
        {
            isApplyingSettingsToUi = false;
        }
    }

    private void OnMainSettingsPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width <= 0)
        {
            return;
        }

        DropArea.Width = e.NewSize.Width;
        DropArea.Height = e.NewSize.Width;
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
        var windowBackground = Color.FromRgb(243, 243, 243);
        var dropAreaBackground = Color.FromRgb(245, 245, 245);

        Resources["WindowBackgroundBrush"] = BrushFromColor(windowBackground);
        Resources["DropAreaBackgroundBrush"] = BrushFromColor(dropAreaBackground);
        Resources["DropAreaBorderBrush"] = BrushFromColor(MixColor(dropAreaBackground, Colors.Black, LightBorderMixRatio));
        Resources["DropAreaDashedBorderBrush"] = BrushFromColor(MixColor(dropAreaBackground, Colors.Black, 0.16));
        Resources["MainTextBrush"] = BrushFromRgb(70, 70, 70);
        Resources["SecondaryTextBrush"] = BrushFromRgb(50, 50, 50);
        Resources["ButtonBackgroundBrush"] = BrushFromRgb(250, 250, 250);
        Resources["ButtonHoverBackgroundBrush"] = BrushFromRgb(245, 249, 255);
        Resources["ButtonPressedBackgroundBrush"] = BrushFromRgb(230, 242, 255);
        Resources["ButtonBorderBrush"] = BrushFromColor(MixColor(dropAreaBackground, Colors.Black, LightBorderMixRatio));
        Resources["AccentBorderBrush"] = BrushFromRgb(0, 120, 215);
        Resources["InputBackgroundBrush"] = BrushFromRgb(255, 255, 255);
        Resources["StatusTextBrush"] = BrushFromRgb(80, 80, 80);
    }

    private void ApplyDarkTheme()
    {
        var windowBackground = Color.FromRgb(32, 32, 32);
        var dropAreaBackground = Color.FromRgb(37, 37, 38);

        Resources["WindowBackgroundBrush"] = BrushFromColor(windowBackground);
        Resources["DropAreaBackgroundBrush"] = BrushFromColor(dropAreaBackground);
        Resources["DropAreaBorderBrush"] = BrushFromColor(MixColor(dropAreaBackground, Colors.White, DarkBorderMixRatio));
        Resources["DropAreaDashedBorderBrush"] = BrushFromColor(MixColor(dropAreaBackground, Colors.White, 0.18));
        Resources["MainTextBrush"] = BrushFromRgb(212, 212, 212);
        Resources["SecondaryTextBrush"] = BrushFromRgb(212, 212, 212);
        Resources["ButtonBackgroundBrush"] = BrushFromRgb(45, 45, 48);
        Resources["ButtonHoverBackgroundBrush"] = BrushFromRgb(62, 62, 66);
        Resources["ButtonPressedBackgroundBrush"] = BrushFromRgb(0, 122, 204);
        Resources["ButtonBorderBrush"] = BrushFromColor(MixColor(dropAreaBackground, Colors.White, DarkBorderMixRatio));
        Resources["AccentBorderBrush"] = BrushFromRgb(0, 122, 204);
        Resources["InputBackgroundBrush"] = BrushFromRgb(32, 32, 32);
        Resources["StatusTextBrush"] = BrushFromRgb(200, 200, 200);
    }

    private static SolidColorBrush BrushFromRgb(byte red, byte green, byte blue)
    {
        return BrushFromColor(Color.FromRgb(red, green, blue));
    }

    private static SolidColorBrush BrushFromColor(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color MixColor(Color baseColor, Color mixColor, double ratio)
    {
        ratio = Math.Clamp(ratio, 0.0, 1.0);

        return Color.FromRgb(
            MixChannel(baseColor.R, mixColor.R, ratio),
            MixChannel(baseColor.G, mixColor.G, ratio),
            MixChannel(baseColor.B, mixColor.B, ratio));
    }

    private static byte MixChannel(byte baseValue, byte mixValue, double ratio)
    {
        return (byte)Math.Round(baseValue + (mixValue - baseValue) * ratio);
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
        ResizeModeLabel.ToolTip = null;
        ResizeAutoButton.Content = text.ResizeAuto;
        ResizeAutoButton.ToolTip = text.ResizeAutoToolTip;
        ResizeCoverButton.Content = text.ResizeMusicCover;
        ResizeCoverButton.ToolTip = text.ResizeMusicCoverToolTip;

        QualityLabel.Text = text.QualityLabel;

        SharpModeLabel.Text = text.SharpModeLabel;
        SetComboBoxItemContent(SharpModeComboBox, "standard", text.SharpStandard);
        SetComboBoxItemContent(SharpModeComboBox, "increased", text.SharpIncreased);
        SetComboBoxItemContent(SharpModeComboBox, "high", text.SharpHigh);
        SetComboBoxItemContent(SharpModeComboBox, "maximum", text.SharpMaximum);

        SmartModeCheckBox.Content = text.SmartMode;
        SmartModeCheckBox.ToolTip = text.SmartModeToolTip;

        ManualModeCheckBox.Content = text.ManualMode;
        ManualModeCheckBox.ToolTip = text.ManualModeToolTip;

        SelectFileButton.Content = text.SelectFileButton;
        DropOrTextBlock.Text = text.DropOrText;
        DropHereTextBlock.Text = text.DropHereText;
        CenterCropButtonText.Text = text.CenterCropButton;
        CenterCropButton.ToolTip = null;
        SaveManualButtonText.Text = text.SaveButton;
        SaveManualButton.ToolTip = null;
        SettingsButtonText.Text = text.AdvancedSettingsButtonText;
        SettingsButton.ToolTip = null;
        CloseFileMenuItem.Header = text.CloseFileMenuItem;
        UpdateDropAreaState();
    }

    private void ApplySettingsToUi()
    {
        isApplyingSettingsToUi = true;

        try
        {
            text = Localization.For(currentSettings.Language);

            ApplyLocalizedText();

            QualityTextBox.Text = currentSettings.Quality.ToString();
            SmartModeCheckBox.IsChecked = currentSettings.SmartMode;
            ManualModeCheckBox.IsChecked = currentSettings.ManualMode;
            UpdateManualActionsPanel();

            SelectResizeMode(currentSettings.ResizeMode);
            SelectSharpMode(currentSettings.SharpMode);
        }
        finally
        {
            isApplyingSettingsToUi = false;
        }
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

    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        if (!SaveQualityFromUi(showMessageOnError: true))
        {
            return;
        }

        var dialog = new SettingsWindow(currentSettings)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        bool wasManualPreviewLoaded = isManualPreviewLoaded;
        ManualResultState? previousManualResultState = wasManualPreviewLoaded
            ? CaptureManualResultState()
            : null;

        currentSettings.CopyFrom(dialog.Settings);
        currentSettings.Save();
        ApplySettingsToUi();
        ApplyTheme();

        if (wasManualPreviewLoaded && previousManualResultState != CaptureManualResultState())
        {
            UpdateManualActionButtons();
        }

        if (wasManualPreviewLoaded && !currentSettings.ManualMode)
        {
            ResetManualPreview();
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

    private void OnSaveManualButtonClick(object sender, RoutedEventArgs e)
    {
        SaveManualPreview();
        e.Handled = true;
    }

    private void OnCloseFileMenuItemClick(object sender, RoutedEventArgs e)
    {
        CloseManualPreview();
        e.Handled = true;
    }

    private void OnPreviewHostContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (!isManualPreviewLoaded)
        {
            e.Handled = true;
        }
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        UpdateDragState(e);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        UpdateDragState(e);
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        HideDropPlusIcon();
        e.Handled = true;
    }

    private void UpdateDragState(DragEventArgs e)
    {
        bool hasSupportedFiles = TryGetDroppedFiles(e.Data, out string[] files) &&
            files.Any(ImageProcessor.IsSupportedInputFile);

        e.Effects = hasSupportedFiles ? DragDropEffects.Copy : DragDropEffects.None;

        if (hasSupportedFiles && !isManualPreviewLoaded)
        {
            DropIdleContent.Visibility = Visibility.Collapsed;
            DropPlusIcon.Visibility = Visibility.Visible;
        }

        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        HideDropPlusIcon();

        if (!TryGetDroppedFiles(e.Data, out string[] files) || files.Length == 0)
        {
            return;
        }

        if (!SaveQualityFromUi(showMessageOnError: true))
        {
            return;
        }

        if (ManualModeCheckBox.IsChecked == true)
        {
            OpenManualFiles(files);
            return;
        }

        ProcessSelectedFiles(files);
    }

    private static bool TryGetDroppedFiles(IDataObject dataObject, out string[] files)
    {
        files = Array.Empty<string>();

        if (!dataObject.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        files = dataObject.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();
        return files.Length > 0;
    }

    private void HideDropPlusIcon()
    {
        if (isManualPreviewLoaded)
        {
            DropPlusIcon.Visibility = Visibility.Collapsed;
            return;
        }

        DropPlusIcon.Visibility = Visibility.Collapsed;
        DropIdleContent.Visibility = Visibility.Visible;
    }

    private void OnQualityLostFocus(object sender, RoutedEventArgs e)
    {
        SaveQualityFromUi(showMessageOnError: false);
    }

    private void OnComboBoxPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.Items.Count == 0 || comboBox.IsDropDownOpen)
        {
            return;
        }

        int direction = e.Delta > 0 ? -1 : 1;
        int currentIndex = comboBox.SelectedIndex >= 0 ? comboBox.SelectedIndex : 0;
        int nextIndex = Math.Clamp(currentIndex + direction, 0, comboBox.Items.Count - 1);

        if (nextIndex != comboBox.SelectedIndex)
        {
            comboBox.SelectedIndex = nextIndex;
        }

        e.Handled = true;
    }

    private void OnResizeModeButtonClick(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || isApplyingSettingsToUi || sender is not FrameworkElement element)
        {
            return;
        }

        currentSettings.ResizeMode = AppSettings.NormalizeResizeMode(element.Tag as string);
        currentSettings.Save();
        SelectResizeMode(currentSettings.ResizeMode);
        UpdateManualActionButtons();
    }

    private void OnSharpModeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || isApplyingSettingsToUi || SharpModeComboBox.SelectedItem is not ComboBoxItem selectedItem)
        {
            return;
        }

        currentSettings.SharpMode = AppSettings.NormalizeSharpMode(selectedItem.Tag as string);
        currentSettings.Save();
        UpdateManualActionButtons();
    }

    private void OnSmartModeChanged(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || isApplyingSettingsToUi)
        {
            return;
        }

        currentSettings.SmartMode = SmartModeCheckBox.IsChecked == true;
        currentSettings.Save();
    }

    private void OnManualModeChanged(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || isApplyingSettingsToUi)
        {
            return;
        }

        currentSettings.ManualMode = ManualModeCheckBox.IsChecked == true;
        currentSettings.Save();
        UpdateManualActionsPanel();

        if (!currentSettings.ManualMode)
        {
            ResetManualPreview();
        }
    }

    private void SelectResizeMode(string resizeMode)
    {
        string normalizedResizeMode = AppSettings.NormalizeResizeMode(resizeMode);

        ResizeAutoButton.IsChecked = string.Equals(normalizedResizeMode, "auto", StringComparison.OrdinalIgnoreCase);
        ResizeCoverButton.IsChecked = string.Equals(normalizedResizeMode, "music_cover", StringComparison.OrdinalIgnoreCase);
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
        UpdateManualActionButtons();

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
        UpdateManualActionButtons();
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
            currentSettings.Language,
            currentSettings.SmartPaddingPercent,
            currentSettings.SmartPaddingMaxPx,
            currentSettings.AutoSizeStep);

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

    private bool LoadManualPreview(string sourcePath)
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

                return false;
            }

            BitmapImage bitmap = LoadBitmapImage(sourcePath);

            manualSourcePath = sourcePath;
            manualImageWidth = bitmap.PixelWidth;
            manualImageHeight = bitmap.PixelHeight;
            manualCropSize = Math.Min(manualImageWidth, manualImageHeight);
            manualCropX = (manualImageWidth - manualCropSize) / 2;
            manualCropY = (manualImageHeight - manualCropSize) / 2;
            savedManualResultState = null;

            PreviewImage.Source = bitmap;
            PreviewImage.Visibility = Visibility.Visible;
            DropIdleContent.Visibility = Visibility.Collapsed;
            DropPlusIcon.Visibility = Visibility.Collapsed;
            CropCanvas.Visibility = Visibility.Visible;

            isManualPreviewLoaded = true;
            UpdateDropAreaFrame();

            SetStatusText(text.ManualPreviewStatus);

            PreviewHost.Focus();
            UpdateManualPreviewLayout();
            UpdateManualActionButtons();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                text.OpenImageErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
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

        if (!HasManualUnsavedChanges())
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
            currentSettings.Language,
            currentSettings.AutoSizeStep);

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

        savedManualResultState = CaptureManualResultState();
        UpdateManualActionButtons();

        if (result.AlreadyCorrectSize || string.IsNullOrWhiteSpace(result.OutputPath))
        {
            SetStatusText(text.FileAlreadyCorrectSize);
            PreviewHost.Focus();
            return;
        }

        string fileName = Path.GetFileName(result.OutputPath);
        SetStatusText(text.DoneWithFile(fileName), fileName);
        PreviewHost.Focus();
    }

    private void ResetManualPreview()
    {
        isManualPreviewLoaded = false;
        isDraggingCrop = false;
        manualCropDragMode = ManualCropDragMode.None;

        manualSourcePath = null;
        manualImageWidth = 0;
        manualImageHeight = 0;
        manualCropSize = 0;
        manualCropX = 0;
        manualCropY = 0;
        savedManualResultState = null;

        PreviewImage.Source = null;
        PreviewImage.Width = double.NaN;
        PreviewImage.Height = double.NaN;
        PreviewImage.Margin = new Thickness(0);
        PreviewImage.Visibility = Visibility.Collapsed;
        DropIdleContent.Visibility = Visibility.Visible;
        DropPlusIcon.Visibility = Visibility.Collapsed;
        CropCanvas.Clip = null;
        CropCanvas.Visibility = Visibility.Collapsed;

        UpdateManualActionButtons();
        UpdateDropAreaFrame();
        PreviewHost.Cursor = null;
        PreviewHost.ReleaseMouseCapture();
    }

    private void UpdateDropAreaState()
    {
        UpdateDropAreaFrame();

        if (isManualPreviewLoaded)
        {
            DropIdleContent.Visibility = Visibility.Collapsed;
            DropPlusIcon.Visibility = Visibility.Collapsed;
            UpdateManualActionButtons();
            return;
        }

        DropIdleContent.Visibility = Visibility.Visible;
        DropPlusIcon.Visibility = Visibility.Collapsed;
        UpdateManualActionButtons();
    }

    private void UpdateDropAreaFrame()
    {
        bool hasPreview = isManualPreviewLoaded;

        DropArea.BorderThickness = hasPreview ? new Thickness(1) : new Thickness(0);
        DropArea.CornerRadius = hasPreview ? new CornerRadius(0) : new CornerRadius(6);

        DropDashedBorder.Visibility = hasPreview ? Visibility.Collapsed : Visibility.Visible;
        DropDashedBorder.RadiusX = hasPreview ? 0 : 6;
        DropDashedBorder.RadiusY = hasPreview ? 0 : 6;
    }

    private void CloseManualPreview()
    {
        if (!isManualPreviewLoaded)
        {
            return;
        }

        ResetManualPreview();
        SetStatusText(string.Empty);
        PreviewHost.Focus();
    }

    private void UpdateManualActionsPanel()
    {
        bool manualMode = ManualModeCheckBox.IsChecked == true;
        ManualActionsPanel.Visibility = manualMode ? Visibility.Visible : Visibility.Collapsed;
        UpdateManualActionButtons();
    }

    private void UpdateManualActionButtons()
    {
        bool manualMode = ManualModeCheckBox.IsChecked == true;
        SaveManualButton.IsEnabled = manualMode && isManualPreviewLoaded && HasManualUnsavedChanges();
        CenterCropButton.IsEnabled = manualMode && isManualPreviewLoaded && !IsManualCropCentered();
    }

    private bool HasManualUnsavedChanges()
    {
        if (!isManualPreviewLoaded)
        {
            return false;
        }

        return savedManualResultState is not { } savedState ||
            savedState != CaptureManualResultState();
    }

    private ManualResultState CaptureManualResultState()
    {
        return new ManualResultState(
            manualSourcePath ?? string.Empty,
            manualCropX,
            manualCropY,
            manualCropSize,
            AppSettings.NormalizeQuality(currentSettings.Quality),
            AppSettings.NormalizeResizeMode(currentSettings.ResizeMode),
            AppSettings.NormalizeSharpMode(currentSettings.SharpMode),
            AppSettings.NormalizeJpegMode(currentSettings.JpegMode),
            AppSettings.NormalizeAutoSizeStep(currentSettings.AutoSizeStep));
    }

    private bool IsManualCropCentered()
    {
        if (!isManualPreviewLoaded || manualCropSize <= 0)
        {
            return true;
        }

        int centeredX = Math.Max(0, (manualImageWidth - manualCropSize) / 2);
        int centeredY = Math.Max(0, (manualImageHeight - manualCropSize) / 2);

        return manualCropX == centeredX && manualCropY == centeredY;
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

        PreviewImage.Width = renderedWidth;
        PreviewImage.Height = renderedHeight;
        PreviewImage.Margin = new Thickness(manualPreviewLeft, manualPreviewTop, 0, 0);

        CropCanvas.Width = hostWidth;
        CropCanvas.Height = hostHeight;
        CropCanvas.Clip = new RectangleGeometry(new Rect(manualPreviewLeft, manualPreviewTop, renderedWidth, renderedHeight));

        double cropLeft = manualPreviewLeft + manualCropX * manualPreviewScale;
        double cropTop = manualPreviewTop + manualCropY * manualPreviewScale;
        double cropSize = manualCropSize * manualPreviewScale;

        CropOverlay.Width = cropSize;
        CropOverlay.Height = cropSize;

        Canvas.SetLeft(CropOverlay, cropLeft);
        Canvas.SetTop(CropOverlay, cropTop);

        UpdateManualCropAdorners(cropLeft, cropTop, cropSize);
    }

    private void UpdateManualCropAdorners(double cropLeft, double cropTop, double cropSize)
    {
        double centerX = cropLeft + cropSize / 2.0;
        double centerY = cropTop + cropSize / 2.0;

        CropCenterVerticalLine.X1 = centerX;
        CropCenterVerticalLine.Y1 = cropTop;
        CropCenterVerticalLine.X2 = centerX;
        CropCenterVerticalLine.Y2 = cropTop + cropSize;

        CropCenterHorizontalLine.X1 = cropLeft;
        CropCenterHorizontalLine.Y1 = centerY;
        CropCenterHorizontalLine.X2 = cropLeft + cropSize;
        CropCenterHorizontalLine.Y2 = centerY;

        CropSizeTextBlock.Text = $"{manualCropSize}×{manualCropSize}";
        CropSizeBadge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        double badgeLeft = cropLeft + 6;
        double badgeTop = cropTop + 6;
        double badgeWidth = CropSizeBadge.DesiredSize.Width;
        double badgeHeight = CropSizeBadge.DesiredSize.Height;

        if (badgeLeft + badgeWidth > cropLeft + cropSize - 6)
        {
            badgeLeft = cropLeft + cropSize - badgeWidth - 6;
        }

        if (badgeTop + badgeHeight > cropTop + cropSize - 6)
        {
            badgeTop = cropTop + cropSize - badgeHeight - 6;
        }

        Canvas.SetLeft(CropSizeBadge, Math.Max(cropLeft + 2, badgeLeft));
        Canvas.SetTop(CropSizeBadge, Math.Max(cropTop + 2, badgeTop));

        PositionCropHandle(CropHandleTopLeft, cropLeft, cropTop);
        PositionCropHandle(CropHandleTopRight, cropLeft + cropSize, cropTop);
        PositionCropHandle(CropHandleBottomLeft, cropLeft, cropTop + cropSize);
        PositionCropHandle(CropHandleBottomRight, cropLeft + cropSize, cropTop + cropSize);
        PositionCropHandle(CropHandleTopSide, centerX, cropTop);
        PositionCropHandle(CropHandleRightSide, cropLeft + cropSize, centerY);
        PositionCropHandle(CropHandleBottomSide, centerX, cropTop + cropSize);
        PositionCropHandle(CropHandleLeftSide, cropLeft, centerY);
    }

    private static void PositionCropHandle(FrameworkElement handle, double centerX, double centerY)
    {
        double width = double.IsNaN(handle.Width) || handle.Width <= 0 ? 9.0 : handle.Width;
        double height = double.IsNaN(handle.Height) || handle.Height <= 0 ? 9.0 : handle.Height;

        Canvas.SetLeft(handle, centerX - width / 2.0);
        Canvas.SetTop(handle, centerY - height / 2.0);
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!isManualPreviewLoaded)
        {
            return;
        }

        Point point = e.GetPosition(PreviewHost);
        manualCropDragMode = GetManualCropDragMode(point);

        if (manualCropDragMode == ManualCropDragMode.None)
        {
            return;
        }

        isDraggingCrop = true;
        dragStartPoint = point;
        dragStartCropX = manualCropX;
        dragStartCropY = manualCropY;
        dragStartCropSize = manualCropSize;

        PreviewHost.CaptureMouse();
        PreviewHost.Focus();
        PreviewHost.Cursor = GetManualCropCursor(manualCropDragMode);

        e.Handled = true;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!isManualPreviewLoaded || manualPreviewScale <= 0)
        {
            return;
        }

        Point currentPoint = e.GetPosition(PreviewHost);

        if (!isDraggingCrop)
        {
            UpdateManualPreviewCursor(currentPoint);
            return;
        }

        int previousCropX = manualCropX;
        int previousCropY = manualCropY;
        int previousCropSize = manualCropSize;

        if (manualCropDragMode == ManualCropDragMode.Move)
        {
            MoveManualCropByMouse(currentPoint);
        }
        else
        {
            ResizeManualCropByMouse(currentPoint);
        }

        if (manualCropX != previousCropX ||
            manualCropY != previousCropY ||
            manualCropSize != previousCropSize)
        {
            UpdateManualPreviewLayout();
            UpdateManualActionButtons();
        }

        e.Handled = true;
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!isDraggingCrop)
        {
            return;
        }

        isDraggingCrop = false;
        manualCropDragMode = ManualCropDragMode.None;
        PreviewHost.ReleaseMouseCapture();
        UpdateManualPreviewCursor(e.GetPosition(PreviewHost));

        e.Handled = true;
    }

    private void MoveManualCropByMouse(Point currentPoint)
    {
        Point adjustedPoint = GetAdjustedManualDragPoint(currentPoint);
        double deltaX = adjustedPoint.X - dragStartPoint.X;
        double deltaY = adjustedPoint.Y - dragStartPoint.Y;

        int offsetX = (int)Math.Round(deltaX / manualPreviewScale);
        int offsetY = (int)Math.Round(deltaY / manualPreviewScale);

        manualCropX = ClampCropCoordinate(dragStartCropX + offsetX, manualImageWidth - manualCropSize);
        manualCropY = ClampCropCoordinate(dragStartCropY + offsetY, manualImageHeight - manualCropSize);
    }

    private void ResizeManualCropByMouse(Point currentPoint)
    {
        Point adjustedPoint = GetAdjustedManualDragPoint(currentPoint);
        Point imagePoint = PreviewPointToImagePoint(adjustedPoint);
        int minSize = GetMinManualCropSize();

        switch (manualCropDragMode)
        {
            case ManualCropDragMode.ResizeTopLeft:
                ResizeManualCropFromTopLeft(imagePoint, minSize);
                break;

            case ManualCropDragMode.ResizeTopRight:
                ResizeManualCropFromTopRight(imagePoint, minSize);
                break;

            case ManualCropDragMode.ResizeBottomLeft:
                ResizeManualCropFromBottomLeft(imagePoint, minSize);
                break;

            case ManualCropDragMode.ResizeBottomRight:
                ResizeManualCropFromBottomRight(imagePoint, minSize);
                break;

            case ManualCropDragMode.ResizeLeft:
                ResizeManualCropFromLeft(imagePoint, minSize);
                break;

            case ManualCropDragMode.ResizeRight:
                ResizeManualCropFromRight(imagePoint, minSize);
                break;

            case ManualCropDragMode.ResizeTop:
                ResizeManualCropFromTop(imagePoint, minSize);
                break;

            case ManualCropDragMode.ResizeBottom:
                ResizeManualCropFromBottom(imagePoint, minSize);
                break;
        }
    }

    private void ResizeManualCropFromTopLeft(Point imagePoint, int minSize)
    {
        int anchorX = dragStartCropX + dragStartCropSize;
        int anchorY = dragStartCropY + dragStartCropSize;
        int maxSize = Math.Min(anchorX, anchorY);
        int size = GetClampedManualCropSize(Math.Min(anchorX - imagePoint.X, anchorY - imagePoint.Y), minSize, maxSize);

        manualCropX = anchorX - size;
        manualCropY = anchorY - size;
        manualCropSize = size;
    }

    private void ResizeManualCropFromTopRight(Point imagePoint, int minSize)
    {
        int anchorX = dragStartCropX;
        int anchorY = dragStartCropY + dragStartCropSize;
        int maxSize = Math.Min(manualImageWidth - anchorX, anchorY);
        int size = GetClampedManualCropSize(Math.Min(imagePoint.X - anchorX, anchorY - imagePoint.Y), minSize, maxSize);

        manualCropX = anchorX;
        manualCropY = anchorY - size;
        manualCropSize = size;
    }

    private void ResizeManualCropFromBottomLeft(Point imagePoint, int minSize)
    {
        int anchorX = dragStartCropX + dragStartCropSize;
        int anchorY = dragStartCropY;
        int maxSize = Math.Min(anchorX, manualImageHeight - anchorY);
        int size = GetClampedManualCropSize(Math.Min(anchorX - imagePoint.X, imagePoint.Y - anchorY), minSize, maxSize);

        manualCropX = anchorX - size;
        manualCropY = anchorY;
        manualCropSize = size;
    }

    private void ResizeManualCropFromBottomRight(Point imagePoint, int minSize)
    {
        int anchorX = dragStartCropX;
        int anchorY = dragStartCropY;
        int maxSize = Math.Min(manualImageWidth - anchorX, manualImageHeight - anchorY);
        int size = GetClampedManualCropSize(Math.Min(imagePoint.X - anchorX, imagePoint.Y - anchorY), minSize, maxSize);

        manualCropX = anchorX;
        manualCropY = anchorY;
        manualCropSize = size;
    }

    private void ResizeManualCropFromLeft(Point imagePoint, int minSize)
    {
        int right = dragStartCropX + dragStartCropSize;
        int centerY = dragStartCropY + dragStartCropSize / 2;
        int maxSize = Math.Min(right, Math.Min(centerY * 2, (manualImageHeight - centerY) * 2));
        int size = GetClampedManualCropSize(right - imagePoint.X, minSize, maxSize);

        manualCropX = right - size;
        manualCropY = ClampCropCoordinate(centerY - size / 2, manualImageHeight - size);
        manualCropSize = size;
    }

    private void ResizeManualCropFromRight(Point imagePoint, int minSize)
    {
        int left = dragStartCropX;
        int centerY = dragStartCropY + dragStartCropSize / 2;
        int maxSize = Math.Min(manualImageWidth - left, Math.Min(centerY * 2, (manualImageHeight - centerY) * 2));
        int size = GetClampedManualCropSize(imagePoint.X - left, minSize, maxSize);

        manualCropX = left;
        manualCropY = ClampCropCoordinate(centerY - size / 2, manualImageHeight - size);
        manualCropSize = size;
    }

    private void ResizeManualCropFromTop(Point imagePoint, int minSize)
    {
        int bottom = dragStartCropY + dragStartCropSize;
        int centerX = dragStartCropX + dragStartCropSize / 2;
        int maxSize = Math.Min(bottom, Math.Min(centerX * 2, (manualImageWidth - centerX) * 2));
        int size = GetClampedManualCropSize(bottom - imagePoint.Y, minSize, maxSize);

        manualCropX = ClampCropCoordinate(centerX - size / 2, manualImageWidth - size);
        manualCropY = bottom - size;
        manualCropSize = size;
    }

    private void ResizeManualCropFromBottom(Point imagePoint, int minSize)
    {
        int top = dragStartCropY;
        int centerX = dragStartCropX + dragStartCropSize / 2;
        int maxSize = Math.Min(manualImageHeight - top, Math.Min(centerX * 2, (manualImageWidth - centerX) * 2));
        int size = GetClampedManualCropSize(imagePoint.Y - top, minSize, maxSize);

        manualCropX = ClampCropCoordinate(centerX - size / 2, manualImageWidth - size);
        manualCropY = top;
        manualCropSize = size;
    }

    private Point GetAdjustedManualDragPoint(Point currentPoint)
    {
        double factor = 1.0;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            factor = 0.25;
        }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            factor = 2.0;
        }

        return new Point(
            dragStartPoint.X + (currentPoint.X - dragStartPoint.X) * factor,
            dragStartPoint.Y + (currentPoint.Y - dragStartPoint.Y) * factor);
    }

    private Point PreviewPointToImagePoint(Point point)
    {
        return new Point(
            (point.X - manualPreviewLeft) / manualPreviewScale,
            (point.Y - manualPreviewTop) / manualPreviewScale);
    }

    private int GetMinManualCropSize()
    {
        int maxCropSize = Math.Min(manualImageWidth, manualImageHeight);
        return Math.Min(MinManualCropSize, Math.Max(1, maxCropSize));
    }

    private static int GetClampedManualCropSize(double rawSize, int minSize, int maxSize)
    {
        maxSize = Math.Max(1, maxSize);
        minSize = Math.Clamp(minSize, 1, maxSize);
        int size = (int)Math.Round(rawSize);

        return Math.Clamp(size, minSize, maxSize);
    }

    private void UpdateManualPreviewCursor(Point point)
    {
        PreviewHost.Cursor = GetManualCropCursor(GetManualCropDragMode(point));
    }

    private ManualCropDragMode GetManualCropDragMode(Point point)
    {
        if (!TryGetManualCropScreenRect(out Rect cropRect))
        {
            return ManualCropDragMode.None;
        }

        if (IsNearPoint(point, cropRect.Left, cropRect.Top))
        {
            return ManualCropDragMode.ResizeTopLeft;
        }

        if (IsNearPoint(point, cropRect.Right, cropRect.Top))
        {
            return ManualCropDragMode.ResizeTopRight;
        }

        if (IsNearPoint(point, cropRect.Left, cropRect.Bottom))
        {
            return ManualCropDragMode.ResizeBottomLeft;
        }

        if (IsNearPoint(point, cropRect.Right, cropRect.Bottom))
        {
            return ManualCropDragMode.ResizeBottomRight;
        }

        if (IsNearVerticalSide(point, cropRect.Left, cropRect.Top, cropRect.Bottom))
        {
            return ManualCropDragMode.ResizeLeft;
        }

        if (IsNearVerticalSide(point, cropRect.Right, cropRect.Top, cropRect.Bottom))
        {
            return ManualCropDragMode.ResizeRight;
        }

        if (IsNearHorizontalSide(point, cropRect.Top, cropRect.Left, cropRect.Right))
        {
            return ManualCropDragMode.ResizeTop;
        }

        if (IsNearHorizontalSide(point, cropRect.Bottom, cropRect.Left, cropRect.Right))
        {
            return ManualCropDragMode.ResizeBottom;
        }

        return cropRect.Contains(point) ? ManualCropDragMode.Move : ManualCropDragMode.None;
    }

    private bool TryGetManualCropScreenRect(out Rect cropRect)
    {
        cropRect = Rect.Empty;

        if (!isManualPreviewLoaded || manualPreviewScale <= 0 || manualCropSize <= 0)
        {
            return false;
        }

        cropRect = new Rect(
            manualPreviewLeft + manualCropX * manualPreviewScale,
            manualPreviewTop + manualCropY * manualPreviewScale,
            manualCropSize * manualPreviewScale,
            manualCropSize * manualPreviewScale);

        return true;
    }

    private static bool IsNearPoint(Point point, double x, double y)
    {
        return Math.Abs(point.X - x) <= CropCornerHitSize &&
               Math.Abs(point.Y - y) <= CropCornerHitSize;
    }

    private static bool IsNearVerticalSide(Point point, double sideX, double top, double bottom)
    {
        double sideStart = top + CropCornerHitSize;
        double sideEnd = bottom - CropCornerHitSize;

        if (sideStart > sideEnd)
        {
            sideStart = top;
            sideEnd = bottom;
        }

        return Math.Abs(point.X - sideX) <= CropSideHitSize &&
               point.Y >= sideStart &&
               point.Y <= sideEnd;
    }

    private static bool IsNearHorizontalSide(Point point, double sideY, double left, double right)
    {
        double sideStart = left + CropCornerHitSize;
        double sideEnd = right - CropCornerHitSize;

        if (sideStart > sideEnd)
        {
            sideStart = left;
            sideEnd = right;
        }

        return Math.Abs(point.Y - sideY) <= CropSideHitSize &&
               point.X >= sideStart &&
               point.X <= sideEnd;
    }

    private static Cursor? GetManualCropCursor(ManualCropDragMode mode)
    {
        return mode switch
        {
            ManualCropDragMode.Move => Cursors.SizeAll,
            ManualCropDragMode.ResizeTopLeft => Cursors.SizeNWSE,
            ManualCropDragMode.ResizeBottomRight => Cursors.SizeNWSE,
            ManualCropDragMode.ResizeTopRight => Cursors.SizeNESW,
            ManualCropDragMode.ResizeBottomLeft => Cursors.SizeNESW,
            ManualCropDragMode.ResizeLeft => Cursors.SizeWE,
            ManualCropDragMode.ResizeRight => Cursors.SizeWE,
            ManualCropDragMode.ResizeTop => Cursors.SizeNS,
            ManualCropDragMode.ResizeBottom => Cursors.SizeNS,
            _ => null
        };
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!isManualPreviewLoaded)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            CloseManualPreview();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.S)
        {
            if (HasManualUnsavedChanges())
            {
                SaveManualPreview();
            }

            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Home)
        {
            CenterManualCrop();
            e.Handled = true;
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
        UpdateManualActionButtons();
    }

    private void MoveManualCrop(int deltaX, int deltaY)
    {
        manualCropX = ClampCropCoordinate(manualCropX + deltaX, manualImageWidth - manualCropSize);
        manualCropY = ClampCropCoordinate(manualCropY + deltaY, manualImageHeight - manualCropSize);

        UpdateManualPreviewLayout();
        UpdateManualActionButtons();
    }

    private void MoveManualCropToStart()
    {
        manualCropX = 0;
        manualCropY = 0;

        UpdateManualPreviewLayout();
        UpdateManualActionButtons();
    }

    private void MoveManualCropToEnd()
    {
        manualCropX = Math.Max(0, manualImageWidth - manualCropSize);
        manualCropY = Math.Max(0, manualImageHeight - manualCropSize);

        UpdateManualPreviewLayout();
        UpdateManualActionButtons();
    }

    private static int ClampCropCoordinate(int value, int maxValue)
    {
        return Math.Clamp(value, 0, Math.Max(0, maxValue));
    }
}
