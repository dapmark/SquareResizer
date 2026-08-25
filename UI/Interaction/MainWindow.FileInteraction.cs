using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace ImageSquareResizer;

public partial class MainWindow
{
    private void OnOpenButtonClick(object sender, RoutedEventArgs e)
    {
        if (manualState.IsLoaded)
        {
            SaveManualPreview();
            return;
        }

        if (!SaveQualityFromUi())
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

        if (hasSupportedFiles && !manualState.IsLoaded)
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

        if (!SaveQualityFromUi())
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
        if (manualState.IsLoaded)
        {
            DropPlusIcon.Visibility = Visibility.Collapsed;
            return;
        }

        DropPlusIcon.Visibility = Visibility.Collapsed;
        DropIdleContent.Visibility = Visibility.Visible;
    }

    private void SetStatusText(string value, string? toolTip = null, bool isError = false)
    {
        StatusTextBlock.Text = value;
        StatusTextBlock.SetResourceReference(
            TextBlock.ForegroundProperty,
            isError ? "StatusErrorTextBrush" : "StatusTextBrush");
        HoverTip.SetShowWhenTrimmed(StatusTextBlock, true);
        HoverTip.SetText(StatusTextBlock, toolTip);
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
            currentSettings.AutoSizeStep,
            currentSettings.OnlineServiceCompatibility);

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
}
