using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ImageSquareResizer;

public partial class MainWindow
{
    private const double MainPageTitleFontSize = 16;
    private const double AdditionalPageTitleFontSize = 16;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWCP_ROUND = 2;
    private const int GWL_STYLE = -16;
    private const long WS_THICKFRAME = 0x00040000L;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

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
        ApplyTitleBarTheme(currentSettings.IsDarkTheme);
    }

    private void ApplyTheme()
    {
        ApplyTheme(currentSettings.IsDarkTheme);
    }

    private void ApplyTheme(bool isDarkTheme)
    {
        ThemeResources.ApplyMain(Resources, isDarkTheme);

        if (IsInitialized)
        {
            ApplyTitleBarTheme(isDarkTheme);
        }
    }

    private void ApplyTitleBarTheme(bool isDarkTheme)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        int useDarkMode = isDarkTheme ? 1 : 0;

        var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        EnsureNativeWindowShadowStyle(handle);

        int cornerPreference = DWMWCP_ROUND;

        _ = DwmSetWindowAttribute(
            handle,
            DWMWA_WINDOW_CORNER_PREFERENCE,
            ref cornerPreference,
            sizeof(int));

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

    private static void EnsureNativeWindowShadowStyle(IntPtr handle)
    {
        // WindowStyle=None with CanMinimize drops the native frame style that DWM uses for its shadow.
        // ResizeMode and WindowChrome still keep the window fixed-size after the style is restored.
        IntPtr currentStyle = GetWindowLongPtr(handle, GWL_STYLE);
        IntPtr shadowStyle = new(currentStyle.ToInt64() | WS_THICKFRAME);

        if (shadowStyle == currentStyle)
        {
            return;
        }

        _ = SetWindowLongPtr(handle, GWL_STYLE, shadowStyle);
        _ = SetWindowPos(
            handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SWP_NOSIZE |
            SWP_NOMOVE |
            SWP_NOZORDER |
            SWP_NOACTIVATE |
            SWP_FRAMECHANGED);
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
        RotateManualButtonText.Text = text.RotateClockwiseButton;
        SaveManualButtonText.Text = text.SaveButton;
        SettingsButtonText.Text = text.AdvancedSettingsButtonText;
        CloseFileMenuItem.Header = text.CloseFileMenuItem;
        HoverTip.SetText(MinimizeWindowButton, text.IsRussian ? "Свернуть" : "Minimize");
        HoverTip.SetText(CloseWindowButton, text.IsRussian ? "Закрыть" : "Close");

        if (AdditionalPages.Visibility != Visibility.Visible)
        {
            UpdateMainPageChrome();
        }

        UpdateDropAreaState();
        UpdateManualResultEstimateText();
    }

    private void UpdateMainPageChrome()
    {
        PageTitleTextBlock.Text = AppVersion.WindowTitle;
        PageTitleTextBlock.FontSize = MainPageTitleFontSize;
        NavigationBackButton.Visibility = Visibility.Hidden;
        Grid.SetColumn(TitleDragArea, 0);
        Grid.SetColumnSpan(TitleDragArea, 3);
    }

    private void OnAdditionalPageChromeChanged(string title, string backToolTip)
    {
        PageTitleTextBlock.Text = title;
        PageTitleTextBlock.FontSize = AdditionalPageTitleFontSize;
        NavigationBackButton.Visibility = Visibility.Visible;
        Grid.SetColumn(TitleDragArea, 2);
        Grid.SetColumnSpan(TitleDragArea, 1);
        HoverTip.SetText(NavigationBackButton, backToolTip);
    }

    private void OnNavigationBackButtonClick(object sender, RoutedEventArgs e)
    {
        HoverTip.DismissUntilMouseLeave(NavigationBackButton);
        AdditionalPages.NavigateBack();
    }

    private void OnMinimizeWindowButtonClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseWindowButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnTitleDragAreaMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ClickCount != 1)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
        }
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

        UpdateLayout();
        double mainWindowWidth = ActualWidth;
        double mainWindowHeight = ActualHeight;

        SizeToContent = System.Windows.SizeToContent.Manual;
        Width = mainWindowWidth;
        Height = mainWindowHeight;

        AdditionalPages.Open(currentSettings);
        MainContentRoot.Visibility = Visibility.Collapsed;
        AdditionalPages.Visibility = Visibility.Visible;
    }

    private void OnAdditionalBackRequested()
    {
        ApplyTheme();
        ReturnToMainPage();
    }

    private void OnAdditionalThemePreviewChanged(bool isDarkTheme)
    {
        ApplyTheme(isDarkTheme);
    }

    private void OnAdditionalSettingsApplied(AppSettings settings)
    {
        bool wasManualPreviewLoaded = manualState.IsLoaded;
        bool wasManualResultEstimateVisible = currentSettings.ShowManualResultEstimate;
        ManualResultState? previousManualResultState = wasManualPreviewLoaded
            ? CaptureManualResultState()
            : null;

        currentSettings.CopyFrom(settings);
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

        ReturnToMainPage();
    }

    private void ReturnToMainPage()
    {
        AdditionalPages.Visibility = Visibility.Collapsed;
        MainContentRoot.Visibility = Visibility.Visible;
        UpdateMainPageChrome();

        ClearValue(WidthProperty);
        ClearValue(HeightProperty);
        SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
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

        string newResizeMode = AppSettings.NormalizeResizeMode(element.Tag as string);
        bool modeChanged = !string.Equals(
            AppSettings.NormalizeResizeMode(currentSettings.ResizeMode),
            newResizeMode,
            StringComparison.OrdinalIgnoreCase);

        if (modeChanged)
        {
            HoverTip.DismissUntilMouseLeave(element);
        }

        currentSettings.ResizeMode = newResizeMode;
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
        bool isAuto = string.Equals(normalizedResizeMode, "auto", StringComparison.OrdinalIgnoreCase);

        ResizeAutoButton.IsChecked = isAuto;
        ResizeCoverButton.IsChecked = !isAuto;
        UpdateResizeModeIndicator(isAuto ? 0d : 138d, animate: IsLoaded && !isApplyingSettingsToUi);
    }

    private void UpdateResizeModeIndicator(double targetX, bool animate)
    {
        const double overshoot = 3d;
        const double movementMilliseconds = 285d;
        const double overshootMilliseconds = movementMilliseconds * 0.82d;

        double currentX = ResizeModeIndicatorTransform.X;
        ResizeModeIndicatorTransform.BeginAnimation(TranslateTransform.XProperty, null);
        ResizeModeIndicatorTransform.X = currentX;

        if (!animate || Math.Abs(currentX - targetX) < 0.1d)
        {
            ResizeModeIndicatorTransform.X = targetX;
            return;
        }

        double direction = Math.Sign(targetX - currentX);
        var animation = new DoubleAnimationUsingKeyFrames
        {
            FillBehavior = FillBehavior.Stop,
        };

        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            currentX,
            KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            targetX + (direction * overshoot),
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(overshootMilliseconds)),
            new CubicEase { EasingMode = EasingMode.EaseOut }));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            targetX,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(movementMilliseconds)),
            new QuadraticEase { EasingMode = EasingMode.EaseOut }));

        animation.Completed += (_, _) =>
        {
            ResizeModeIndicatorTransform.BeginAnimation(TranslateTransform.XProperty, null);
            ResizeModeIndicatorTransform.X = targetX;
        };

        ResizeModeIndicatorTransform.BeginAnimation(
            TranslateTransform.XProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace);
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
