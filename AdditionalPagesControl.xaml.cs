using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace ImageSquareResizer;

public partial class AdditionalPagesControl : UserControl
{
    private enum SettingsPage
    {
        Settings,
        About,
        Licenses,
    }

    private const double LicenseSeparatorSafetyMargin = 1.5;
    private const int PhysicalAKeyScanCode = 0x1E;
    private const int WmKeyDown = 0x0100;

    private static readonly string[] LicenseResourcePaths =
    {
        "Licenses/Magick.NET-Apache-2.0.txt",
        "Licenses/ImageMagick-License.txt",
        "Licenses/SharpVectors-BSD-3-Clause.txt",
        "Licenses/Tabler-MIT.txt",
    };

    private AppSettings settingsDraft = new();
    private Localization text = Localization.For(AppSettings.DefaultLanguage);
    private bool isApplyingUi;
    private bool licenseTextRefreshQueued;
    private SettingsPage currentPage = SettingsPage.Settings;
    private HwndSource? hwndSource;
    private double licenseSeparatorLayoutWidth = double.NaN;
    private int licenseSeparatorLength;
    private bool isLicenseScrollBarSyncing;
    private WindowsIntegrationSnapshot windowsIntegrationSnapshot;
    private bool hasWindowsIntegrationState;
    internal event Action? BackRequested;
    internal event Action<string, string>? PageChromeChanged;
    internal event Action<bool>? ThemePreviewChanged;
    internal event Action<AppSettings>? SettingsApplied;

    public AdditionalPagesControl()
    {
        InitializeComponent();
        LicenseTextViewer.ScrollOffsetChanged += LicenseTextViewer_OnScrollOffsetChanged;
        Loaded += AdditionalPagesControl_OnLoaded;
        Unloaded += AdditionalPagesControl_OnUnloaded;

        DataObject.AddPastingHandler(SmartPaddingMaxPxTextBox, OnIntegerPaste);
        DataObject.AddPastingHandler(SmartPaddingPercentTextBox, OnDecimalPaste);

        ApplyDraftToUi();
        ApplyTheme();
    }

    internal void Open(AppSettings settings)
    {
        settingsDraft = settings.Clone();
        currentPage = SettingsPage.Settings;
        ApplyDraftToUi();
        ApplyTheme();
        ClearValidationStatus();
        LoadWindowsIntegrationState();
        SwitchPage(SettingsPage.Settings);
    }

    internal void NavigateBack()
    {
        if (currentPage == SettingsPage.Licenses)
        {
            SwitchPage(SettingsPage.About);
            return;
        }

        if (currentPage == SettingsPage.About)
        {
            SwitchPage(SettingsPage.Settings);
            return;
        }

        BackRequested?.Invoke();
    }

    private void ApplyDraftToUi()
    {
        isApplyingUi = true;

        try
        {
            text = Localization.For(settingsDraft.Language);
            ApplyLocalizedText();

            SelectComboBoxItem(LanguageComboBox, settingsDraft.Language);
            SelectComboBoxItem(ThemeComboBox, settingsDraft.Theme);
            SelectComboBoxItem(JpegModeComboBox, settingsDraft.JpegMode.ToString(CultureInfo.InvariantCulture));
            SelectComboBoxItem(AutoSizeStepComboBox, settingsDraft.AutoSizeStep.ToString(CultureInfo.InvariantCulture));
            OnlineServiceCompatibilityCheckBox.IsChecked = settingsDraft.OnlineServiceCompatibility;

            SmartPaddingPercentTextBox.Text = AppSettings.FormatDouble(settingsDraft.SmartPaddingPercent);
            SmartPaddingMaxPxTextBox.Text = settingsDraft.SmartPaddingMaxPx.ToString(CultureInfo.InvariantCulture);
        }
        finally
        {
            isApplyingUi = false;
        }
    }

    private void ApplyLocalizedText()
    {
        InterfaceSectionTextBlock.Text = text.IsRussian ? "Интерфейс" : "Interface";
        LanguageLabel.Text = text.IsRussian ? "Язык" : "Language";
        ThemeLabel.Text = text.IsRussian ? "Тема" : "Theme";
        AdvancedSectionTextBlock.Text = text.IsRussian ? "Обработка" : "Processing";
        JpegModeLabel.Text = text.IsRussian ? "JPEG режим" : "JPEG mode";
        AutoSizeStepLabel.Text = text.IsRussian ? "Шаг авторазмера" : "Auto size step";
        OnlineServiceCompatibilityCheckBox.Content = text.IsRussian
            ? "Улучшать совместимость с онлайн-сервисами"
            : "Improve compatibility with online services";
        SmartPaddingSectionTextBlock.Text = text.IsRussian ? "Умный режим" : "Smart mode";
        SmartPaddingPercentLabel.Text = text.IsRussian ? "Макс. разница, %" : "Max difference, %";
        SmartPaddingMaxPxLabel.Text = text.IsRussian ? "Макс. дорисовка, пкс" : "Max fill, px";
        WindowsIntegrationSectionTextBlock.Text = text.IsRussian
            ? "Интеграция с Windows"
            : "Windows integration";
        ContextMenuIntegrationCheckBox.Content = text.IsRussian
            ? "Изменение размера в контекстном меню"
            : "Resize command in context menu";
        ManualContextMenuIntegrationCheckBox.Content = text.IsRussian
            ? "Ручной режим в контекстном меню"
            : "Manual mode in context menu";
        SendToIntegrationCheckBox.Content = text.IsRussian
            ? "Ярлык в меню «Отправить»"
            : "Shortcut in the Send to menu";

        var jpegModeToolTip = text.IsRussian ? "Компактный режим уменьшает вес за счет некоторого снижения качества, максимальный режим сохраняет качество, но увеличивает вес" : "Compact mode reduces file size with some quality loss, maximum mode keeps quality but increases file size";
        var autoSizeStepToolTip = text.IsRussian ? "Задаёт шаг округления вниз для варианта «Авто»" : "Sets the downward rounding step for the Auto option";
        var onlineServiceCompatibilityToolTip = text.IsRussian
            ? "Преобразует изображение в sRGB и удаляет EXIF, XMP, комментарии, цветовые профили и другие необязательные данные"
            : "Converts the image to sRGB and removes EXIF, XMP, comments, color profiles, and other optional data";
        var smartPaddingPercentToolTip = text.IsRussian
            ? "Задаёт максимальную разницу между шириной и высотой в процентах от большей стороны, при которой изображение можно дополнить фоном до квадрата вместо обрезки"
            : "Sets the maximum difference between width and height as a percentage of the larger side at which the image may be padded to a square instead of cropped";
        var smartPaddingMaxPxToolTip = text.IsRussian ? "Ограничивает кол-во пикселей, которое можно добавить фоном" : "Limits the number of pixels that can be added as background";

        HoverTip.SetText(JpegModeLabel, jpegModeToolTip);
        HoverTip.SetText(AutoSizeStepLabel, autoSizeStepToolTip);
        HoverTip.SetText(OnlineServiceCompatibilityCheckBox, onlineServiceCompatibilityToolTip);
        HoverTip.SetText(SmartPaddingPercentLabel, smartPaddingPercentToolTip);
        HoverTip.SetText(SmartPaddingMaxPxLabel, smartPaddingMaxPxToolTip);
        ApplyWindowsIntegrationToolTips();
        UpdateWindowsIntegrationInfo();

        AboutButtonText.Text = text.IsRussian ? "О программе" : "About";
        ResetButtonText.Text = text.IsRussian ? "Сброс" : "Reset";
        ApplyButtonText.Text = text.IsRussian ? "Применить" : "Apply";
        LicenseButton.Content = text.IsRussian
            ? "Лицензия и сторонние компоненты"
            : "License and third-party components";
        LicenseCopyMenuItem.Header = text.IsRussian ? "Копировать" : "Copy";
        LicenseSelectAllMenuItem.Header = text.IsRussian ? "Выделить всё" : "Select all";
        licenseSeparatorLayoutWidth = double.NaN;
        licenseSeparatorLength = 0;
        LicenseTextViewer.Clear();
        SyncLicenseScrollBar();

        var buildDate = GetBuildDate();
        AboutVersionTextBlock.Text = text.IsRussian
            ? string.IsNullOrWhiteSpace(buildDate)
                ? $"Версия: {AppVersion.Current}"
                : $"Версия: {AppVersion.Current} build {buildDate}"
            : string.IsNullOrWhiteSpace(buildDate)
                ? $"Version: {AppVersion.Current}"
                : $"Version: {AppVersion.Current} build {buildDate}";

        SetComboBoxItemContent(LanguageComboBox, "en", "English");
        SetComboBoxItemContent(LanguageComboBox, "ru", "Русский");
        SetComboBoxItemContent(ThemeComboBox, "dark", text.IsRussian ? "Тёмная" : "Dark");
        SetComboBoxItemContent(ThemeComboBox, "light", text.IsRussian ? "Светлая" : "Light");
        SetComboBoxItemContent(JpegModeComboBox, "1", text.IsRussian ? "Компактный" : "Compact");
        SetComboBoxItemContent(JpegModeComboBox, "2", text.IsRussian ? "Сбалансированный" : "Balanced");
        SetComboBoxItemContent(JpegModeComboBox, "3", text.IsRussian ? "Максимальный" : "Maximum");

        UpdatePageChrome();
    }

    private void ApplyTheme()
    {
        ThemeResources.ApplySettings(Resources, settingsDraft.IsDarkTheme);
        SyncLicenseContextMenuThemeResources();
        ThemePreviewChanged?.Invoke(settingsDraft.IsDarkTheme);
    }

    private void SyncLicenseContextMenuThemeResources()
    {
        string[] keys =
        {
            "WindowBackgroundBrush",
            "ButtonBorderBrush",
            "MainTextBrush",
            "SecondaryTextBrush",
            "SoftButtonHoverBackgroundBrush",
        };

        foreach (string key in keys)
        {
            if (Resources.Contains(key))
            {
                LicenseContextMenu.Resources[key] = Resources[key];
            }
        }
    }

    private void LoadWindowsIntegrationState()
    {
        hasWindowsIntegrationState = false;
        bool isAvailable = WindowsIntegrationService.IsManagementAvailable;
        SetWindowsIntegrationControlsEnabled(isAvailable);

        if (!isAvailable)
        {
            ContextMenuIntegrationCheckBox.IsChecked = false;
            ManualContextMenuIntegrationCheckBox.IsChecked = false;
            SendToIntegrationCheckBox.IsChecked = false;
            ApplyWindowsIntegrationToolTips();
            UpdateWindowsIntegrationInfo();
            return;
        }

        try
        {
            windowsIntegrationSnapshot = WindowsIntegrationService.GetState();
            hasWindowsIntegrationState = true;
            ApplyWindowsIntegrationStateToUi();
        }
        catch (Exception ex)
        {
            SetWindowsIntegrationControlsEnabled(false);
            ShowValidationMessage(
                text.IsRussian
                    ? $"Не удалось определить состояние интеграции с Windows: {ex.Message}"
                    : $"Could not determine the Windows integration state: {ex.Message}");
        }

        ApplyWindowsIntegrationToolTips();
        UpdateWindowsIntegrationInfo();
    }

    private bool TryApplyWindowsIntegrationSelection()
    {
        if (!WindowsIntegrationService.IsManagementAvailable)
        {
            return true;
        }

        var selection = new WindowsIntegrationSelection(
            ContextMenuIntegrationCheckBox.IsChecked == true,
            ManualContextMenuIntegrationCheckBox.IsChecked == true,
            SendToIntegrationCheckBox.IsChecked == true);

        try
        {
            windowsIntegrationSnapshot = WindowsIntegrationService.Apply(selection);
            hasWindowsIntegrationState = true;
            ApplyWindowsIntegrationStateToUi();
            ApplyWindowsIntegrationToolTips();
            UpdateWindowsIntegrationInfo();
            return true;
        }
        catch (Exception ex)
        {
            TryRefreshWindowsIntegrationStateAfterFailure();
            ShowValidationMessage(
                text.IsRussian
                    ? $"Не удалось применить интеграцию с Windows: {ex.Message}"
                    : $"Could not apply the Windows integration: {ex.Message}");
            return false;
        }
    }

    private void TryRefreshWindowsIntegrationStateAfterFailure()
    {
        try
        {
            windowsIntegrationSnapshot = WindowsIntegrationService.GetState();
            hasWindowsIntegrationState = true;
            ApplyWindowsIntegrationStateToUi();
        }
        catch
        {
            hasWindowsIntegrationState = false;
            SetWindowsIntegrationControlsEnabled(false);
        }

        ApplyWindowsIntegrationToolTips();
        UpdateWindowsIntegrationInfo();
    }

    private void ApplyWindowsIntegrationStateToUi()
    {
        ContextMenuIntegrationCheckBox.IsChecked =
            windowsIntegrationSnapshot.ContextMenu != WindowsIntegrationStatus.Absent;
        ManualContextMenuIntegrationCheckBox.IsChecked =
            windowsIntegrationSnapshot.ManualContextMenu != WindowsIntegrationStatus.Absent;
        SendToIntegrationCheckBox.IsChecked =
            windowsIntegrationSnapshot.SendToShortcut != WindowsIntegrationStatus.Absent;
        SetWindowsIntegrationControlsEnabled(true);
    }

    private void SetWindowsIntegrationControlsEnabled(bool isEnabled)
    {
        ContextMenuIntegrationCheckBox.IsEnabled = isEnabled;
        ManualContextMenuIntegrationCheckBox.IsEnabled = isEnabled;
        SendToIntegrationCheckBox.IsEnabled = isEnabled;
    }

    private void ApplyWindowsIntegrationToolTips()
    {
        string contextMenuToolTip = text.IsRussian
            ? "Добавляет команду «Преобразовать с SquareResizer» для поддерживаемых изображений"
            : "Adds the Convert with SquareResizer command for supported images";
        string manualContextMenuToolTip = text.IsRussian
            ? "Добавляет команду открытия изображения сразу в ручном режиме"
            : "Adds a command that opens an image directly in manual mode";
        string sendToToolTip = text.IsRussian
            ? "Добавляет ярлык SquareResizer в меню Windows «Отправить»"
            : "Adds a SquareResizer shortcut to the Windows Send to menu";

        if (hasWindowsIntegrationState)
        {
            contextMenuToolTip = AddRepairHint(
                contextMenuToolTip,
                windowsIntegrationSnapshot.ContextMenu);
            manualContextMenuToolTip = AddRepairHint(
                manualContextMenuToolTip,
                windowsIntegrationSnapshot.ManualContextMenu);
            sendToToolTip = AddRepairHint(
                sendToToolTip,
                windowsIntegrationSnapshot.SendToShortcut);
        }

        HoverTip.SetText(ContextMenuIntegrationCheckBox, contextMenuToolTip);
        HoverTip.SetText(ManualContextMenuIntegrationCheckBox, manualContextMenuToolTip);
        HoverTip.SetText(SendToIntegrationCheckBox, sendToToolTip);
    }

    private string AddRepairHint(string toolTip, WindowsIntegrationStatus status)
    {
        if (status != WindowsIntegrationStatus.NeedsRepair)
        {
            return toolTip;
        }

        return text.IsRussian
            ? $"{toolTip}. Текущая запись требует обновления"
            : $"{toolTip}. The current registration needs to be updated";
    }

    private void UpdateWindowsIntegrationInfo()
    {
        if (!WindowsIntegrationService.IsManagementAvailable)
        {
            WindowsIntegrationInfoTextBlock.Text = text.IsRussian
                ? "Только в готовой сборке"
                : "Published build only";
            WindowsIntegrationInfoTextBlock.Visibility = Visibility.Visible;
            return;
        }

        bool hasSelectedRepair = hasWindowsIntegrationState &&
            ((ContextMenuIntegrationCheckBox.IsChecked == true &&
              windowsIntegrationSnapshot.ContextMenu == WindowsIntegrationStatus.NeedsRepair) ||
             (ManualContextMenuIntegrationCheckBox.IsChecked == true &&
              windowsIntegrationSnapshot.ManualContextMenu == WindowsIntegrationStatus.NeedsRepair) ||
             (SendToIntegrationCheckBox.IsChecked == true &&
              windowsIntegrationSnapshot.SendToShortcut == WindowsIntegrationStatus.NeedsRepair));

        if (hasSelectedRepair)
        {
            WindowsIntegrationInfoTextBlock.Text = text.IsRussian
                ? "Требуется обновление"
                : "Update required";
            WindowsIntegrationInfoTextBlock.Visibility = Visibility.Visible;
            return;
        }

        WindowsIntegrationInfoTextBlock.Text = string.Empty;
        WindowsIntegrationInfoTextBlock.Visibility = Visibility.Collapsed;
    }

    private void AdditionalPagesControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (hwndSource is not null)
        {
            return;
        }

        Window? ownerWindow = Window.GetWindow(this);
        hwndSource = ownerWindow is null
            ? null
            : PresentationSource.FromVisual(ownerWindow) as HwndSource;
        hwndSource?.AddHook(WindowMessageHook);
    }

    private void AdditionalPagesControl_OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (hwndSource is not null)
        {
            hwndSource.RemoveHook(WindowMessageHook);
            hwndSource = null;
        }
    }

    private IntPtr WindowMessageHook(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message != WmKeyDown ||
            currentPage != SettingsPage.Licenses ||
            !Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ||
            Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            return IntPtr.Zero;
        }

        int virtualKey = unchecked((int)wParam.ToInt64());
        int scanCode = unchecked((int)((lParam.ToInt64() >> 16) & 0xFF));
        if (virtualKey == 'A' || scanCode == PhysicalAKeyScanCode)
        {
            LicenseTextViewer.SelectAll();
            LicenseTextViewer.Focus();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void SwitchPage(SettingsPage page)
    {
        currentPage = page;
        SettingsPageGrid.Visibility = page == SettingsPage.Settings
            ? Visibility.Visible
            : Visibility.Collapsed;
        AboutPageGrid.Visibility = page == SettingsPage.About
            ? Visibility.Visible
            : Visibility.Collapsed;
        LicensePageGrid.Visibility = page == SettingsPage.Licenses
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (page == SettingsPage.Licenses)
        {
            LicenseTextViewer.ClearSelection();
            LicenseTextViewer.SetVerticalOffset(0.0);
            QueueLicenseTextRefresh();
            SyncLicenseScrollBar();
        }

        UpdatePageChrome();
    }

    private void UpdatePageChrome()
    {
        string title = currentPage switch
        {
            SettingsPage.About => text.IsRussian ? "О программе" : "About",
            SettingsPage.Licenses => text.IsRussian ? "Лицензии" : "Licenses",
            _ => text.IsRussian ? "Дополнительно" : "Advanced",
        };

        PageChromeChanged?.Invoke(title, text.IsRussian ? "Назад" : "Back");
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isApplyingUi)
        {
            return;
        }

        settingsDraft.Language = AppSettings.NormalizeLanguage(GetSelectedTag(LanguageComboBox));
        text = Localization.For(settingsDraft.Language);
        ApplyLocalizedText();
        ClearValidationStatus();
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isApplyingUi)
        {
            return;
        }

        settingsDraft.Theme = AppSettings.NormalizeTheme(GetSelectedTag(ThemeComboBox));
        ApplyTheme();
    }

    private void OnWindowsIntegrationCheckBoxClick(object sender, RoutedEventArgs e)
    {
        ClearValidationStatus();
        UpdateWindowsIntegrationInfo();
    }

    private void OnAboutButtonClick(object sender, RoutedEventArgs e)
    {
        SwitchPage(SettingsPage.About);
    }

    private void LicenseButton_OnClick(object sender, RoutedEventArgs e)
    {
        SwitchPage(SettingsPage.Licenses);
    }

    private void OnResetButtonClick(object sender, RoutedEventArgs e)
    {
        settingsDraft.Language = AppSettings.DefaultLanguage;
        settingsDraft.Theme = AppSettings.DefaultTheme;
        settingsDraft.JpegMode = AppSettings.DefaultJpegMode;
        settingsDraft.OnlineServiceCompatibility = AppSettings.DefaultOnlineServiceCompatibility;
        settingsDraft.SmartPaddingPercent = AppSettings.DefaultSmartPaddingPercent;
        settingsDraft.SmartPaddingMaxPx = AppSettings.DefaultSmartPaddingMaxPx;
        settingsDraft.AutoSizeStep = AppSettings.DefaultAutoSizeStep;

        ApplyDraftToUi();
        ApplyTheme();
        ClearValidationStatus();
    }

    private void OnSaveButtonClick(object sender, RoutedEventArgs e)
    {
        if (!TrySaveUiToDraft())
        {
            return;
        }

        if (!TryApplyWindowsIntegrationSelection())
        {
            return;
        }

        SettingsApplied?.Invoke(settingsDraft.Clone());
    }

    private bool TrySaveUiToDraft()
    {
        if (!AppSettings.TryParseDouble(SmartPaddingPercentTextBox.Text, out double smartPaddingPercent) ||
            smartPaddingPercent is < 0.0 or > 20.0)
        {
            ShowValidationError(SmartPaddingPercentTextBox, text.InvalidSmartPaddingPercentStatus);
            return false;
        }

        if (!int.TryParse(
                SmartPaddingMaxPxTextBox.Text.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int smartPaddingMaxPx) ||
            smartPaddingMaxPx is < 0 or > 300)
        {
            ShowValidationError(SmartPaddingMaxPxTextBox, text.InvalidSmartPaddingMaxPxStatus);
            return false;
        }

        settingsDraft.Language = AppSettings.NormalizeLanguage(GetSelectedTag(LanguageComboBox));
        settingsDraft.Theme = AppSettings.NormalizeTheme(GetSelectedTag(ThemeComboBox));
        settingsDraft.JpegMode = AppSettings.NormalizeJpegMode(ParseJpegMode(GetSelectedTag(JpegModeComboBox)));
        settingsDraft.AutoSizeStep = AppSettings.NormalizeAutoSizeStep(ParseAutoSizeStep(GetSelectedTag(AutoSizeStepComboBox)));
        settingsDraft.OnlineServiceCompatibility = OnlineServiceCompatibilityCheckBox.IsChecked == true;
        settingsDraft.SmartPaddingPercent = smartPaddingPercent;
        settingsDraft.SmartPaddingMaxPx = smartPaddingMaxPx;

        ClearValidationStatus();
        return true;
    }

    private void ShowValidationError(TextBox input, string message)
    {
        ShowValidationMessage(message);
        input.Focus();
        input.SelectAll();
    }

    private void ShowValidationMessage(string message)
    {
        ValidationStatusTextBlock.Text = message;
        ValidationStatusTextBlock.Visibility = Visibility.Visible;
        HoverTip.SetText(ValidationStatusTextBlock, message);
    }

    private void ClearValidationStatus()
    {
        if (ValidationStatusTextBlock is not null)
        {
            ValidationStatusTextBlock.Text = string.Empty;
            ValidationStatusTextBlock.Visibility = Visibility.Collapsed;
        }
    }

    private void OnValidationTextChanged(object sender, TextChangedEventArgs e)
    {
        ClearValidationStatus();
    }

    private static int ParseJpegMode(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int jpegMode)
            ? jpegMode
            : AppSettings.DefaultJpegMode;
    }


    private static int ParseAutoSizeStep(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int autoSizeStep)
            ? autoSizeStep
            : AppSettings.DefaultAutoSizeStep;
    }

    private static void SelectComboBoxItem(ComboBox comboBox, string tag)
    {
        foreach (object item in comboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem &&
                string.Equals(comboBoxItem.Tag as string, tag, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = comboBoxItem;
                return;
            }
        }

        if (comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private static string? GetSelectedTag(ComboBox comboBox)
    {
        return comboBox.SelectedItem is ComboBoxItem comboBoxItem
            ? comboBoxItem.Tag as string
            : null;
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

    private void OnSmartPaddingPercentPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!AppSettings.TryParseDouble(SmartPaddingPercentTextBox.Text, out double value))
        {
            value = settingsDraft.SmartPaddingPercent;
        }

        value += GetWheelDelta(e);
        value = AppSettings.NormalizeSmartPaddingPercent(value);

        SmartPaddingPercentTextBox.Text = AppSettings.FormatDouble(value);
        SmartPaddingPercentTextBox.CaretIndex = SmartPaddingPercentTextBox.Text.Length;

        e.Handled = true;
    }

    private void OnSmartPaddingMaxPxPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        int value = ParseIntegerTextBoxValue(SmartPaddingMaxPxTextBox, settingsDraft.SmartPaddingMaxPx);
        value += GetWheelDelta(e);
        value = AppSettings.NormalizeSmartPaddingMaxPx(value);

        SmartPaddingMaxPxTextBox.Text = value.ToString(CultureInfo.InvariantCulture);
        SmartPaddingMaxPxTextBox.CaretIndex = SmartPaddingMaxPxTextBox.Text.Length;

        e.Handled = true;
    }

    private static int ParseIntegerTextBoxValue(TextBox textBox, int fallback)
    {
        return int.TryParse(textBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : fallback;
    }

    private static int GetWheelDelta(MouseWheelEventArgs e)
    {
        int step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
        return e.Delta > 0 ? step : -step;
    }

    private void OnIntegerPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsAllDigits(e.Text);
    }

    private void OnDecimalPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsDecimalTextAllowed(sender as TextBox, e.Text);
    }

    private void OnNumberPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
    }

    private void OnIntegerPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        string textValue = (string)e.DataObject.GetData(DataFormats.Text);
        if (!IsAllDigits(textValue))
        {
            e.CancelCommand();
        }
    }

    private void OnDecimalPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        string textValue = (string)e.DataObject.GetData(DataFormats.Text);
        if (!IsDecimalTextAllowed(sender as TextBox, textValue))
        {
            e.CancelCommand();
        }
    }

    private static bool IsAllDigits(string value)
    {
        return !string.IsNullOrEmpty(value) && value.All(char.IsDigit);
    }

    private static bool IsDecimalTextAllowed(TextBox? textBox, string input)
    {
        if (textBox is null || string.IsNullOrEmpty(input))
        {
            return false;
        }

        string separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        if (separator != ".")
        {
            input = input.Replace(separator, ".");
        }

        string current = textBox.Text ?? string.Empty;
        int selectionStart = textBox.SelectionStart;
        int selectionLength = textBox.SelectionLength;
        string candidate = current.Remove(selectionStart, selectionLength).Insert(selectionStart, input);

        if (candidate.Count(ch => ch == '.') > 1)
        {
            return false;
        }

        return candidate.All(ch => char.IsDigit(ch) || ch == '.');
    }
    private void LicenseTextViewer_OnScrollOffsetChanged(object? sender, EventArgs e)
    {
        SyncLicenseScrollBar();
    }

    private void LicensePageGrid_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (LicenseTextViewer.ScrollByMouseWheelDelta(e.Delta))
        {
            e.Handled = true;
        }
    }

    private void LicensePageGrid_OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        LicenseContextMenu.PlacementTarget = LicensePageGrid;
        LicenseContextMenu.IsOpen = true;
        e.Handled = true;
    }

    private void LicenseScrollBar_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isLicenseScrollBarSyncing)
        {
            return;
        }

        LicenseTextViewer.SetVerticalOffset(e.NewValue);
    }

    private void SyncLicenseScrollBar()
    {
        double maximum = Math.Max(0.0, LicenseTextViewer.ScrollableHeight);
        double value = Math.Clamp(LicenseTextViewer.VerticalOffset, 0.0, maximum);

        isLicenseScrollBarSyncing = true;
        try
        {
            LicenseScrollBar.Minimum = 0.0;
            LicenseScrollBar.Maximum = maximum;
            LicenseScrollBar.ViewportSize = Math.Max(0.0, LicenseTextViewer.ViewportHeight);
            LicenseScrollBar.SmallChange = 34.0;
            LicenseScrollBar.LargeChange = Math.Max(24.0, LicenseTextViewer.ViewportHeight * 0.82);
            LicenseScrollBar.Value = value;
            LicenseScrollBar.Visibility = maximum > 0.0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        finally
        {
            isLicenseScrollBarSyncing = false;
        }
    }

    private void LicenseContextMenu_OnOpened(object sender, RoutedEventArgs e)
    {
        LicenseCopyMenuItem.IsEnabled = LicenseTextViewer.SelectionLength > 0;
    }

    private void LicenseCopyMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        LicenseTextViewer.Copy();
    }

    private void LicenseSelectAllMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        LicenseTextViewer.SelectAll();
        LicenseTextViewer.Focus();
    }

    private void LicenseTextViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (currentPage == SettingsPage.Licenses)
        {
            QueueLicenseTextRefresh();
            SyncLicenseScrollBar();
        }
    }

    private void QueueLicenseTextRefresh()
    {
        if (licenseTextRefreshQueued)
        {
            return;
        }

        licenseTextRefreshQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            licenseTextRefreshQueued = false;
            RefreshLicenseTextForViewport();
        }));
    }

    private void RefreshLicenseTextForViewport()
    {
        if (currentPage != SettingsPage.Licenses || !LicenseTextViewer.IsLoaded)
        {
            return;
        }

        double viewportWidth = LicenseTextViewer.ActualWidth;
        if (viewportWidth <= 0.0 ||
            (!double.IsNaN(licenseSeparatorLayoutWidth) &&
             Math.Abs(viewportWidth - licenseSeparatorLayoutWidth) < 0.1 &&
             LicenseTextViewer.ExtentHeight > 0.0))
        {
            SyncLicenseScrollBar();
            return;
        }

        int separatorLength = CalculateLicenseSeparatorLength(viewportWidth);
        licenseSeparatorLayoutWidth = viewportWidth;
        if (licenseSeparatorLength == separatorLength && LicenseTextViewer.ExtentHeight > 0.0)
        {
            SyncLicenseScrollBar();
            return;
        }

        licenseSeparatorLength = separatorLength;
        LicenseTextViewer.SetText(BuildLicenseText(separatorLength));
        LicenseTextViewer.ClearSelection();
        LicenseTextViewer.SetVerticalOffset(0.0);
        SyncLicenseScrollBar();
    }

    private int CalculateLicenseSeparatorLength(double viewportWidth)
    {
        double availableWidth = Math.Max(1.0, viewportWidth - LicenseSeparatorSafetyMargin);
        var typeface = new Typeface(
            LicenseTextViewer.FontFamily,
            LicenseTextViewer.FontStyle,
            LicenseTextViewer.FontWeight,
            LicenseTextViewer.FontStretch);
        double pixelsPerDip = VisualTreeHelper.GetDpi(LicenseTextViewer).PixelsPerDip;

        bool Fits(int count)
        {
            var formattedText = new FormattedText(
                new string('═', Math.Max(1, count)),
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                LicenseTextViewer.FontSize,
                Brushes.Black,
                null,
                TextFormattingMode.Display,
                pixelsPerDip);
            return formattedText.WidthIncludingTrailingWhitespace <= availableWidth;
        }

        int low = 1;
        int high = 256;
        int best = 1;
        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            if (Fits(mid))
            {
                best = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return best;
    }

    private string BuildLicenseText(int separatorLength)
    {
        var builder = new StringBuilder();
        AppendLicenseSection(builder, "LICENSE", ReadResourceText("LICENSE"), separatorLength);

        string noticesPath = text.IsRussian
            ? "Licenses/THIRD_PARTY_NOTICES-RU.md"
            : "Licenses/THIRD_PARTY_NOTICES-EN.md";
        AppendLicenseSection(builder, noticesPath, ReadResourceText(noticesPath), separatorLength);

        foreach (string resourcePath in LicenseResourcePaths)
        {
            AppendLicenseSection(builder, resourcePath, ReadResourceText(resourcePath), separatorLength);
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendLicenseSection(
        StringBuilder builder,
        string title,
        string content,
        int separatorLength)
    {
        if (builder.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine();
        }

        string separator = new('═', Math.Max(1, separatorLength));
        builder.AppendLine(separator);
        builder.AppendLine(title);
        builder.AppendLine(separator);
        builder.AppendLine();
        builder.Append(content.TrimEnd());
    }

    private string ReadResourceText(string resourcePath)
    {
        var resource = Application.GetResourceStream(new Uri(resourcePath, UriKind.Relative));
        if (resource?.Stream is null)
        {
            return text.IsRussian
                ? $"Не удалось прочитать встроенный ресурс: {resourcePath}"
                : $"Failed to read embedded resource: {resourcePath}";
        }

        using var reader = new StreamReader(
            resource.Stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: false);
        return reader.ReadToEnd();
    }

    private static string? GetBuildDate()
    {
        foreach (var attribute in Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attribute.Key == "BuildDate")
            {
                return attribute.Value;
            }
        }

        return null;
    }

}
