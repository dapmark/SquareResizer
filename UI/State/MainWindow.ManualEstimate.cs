using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ImageSquareResizer;

public partial class MainWindow
{
    private const int ManualFileSizeEstimateDelayMilliseconds = 300;

    private void ScheduleManualFileSizeEstimate()
    {
        if (!currentSettings.ShowManualResultEstimate)
        {
            CancelManualFileSizeEstimate(clearEstimate: true);
            UpdateManualResultEstimateText();
            return;
        }

        if (!manualState.IsLoaded ||
            string.IsNullOrWhiteSpace(manualState.SourcePath) ||
            !int.TryParse(QualityTextBox.Text.Trim(), out int quality))
        {
            CancelManualFileSizeEstimate(clearEstimate: true);
            UpdateManualResultEstimateText();
            return;
        }

        manualState.FileSizeEstimateCancellation?.Cancel();

        int requestId = ++manualState.FileSizeEstimateRequestId;
        var cancellation = new CancellationTokenSource();
        manualState.FileSizeEstimateCancellation = cancellation;

        manualState.EstimatedFileSizeBytes = null;
        manualState.IsFileSizeEstimatePending = true;

        ManualResultState state = CaptureManualResultState(AppSettings.NormalizeQuality(quality));
        UpdateManualResultEstimateText();

        _ = EstimateManualFileSizeAsync(state, requestId, cancellation);
    }

    private void InvalidateManualFileSizeEstimate()
    {
        manualState.FileSizeEstimateCancellation?.Cancel();
        manualState.FileSizeEstimateCancellation = null;
        manualState.FileSizeEstimateRequestId++;
        manualState.EstimatedFileSizeBytes = null;
        manualState.IsFileSizeEstimatePending = manualState.IsLoaded && currentSettings.ShowManualResultEstimate;
        UpdateManualResultEstimateText();
    }

    private void CancelManualFileSizeEstimate(bool clearEstimate)
    {
        manualState.FileSizeEstimateCancellation?.Cancel();
        manualState.FileSizeEstimateCancellation = null;
        manualState.FileSizeEstimateRequestId++;
        manualState.IsFileSizeEstimatePending = false;

        if (clearEstimate)
        {
            manualState.EstimatedFileSizeBytes = null;
        }

        UpdateManualResultEstimateText();
    }

    private async Task EstimateManualFileSizeAsync(
        ManualResultState state,
        int requestId,
        CancellationTokenSource cancellation)
    {
        CancellationToken token = cancellation.Token;

        try
        {
            await Task.Delay(ManualFileSizeEstimateDelayMilliseconds, token);
            await manualState.FileSizeEstimateGate.WaitAsync(token);

            long estimatedBytes;

            try
            {
                token.ThrowIfCancellationRequested();

                estimatedBytes = await Task.Run(
                    () => ImageProcessor.EstimateManualCropFileSize(
                        state.SourcePath,
                        state.Quality,
                        state.ResizeMode,
                        state.SharpMode,
                        state.JpegMode,
                        state.CropX,
                        state.CropY,
                        state.CropSize,
                        state.AutoSizeStep,
                        state.OnlineServiceCompatibility,
                        state.RotationQuarterTurns),
                    token);
            }
            finally
            {
                manualState.FileSizeEstimateGate.Release();
            }

            token.ThrowIfCancellationRequested();

            await Dispatcher.InvokeAsync(() =>
            {
                if (requestId != manualState.FileSizeEstimateRequestId)
                {
                    return;
                }

                if (ReferenceEquals(manualState.FileSizeEstimateCancellation, cancellation))
                {
                    manualState.FileSizeEstimateCancellation = null;
                }

                manualState.EstimatedFileSizeBytes = estimatedBytes;
                manualState.IsFileSizeEstimatePending = false;
                UpdateManualResultEstimateText();
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (requestId != manualState.FileSizeEstimateRequestId)
                {
                    return;
                }

                if (ReferenceEquals(manualState.FileSizeEstimateCancellation, cancellation))
                {
                    manualState.FileSizeEstimateCancellation = null;
                }

                manualState.EstimatedFileSizeBytes = null;
                manualState.IsFileSizeEstimatePending = false;
                UpdateManualResultEstimateText();
            });
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    private string GetManualCropBadgeText()
    {
        return $"{manualState.CropSize}×{manualState.CropSize}";
    }

    private void UpdateManualResultEstimateText()
    {
        bool showEstimate = currentSettings.ShowManualResultEstimate &&
            ManualModeCheckBox.IsChecked == true &&
            manualState.IsLoaded &&
            manualState.CropSize > 0;

        ManualResultEstimateTextBlock.Visibility = showEstimate
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!showEstimate)
        {
            ManualResultEstimateTextBlock.Text = string.Empty;
            return;
        }

        int targetSize = ImageProcessor.GetTargetSizeFromSquareSize(
            manualState.CropSize,
            currentSettings.ResizeMode,
            currentSettings.AutoSizeStep);

        if (manualState.IsFileSizeEstimatePending)
        {
            ManualResultEstimateTextBlock.Text = text.ManualResultEstimate(targetSize, "…");
            return;
        }

        ManualResultEstimateTextBlock.Text = manualState.EstimatedFileSizeBytes is long bytes
            ? text.ManualResultEstimate(targetSize, FormatFileSize(bytes))
            : text.ManualResultDimensions(targetSize);
    }

    private string FormatFileSize(long bytes)
    {
        const double bytesPerKilobyte = 1024.0;
        const double bytesPerMegabyte = bytesPerKilobyte * 1024.0;

        CultureInfo culture = text.IsRussian
            ? CultureInfo.GetCultureInfo("ru-RU")
            : CultureInfo.GetCultureInfo("en-US");

        if (bytes >= bytesPerMegabyte)
        {
            double megabytes = bytes / bytesPerMegabyte;
            string format = megabytes >= 10 ? "0.0" : "0.00";
            return megabytes.ToString(format, culture) + (text.IsRussian ? " МБ" : " MB");
        }

        if (bytes >= bytesPerKilobyte)
        {
            double kilobytes = bytes / bytesPerKilobyte;
            string format = kilobytes >= 100 ? "0" : "0.0";
            return kilobytes.ToString(format, culture) + (text.IsRussian ? " КБ" : " KB");
        }

        return bytes.ToString(culture) + (text.IsRussian ? " Б" : " B");
    }
}
