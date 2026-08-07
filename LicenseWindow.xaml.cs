using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ImageSquareResizer;

internal partial class LicenseWindow : Window
{
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
        ThemeResources.ApplyLicense(Resources, settings.IsDarkTheme);
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
