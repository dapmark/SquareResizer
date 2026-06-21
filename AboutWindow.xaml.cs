using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ImageSquareResizer;

internal partial class AboutWindow : Window
{
    private const double DarkBorderMixRatio = 0.03;
    private const double LightBorderMixRatio = 0.06;

    public AboutWindow(AppSettings settings)
    {
        InitializeComponent();

        ApplyTheme(settings);
        ApplyLocalizedText(settings);
    }

    private void ApplyLocalizedText(AppSettings settings)
    {
        var text = Localization.For(settings.Language);
        Title = text.IsRussian ? "О программе" : "About";
        VersionTextBlock.Text = text.IsRussian
            ? $"Версия: {AppVersion.Current}"
            : $"Version: {AppVersion.Current}";
    }

    private void ApplyTheme(AppSettings settings)
    {
        if (settings.IsDarkTheme)
        {
            ApplyDarkTheme();
            return;
        }

        ApplyLightTheme();
    }

    private void ApplyLightTheme()
    {
        var windowBackground = Color.FromRgb(255, 255, 255);

        Resources["WindowBackgroundBrush"] = BrushFromColor(windowBackground);
        Resources["MainTextBrush"] = BrushFromRgb(31, 41, 55);
        Resources["SecondaryTextBrush"] = BrushFromRgb(107, 114, 128);
        Resources["WindowBorderBrush"] = BrushFromColor(MixColor(windowBackground, Colors.Black, LightBorderMixRatio));
        Resources["TitleButtonForegroundBrush"] = BrushFromRgb(75, 85, 99);
        Resources["SoftButtonHoverBackgroundBrush"] = BrushFromRgb(234, 243, 255);
        Resources["SoftButtonPressedBackgroundBrush"] = BrushFromRgb(215, 234, 254);
    }

    private void ApplyDarkTheme()
    {
        var windowBackground = Color.FromRgb(32, 32, 32);

        Resources["WindowBackgroundBrush"] = BrushFromColor(windowBackground);
        Resources["MainTextBrush"] = BrushFromRgb(212, 212, 212);
        Resources["SecondaryTextBrush"] = BrushFromRgb(170, 170, 170);
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

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RootGrid_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ClickCount == 2)
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
}
