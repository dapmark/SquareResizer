using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Effects;

namespace ImageSquareResizer;

internal partial class AboutWindow : Window
{
    private readonly AppSettings settings;

    public AboutWindow(AppSettings settings)
    {
        this.settings = settings.Clone();
        InitializeComponent();

        ThemeResources.ApplyAbout(Resources, this.settings.IsDarkTheme);
        ApplyLocalizedText(this.settings);
    }

    private void ApplyLocalizedText(AppSettings currentSettings)
    {
        var text = Localization.For(currentSettings.Language);
        Title = text.IsRussian ? "О программе" : "About";
        HoverTip.SetText(CloseButton, text.IsRussian ? "Закрыть" : "Close");
        VersionTextBlock.Text = text.IsRussian
            ? $"Версия: {AppVersion.Current}"
            : $"Version: {AppVersion.Current}";
        LicenseLinkRun.Text = text.IsRussian
            ? "Лицензия и сторонние компоненты"
            : "License and third-party components";
    }

    private void LicenseLink_OnClick(object sender, RoutedEventArgs e)
    {
        AboutContentRoot.Effect = new BlurEffect
        {
            Radius = 4,
            RenderingBias = RenderingBias.Performance
        };

        try
        {
            var window = new LicenseWindow(settings)
            {
                Owner = this
            };

            window.ShowDialog();
        }
        finally
        {
            AboutContentRoot.Effect = null;
        }
    }

    private static bool IsInsideHyperlink(object? source)
    {
        if (source is Hyperlink)
        {
            return true;
        }

        if (source is not FrameworkContentElement contentElement)
        {
            return false;
        }

        DependencyObject? current = contentElement;
        while (current is FrameworkContentElement currentContent)
        {
            if (current is Hyperlink)
            {
                return true;
            }

            current = currentContent.Parent;
        }

        return false;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RootGrid_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || IsInsideHyperlink(e.OriginalSource))
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
