using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ImageSquareResizer;

internal partial class SettingsWindow : Window
{
    private const double DarkBorderMixRatio = 0.03;
    private const double LightBorderMixRatio = 0.06;

    private AppSettings settingsDraft;
    private Localization text;
    private bool isApplyingUi;


    public AppSettings Settings { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        Settings = settings.Clone();
        settingsDraft = settings.Clone();
        text = Localization.For(settingsDraft.Language);

        InitializeComponent();

        DataObject.AddPastingHandler(SmartPaddingMaxPxTextBox, OnIntegerPaste);
        DataObject.AddPastingHandler(SmartPaddingPercentTextBox, OnDecimalPaste);

        ApplyDraftToUi();
        ApplyTheme();
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
        var title = text.IsRussian ? "Настройки" : "Settings";
        Title = title;
        TitleTextBlock.Text = title;

        InterfaceSectionTextBlock.Text = text.IsRussian ? "Интерфейс" : "Interface";
        LanguageLabel.Text = text.IsRussian ? "Язык" : "Language";
        ThemeLabel.Text = text.IsRussian ? "Тема" : "Theme";
        AdvancedSectionTextBlock.Text = text.IsRussian ? "Дополнительно" : "Advanced";
        JpegModeLabel.Text = text.IsRussian ? "JPEG режим" : "JPEG mode";
        SmartPaddingSectionTextBlock.Text = text.IsRussian ? "Умная дорисовка" : "Smart padding";
        SmartPaddingPercentLabel.Text = text.IsRussian ? "Макс. разница" : "Max difference";
        SmartPaddingMaxPxLabel.Text = text.IsRussian ? "Лимит дорисовки" : "Padding limit";
        ResetButton.Content = text.IsRussian ? "Сброс" : "Reset";
        CancelButton.Content = text.IsRussian ? "Отмена" : "Cancel";
        SaveButton.Content = text.IsRussian ? "Применить" : "Apply";

        SetComboBoxItemContent(LanguageComboBox, "en", "English");
        SetComboBoxItemContent(LanguageComboBox, "ru", "Русский");
        SetComboBoxItemContent(ThemeComboBox, "dark", text.IsRussian ? "Тёмная" : "Dark");
        SetComboBoxItemContent(ThemeComboBox, "light", text.IsRussian ? "Светлая" : "Light");
        SetComboBoxItemContent(JpegModeComboBox, "1", text.IsRussian ? "Компактный" : "Compact");
        SetComboBoxItemContent(JpegModeComboBox, "2", text.IsRussian ? "Сбалансированный" : "Balanced");
        SetComboBoxItemContent(JpegModeComboBox, "3", text.IsRussian ? "Максимальный" : "Maximum");
    }

    private void ApplyTheme()
    {
        if (settingsDraft.IsDarkTheme)
        {
            ApplyDarkTheme();
            return;
        }

        ApplyLightTheme();
    }

    private void ApplyLightTheme()
    {
        var windowBackground = Color.FromRgb(243, 243, 243);
        var panelBackground = Color.FromRgb(255, 255, 255);

        Resources["WindowBackgroundBrush"] = BrushFromColor(windowBackground);
        Resources["PanelBackgroundBrush"] = BrushFromColor(panelBackground);
        Resources["MainTextBrush"] = BrushFromRgb(32, 32, 32);
        Resources["SecondaryTextBrush"] = BrushFromRgb(55, 55, 55);
        Resources["ButtonBackgroundBrush"] = BrushFromRgb(250, 250, 250);
        Resources["ButtonHoverBackgroundBrush"] = BrushFromRgb(245, 249, 255);
        Resources["ButtonPressedBackgroundBrush"] = BrushFromRgb(230, 242, 255);
        Resources["ButtonBorderBrush"] = BrushFromColor(MixColor(panelBackground, Colors.Black, LightBorderMixRatio));
        Resources["AccentBorderBrush"] = BrushFromRgb(0, 120, 215);
        Resources["InputBackgroundBrush"] = BrushFromRgb(255, 255, 255);
        Resources["WindowBorderBrush"] = BrushFromColor(MixColor(windowBackground, Colors.Black, LightBorderMixRatio));
        Resources["TitleButtonForegroundBrush"] = BrushFromRgb(75, 85, 99);
        Resources["SoftButtonHoverBackgroundBrush"] = BrushFromRgb(234, 243, 255);
        Resources["SoftButtonPressedBackgroundBrush"] = BrushFromRgb(215, 234, 254);
    }

    private void ApplyDarkTheme()
    {
        var windowBackground = Color.FromRgb(32, 32, 32);
        var panelBackground = Color.FromRgb(37, 37, 38);

        Resources["WindowBackgroundBrush"] = BrushFromColor(windowBackground);
        Resources["PanelBackgroundBrush"] = BrushFromColor(panelBackground);
        Resources["MainTextBrush"] = BrushFromRgb(212, 212, 212);
        Resources["SecondaryTextBrush"] = BrushFromRgb(212, 212, 212);
        Resources["ButtonBackgroundBrush"] = BrushFromRgb(45, 45, 48);
        Resources["ButtonHoverBackgroundBrush"] = BrushFromRgb(62, 62, 66);
        Resources["ButtonPressedBackgroundBrush"] = BrushFromRgb(0, 122, 204);
        Resources["ButtonBorderBrush"] = BrushFromColor(MixColor(panelBackground, Colors.White, DarkBorderMixRatio));
        Resources["AccentBorderBrush"] = BrushFromRgb(0, 122, 204);
        Resources["InputBackgroundBrush"] = BrushFromRgb(32, 32, 32);
        Resources["WindowBorderBrush"] = BrushFromColor(MixColor(windowBackground, Colors.White, DarkBorderMixRatio));
        Resources["TitleButtonForegroundBrush"] = BrushFromRgb(212, 212, 212);
        Resources["SoftButtonHoverBackgroundBrush"] = BrushFromRgb(51, 51, 51);
        Resources["SoftButtonPressedBackgroundBrush"] = BrushFromRgb(62, 62, 66);
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

    private static Color MixColor(Color source, Color target, double ratio)
    {
        ratio = Math.Clamp(ratio, 0.0, 1.0);
        byte red = (byte)Math.Round(source.R + (target.R - source.R) * ratio);
        byte green = (byte)Math.Round(source.G + (target.G - source.G) * ratio);
        byte blue = (byte)Math.Round(source.B + (target.B - source.B) * ratio);
        return Color.FromRgb(red, green, blue);
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
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

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
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

    private void OnResetButtonClick(object sender, RoutedEventArgs e)
    {
        settingsDraft.Language = AppSettings.DefaultLanguage;
        settingsDraft.Theme = AppSettings.DefaultTheme;
        settingsDraft.JpegMode = AppSettings.DefaultJpegMode;
        settingsDraft.SmartPaddingPercent = AppSettings.DefaultSmartPaddingPercent;
        settingsDraft.SmartPaddingMaxPx = AppSettings.DefaultSmartPaddingMaxPx;

        ApplyDraftToUi();
        ApplyTheme();
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnSaveButtonClick(object sender, RoutedEventArgs e)
    {
        if (!TrySaveUiToDraft())
        {
            return;
        }

        Settings = settingsDraft.Clone();
        Settings.Save();
        DialogResult = true;
    }

    private bool TrySaveUiToDraft()
    {
        if (!AppSettings.TryParseDouble(SmartPaddingPercentTextBox.Text, out double smartPaddingPercent))
        {
            ShowInvalidValue(text.IsRussian ? "Введите максимальную разницу сторон от 0 до 20 %." : "Enter a max side difference from 0 to 20%.");
            SmartPaddingPercentTextBox.Focus();
            return false;
        }

        if (!int.TryParse(SmartPaddingMaxPxTextBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int smartPaddingMaxPx))
        {
            ShowInvalidValue(text.IsRussian ? "Введите лимит дорисовки от 0 до 300 px." : "Enter a padding limit from 0 to 300 px.");
            SmartPaddingMaxPxTextBox.Focus();
            return false;
        }

        settingsDraft.Language = AppSettings.NormalizeLanguage(GetSelectedTag(LanguageComboBox));
        settingsDraft.Theme = AppSettings.NormalizeTheme(GetSelectedTag(ThemeComboBox));
        settingsDraft.JpegMode = AppSettings.NormalizeJpegMode(ParseJpegMode(GetSelectedTag(JpegModeComboBox)));
        settingsDraft.SmartPaddingPercent = AppSettings.NormalizeSmartPaddingPercent(smartPaddingPercent);
        settingsDraft.SmartPaddingMaxPx = AppSettings.NormalizeSmartPaddingMaxPx(smartPaddingMaxPx);

        return true;
    }

    private void ShowInvalidValue(string message)
    {
        MessageBox.Show(this, message, text.InvalidValueTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static int ParseJpegMode(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int jpegMode)
            ? jpegMode
            : AppSettings.DefaultJpegMode;
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
}
