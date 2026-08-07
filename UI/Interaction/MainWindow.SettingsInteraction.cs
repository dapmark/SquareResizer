using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ImageSquareResizer;

public partial class MainWindow
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_BORDER_COLOR = 34;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

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
        ThemeResources.ApplyMain(Resources, currentSettings.IsDarkTheme);

        if (IsInitialized)
        {
            ApplyTitleBarTheme();
        }
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

        if (Resources["WindowBorderBrush"] is SolidColorBrush borderBrush)
        {
            int borderColor = ToColorRef(borderBrush.Color);

            _ = DwmSetWindowAttribute(
                handle,
                DWMWA_BORDER_COLOR,
                ref borderColor,
                sizeof(int));
        }
    }

    private static int ToColorRef(Color color)
    {
        return color.R | (color.G << 8) | (color.B << 16);
    }

    private void ApplyLocalizedText()
    {
        ResizeModeLabel.Text = text.ResizeModeLabel;
        ResizeAutoButton.Content = text.ResizeAuto;
        HoverTip.SetText(ResizeAutoButton, text.ResizeAutoToolTip);
        ResizeCoverButton.Content = text.ResizeMusicCover;
        HoverTip.SetText(ResizeCoverButton, text.ResizeMusicCoverToolTip);

        QualityLabel.Text = text.QualityLabel;

        SharpModeLabel.Text = text.SharpModeLabel;
        HoverTip.SetText(SharpModeLabel, text.SharpModeToolTip);
        SetComboBoxItemContent(SharpModeComboBox, "standard", text.SharpStandard);
        SetComboBoxItemContent(SharpModeComboBox, "increased", text.SharpIncreased);
        SetComboBoxItemContent(SharpModeComboBox, "high", text.SharpHigh);
        SetComboBoxItemContent(SharpModeComboBox, "maximum", text.SharpMaximum);

        SmartModeCheckBox.Content = text.SmartMode;
        HoverTip.SetText(SmartModeCheckBox, text.SmartModeToolTip);

        ManualModeCheckBox.Content = text.ManualMode;
        HoverTip.SetText(ManualModeCheckBox, text.ManualModeToolTip);

        SelectFileButton.Content = text.SelectFileButton;
        DropOrTextBlock.Text = text.DropOrText;
        DropHereTextBlock.Text = text.DropHereText;
        CenterCropButtonText.Text = text.CenterCropButton;
        SaveManualButtonText.Text = text.SaveButton;
        SettingsButtonText.Text = text.AdvancedSettingsButtonText;
        CloseFileMenuItem.Header = text.CloseFileMenuItem;
        UpdateDropAreaState();
        UpdateManualResultEstimateText();
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
        if (!SaveQualityFromUi())
        {
            return;
        }

        bool? dialogResult = null;
        SettingsWindow? dialog = null;
        MainContentRoot.Effect = new BlurEffect
        {
            Radius = 4,
            RenderingBias = RenderingBias.Performance,
        };

        try
        {
            dialog = new SettingsWindow(currentSettings)
            {
                Owner = this
            };

            dialogResult = dialog.ShowDialog();
        }
        finally
        {
            MainContentRoot.Effect = null;
        }

        if (dialogResult != true || dialog is null)
        {
            return;
        }

        bool wasManualPreviewLoaded = manualState.IsLoaded;
        bool wasManualResultEstimateVisible = currentSettings.ShowManualResultEstimate;
        ManualResultState? previousManualResultState = wasManualPreviewLoaded
            ? CaptureManualResultState()
            : null;

        currentSettings.CopyFrom(dialog.Settings);
        currentSettings.Save();
        ApplySettingsToUi();
        ApplyTheme();

        if (wasManualPreviewLoaded)
        {
            UpdateManualPreviewLayout();
        }

        if (wasManualPreviewLoaded && previousManualResultState != CaptureManualResultState())
        {
            UpdateManualActionButtons();
            ScheduleManualFileSizeEstimate();
        }
        else if (wasManualPreviewLoaded &&
                 !wasManualResultEstimateVisible &&
                 currentSettings.ShowManualResultEstimate)
        {
            ScheduleManualFileSizeEstimate();
        }
        else if (!currentSettings.ShowManualResultEstimate)
        {
            CancelManualFileSizeEstimate(clearEstimate: true);
            UpdateManualResultEstimateText();
        }

        if (wasManualPreviewLoaded && !currentSettings.ManualMode)
        {
            ResetManualPreview();
            SetStatusText(string.Empty);
        }
    }

    private void OnQualityTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsInitialized || isApplyingSettingsToUi)
        {
            return;
        }

        UpdateManualActionButtons();
        ScheduleManualFileSizeEstimate();
    }

    private void OnQualityLostFocus(object sender, RoutedEventArgs e)
    {
        SaveQualityFromUi();
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
        ScheduleManualFileSizeEstimate();
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
        ScheduleManualFileSizeEstimate();
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
            SetStatusText(string.Empty);
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
        SaveQualityFromUi();
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

    private bool SaveQualityFromUi()
    {
        string rawText = QualityTextBox.Text.Trim();

        if (!int.TryParse(rawText, out int quality) || quality is < 1 or > 100)
        {
            hasQualityValidationError = true;
            SetStatusText(text.InvalidQualityMessage, isError: true);
            QualityTextBox.Focus();
            QualityTextBox.SelectAll();
            return false;
        }

        currentSettings.Quality = quality;
        currentSettings.Save();

        QualityTextBox.Text = currentSettings.Quality.ToString();
        UpdateManualActionButtons();
        ScheduleManualFileSizeEstimate();

        if (hasQualityValidationError)
        {
            hasQualityValidationError = false;

            if (manualState.IsLoaded && HasManualUnsavedChanges())
            {
                SetStatusText(text.ManualPreviewStatus);
            }
            else
            {
                SetStatusText(string.Empty);
            }
        }

        return true;
    }
}
