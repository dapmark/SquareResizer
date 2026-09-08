using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageSquareResizer;

public partial class MainWindow
{
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

    private void OnCenterCropButtonClick(object sender, RoutedEventArgs e)
    {
        if (!manualState.IsLoaded)
        {
            return;
        }

        CenterManualCrop();
        PreviewHost.Focus();
        e.Handled = true;
    }

    private void OnRotateManualButtonClick(object sender, RoutedEventArgs e)
    {
        RotateManualPreviewClockwise();
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
        if (!manualState.IsLoaded)
        {
            e.Handled = true;
        }
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

            manualState.SourcePath = sourcePath;
            manualState.ImageWidth = bitmap.PixelWidth;
            manualState.ImageHeight = bitmap.PixelHeight;
            manualState.RotationQuarterTurns = 0;
            int initialCropSize = Math.Min(manualState.ImageWidth, manualState.ImageHeight);
            ApplyManualCrop(ManualCropGeometry.Center(
                manualState.ImageWidth,
                manualState.ImageHeight,
                initialCropSize));
            manualState.SavedResultState = null;

            PreviewImage.Source = bitmap;
            PreviewImage.Visibility = Visibility.Visible;
            DropIdleContent.Visibility = Visibility.Collapsed;
            DropPlusIcon.Visibility = Visibility.Collapsed;
            CropCanvas.Visibility = Visibility.Visible;

            manualState.IsLoaded = true;
            UpdateDropAreaFrame();

            SetStatusText(text.ManualPreviewStatus);

            PreviewHost.Focus();
            UpdateManualPreviewLayout();
            UpdateManualActionButtons();
            UpdateManualResultEstimateText();
            ScheduleManualFileSizeEstimate();
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

    private void RotateManualPreviewClockwise()
    {
        if (!manualState.IsLoaded ||
            manualState.ImageWidth <= 0 ||
            manualState.ImageHeight <= 0 ||
            PreviewImage.Source is not BitmapSource bitmapSource)
        {
            return;
        }

        int previousWidth = manualState.ImageWidth;
        int previousHeight = manualState.ImageHeight;
        ManualCrop rotatedCrop = ManualCropGeometry.RotateClockwise(
            GetManualCrop(),
            previousWidth,
            previousHeight);

        var rotatedBitmap = new TransformedBitmap(bitmapSource, new RotateTransform(90));
        rotatedBitmap.Freeze();

        PreviewImage.Source = rotatedBitmap;
        manualState.ImageWidth = previousHeight;
        manualState.ImageHeight = previousWidth;
        manualState.RotationQuarterTurns = (manualState.RotationQuarterTurns + 1) % 4;
        ApplyManualCrop(rotatedCrop);

        UpdateManualPreviewLayout();
        UpdateManualActionButtons();
        ScheduleManualFileSizeEstimate();
    }

    private void SaveManualPreview()
    {
        if (!manualState.IsLoaded || string.IsNullOrWhiteSpace(manualState.SourcePath))
        {
            return;
        }

        if (!SaveQualityFromUi())
        {
            return;
        }

        if (!HasManualUnsavedChanges())
        {
            return;
        }

        CancelManualFileSizeEstimate(clearEstimate: false);
        UpdateManualResultEstimateText();
        SetStatusText(text.SavingStatus);

        ProcessResult result = ImageProcessor.ProcessManualCropFile(
            manualState.SourcePath,
            currentSettings.Quality,
            currentSettings.ResizeMode,
            currentSettings.SharpMode,
            currentSettings.JpegMode,
            manualState.CropX,
            manualState.CropY,
            manualState.CropSize,
            currentSettings.Language,
            currentSettings.AutoSizeStep,
            currentSettings.OnlineServiceCompatibility,
            manualState.RotationQuarterTurns);

        if (!result.Success)
        {
            MessageBox.Show(
                this,
                result.ErrorMessage ?? text.UnknownError,
                text.SaveErrorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            SetStatusText(text.SaveErrorStatus);
            ScheduleManualFileSizeEstimate();
            return;
        }

        manualState.SavedResultState = CaptureManualResultState();
        UpdateManualActionButtons();

        if (result.AlreadyCorrectSize || string.IsNullOrWhiteSpace(result.OutputPath))
        {
            SetStatusText(text.FileAlreadyCorrectSize);
            PreviewHost.Focus();
            return;
        }

        try
        {
            manualState.EstimatedFileSizeBytes = new FileInfo(result.OutputPath).Length;
            manualState.IsFileSizeEstimatePending = false;
            UpdateManualResultEstimateText();
        }
        catch
        {
            ScheduleManualFileSizeEstimate();
        }

        string fileName = Path.GetFileName(result.OutputPath);
        SetStatusText(text.DoneWithFile(fileName), fileName);
        PreviewHost.Focus();
    }

    private void ResetManualPreview()
    {
        CancelManualFileSizeEstimate(clearEstimate: true);

        manualState.ResetPreview();

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
        UpdateManualResultEstimateText();
        UpdateDropAreaFrame();
        PreviewHost.Cursor = null;
        PreviewHost.ReleaseMouseCapture();
    }

    private void UpdateDropAreaState()
    {
        UpdateDropAreaFrame();

        if (manualState.IsLoaded)
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
        bool hasPreview = manualState.IsLoaded;

        DropArea.BorderThickness = hasPreview ? new Thickness(1) : new Thickness(0);
        DropArea.CornerRadius = hasPreview ? new CornerRadius(0) : new CornerRadius(6);

        DropDashedBorder.Visibility = hasPreview ? Visibility.Collapsed : Visibility.Visible;
        DropDashedBorder.RadiusX = hasPreview ? 0 : 6;
        DropDashedBorder.RadiusY = hasPreview ? 0 : 6;
    }

    private void CloseManualPreview()
    {
        if (!manualState.IsLoaded)
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
        UpdateManualResultEstimateText();
    }

    private void UpdateManualActionButtons()
    {
        bool manualMode = ManualModeCheckBox.IsChecked == true;
        bool hasUnsavedChanges = manualMode && manualState.IsLoaded && HasManualUnsavedChanges();

        SaveManualButton.IsEnabled = hasUnsavedChanges;
        CenterCropButton.IsEnabled = manualMode && manualState.IsLoaded && !IsManualCropCentered();
        RotateManualButton.IsEnabled = manualMode && manualState.IsLoaded;

        if (hasUnsavedChanges)
        {
            SetStatusText(text.ManualPreviewStatus);
        }
    }

    private bool HasManualUnsavedChanges()
    {
        if (!manualState.IsLoaded)
        {
            return false;
        }

        return manualState.SavedResultState is not { } savedState ||
            savedState != CaptureManualResultState();
    }

    private ManualResultState CaptureManualResultState(int? qualityOverride = null)
    {
        int quality = qualityOverride ?? GetManualQualityFromUi();

        return new ManualResultState(
            manualState.SourcePath ?? string.Empty,
            manualState.CropX,
            manualState.CropY,
            manualState.CropSize,
            manualState.RotationQuarterTurns,
            AppSettings.NormalizeQuality(quality),
            AppSettings.NormalizeResizeMode(currentSettings.ResizeMode),
            AppSettings.NormalizeSharpMode(currentSettings.SharpMode),
            AppSettings.NormalizeJpegMode(currentSettings.JpegMode),
            AppSettings.NormalizeAutoSizeStep(currentSettings.AutoSizeStep),
            currentSettings.OnlineServiceCompatibility);
    }

    private int GetManualQualityFromUi()
    {
        return int.TryParse(QualityTextBox.Text.Trim(), out int quality)
            ? AppSettings.NormalizeQuality(quality)
            : AppSettings.NormalizeQuality(currentSettings.Quality);
    }

    private bool IsManualCropCentered()
    {
        return !manualState.IsLoaded || ManualCropGeometry.IsCentered(
            GetManualCrop(),
            manualState.ImageWidth,
            manualState.ImageHeight);
    }
}
