using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ImageSquareResizer;

internal partial class LicenseWindow : Window
{
    private const double DarkBorderMixRatio = 0.03;
    private const double LightBorderMixRatio = 0.06;
    private static readonly string[] LicenseResourcePaths =
    {
        "Licenses/Magick.NET-Apache-2.0.txt",
        "Licenses/ImageMagick-License.txt",
        "Licenses/SharpVectors-BSD-3-Clause.txt",
        "Licenses/Lucide-ISC-and-Feather-MIT.txt"
    };

    private readonly Localization localization;

    public LicenseWindow(AppSettings settings)
    {
        localization = Localization.For(settings.Language);
        InitializeComponent();
        ApplyTheme(settings);
        ApplyLocalizedText();
        LicenseTextBox.Text = BuildLicenseText();
    }

    private void ApplyLocalizedText()
    {
        string title = localization.IsRussian
            ? "Лицензия и сторонние компоненты"
            : "License and third-party components";

        Title = title;
        TitleTextBlock.Text = title;
        HoverTip.SetText(CloseButton, localization.IsRussian ? "Закрыть" : "Close");
    }

    private string BuildLicenseText()
    {
        var builder = new StringBuilder();
        AppendSection(
            builder,
            "LICENSE",
            ReadResourceText("LICENSE"));

        string noticesPath = localization.IsRussian
            ? "Licenses/THIRD_PARTY_NOTICES-RU.md"
            : "Licenses/THIRD_PARTY_NOTICES-EN.md";
        AppendSection(builder, noticesPath, ReadResourceText(noticesPath));

        foreach (string resourcePath in LicenseResourcePaths)
        {
            AppendSection(builder, resourcePath, ReadResourceText(resourcePath));
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendSection(StringBuilder builder, string title, string text)
    {
        if (builder.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine();
        }

        builder.AppendLine(new string('=', 68));
        builder.AppendLine(title);
        builder.AppendLine(new string('=', 68));
        builder.AppendLine();
        builder.Append(text.TrimEnd());
    }

    private string ReadResourceText(string resourcePath)
    {
        var resource = Application.GetResourceStream(new Uri(resourcePath, UriKind.Relative));
        if (resource?.Stream is null)
        {
            return localization.IsRussian
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
        var inputBackground = Color.FromRgb(249, 250, 251);

        Resources["WindowBackgroundBrush"] = BrushFromColor(windowBackground);
        Resources["InputBackgroundBrush"] = BrushFromColor(inputBackground);
        Resources["MainTextBrush"] = BrushFromRgb(31, 41, 55);
        Resources["SecondaryTextBrush"] = BrushFromRgb(107, 114, 128);
        Resources["WindowBorderBrush"] = BrushFromColor(MixColor(windowBackground, Colors.Black, LightBorderMixRatio));
        Resources["ButtonBorderBrush"] = BrushFromColor(MixColor(inputBackground, Colors.Black, 0.08));
        Resources["AccentBrush"] = BrushFromRgb(0, 120, 215);
        Resources["TitleButtonForegroundBrush"] = BrushFromRgb(75, 85, 99);
        Resources["SoftButtonHoverBackgroundBrush"] = BrushFromRgb(234, 243, 255);
        Resources["SoftButtonPressedBackgroundBrush"] = BrushFromRgb(215, 234, 254);
        Resources["ScrollThumbBrush"] = BrushFromRgb(176, 181, 188);
        Resources["ScrollThumbHoverBrush"] = BrushFromRgb(139, 146, 156);
        Resources["ScrollThumbPressedBrush"] = BrushFromRgb(107, 114, 128);
    }

    private void ApplyDarkTheme()
    {
        var windowBackground = Color.FromRgb(32, 32, 32);
        var inputBackground = Color.FromRgb(37, 37, 38);

        Resources["WindowBackgroundBrush"] = BrushFromColor(windowBackground);
        Resources["InputBackgroundBrush"] = BrushFromColor(inputBackground);
        Resources["MainTextBrush"] = BrushFromRgb(212, 212, 212);
        Resources["SecondaryTextBrush"] = BrushFromRgb(170, 170, 170);
        Resources["WindowBorderBrush"] = BrushFromColor(MixColor(windowBackground, Colors.White, DarkBorderMixRatio));
        Resources["ButtonBorderBrush"] = BrushFromColor(MixColor(inputBackground, Colors.White, 0.06));
        Resources["AccentBrush"] = BrushFromRgb(0, 122, 204);
        Resources["TitleButtonForegroundBrush"] = BrushFromRgb(212, 212, 212);
        Resources["SoftButtonHoverBackgroundBrush"] = BrushFromRgb(51, 51, 51);
        Resources["SoftButtonPressedBackgroundBrush"] = BrushFromRgb(62, 62, 66);
        Resources["ScrollThumbBrush"] = BrushFromRgb(104, 104, 104);
        Resources["ScrollThumbHoverBrush"] = BrushFromRgb(133, 133, 133);
        Resources["ScrollThumbPressedBrush"] = BrushFromRgb(160, 160, 160);
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

    private void Window_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        e.Handled = true;
        Close();
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
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
