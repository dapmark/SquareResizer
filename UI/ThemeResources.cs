using System;
using System.Windows;
using System.Windows.Media;

namespace ImageSquareResizer;

internal static class ThemeResources
{
    private const double DarkBorderMixRatio = 0.03;
    private const double LightBorderMixRatio = 0.06;

    public static void ApplyMain(ResourceDictionary resources, bool isDark)
    {
        if (isDark)
        {
            var windowBackground = Color.FromRgb(32, 32, 32);
            var dropAreaBackground = Color.FromRgb(37, 37, 38);

            SetBrush(resources, "WindowBackgroundBrush", windowBackground);
            SetBrush(resources, "WindowBorderBrush", MixColor(windowBackground, Colors.White, DarkBorderMixRatio));
            SetBrush(resources, "DropAreaBackgroundBrush", dropAreaBackground);
            SetBrush(resources, "DropAreaBorderBrush", MixColor(dropAreaBackground, Colors.White, DarkBorderMixRatio));
            SetBrush(resources, "DropAreaDashedBorderBrush", MixColor(dropAreaBackground, Colors.White, 0.18));
            SetBrush(resources, "MainTextBrush", Color.FromRgb(212, 212, 212));
            SetBrush(resources, "SecondaryTextBrush", Color.FromRgb(212, 212, 212));
            SetBrush(resources, "ButtonBackgroundBrush", Color.FromRgb(45, 45, 48));
            SetBrush(resources, "ButtonHoverBackgroundBrush", Color.FromRgb(62, 62, 66));
            SetBrush(resources, "ButtonPressedBackgroundBrush", Color.FromRgb(0, 122, 204));
            SetBrush(resources, "ButtonBorderBrush", MixColor(dropAreaBackground, Colors.White, DarkBorderMixRatio));
            SetBrush(resources, "AccentBorderBrush", Color.FromRgb(0, 122, 204));
            SetBrush(resources, "InputBackgroundBrush", Color.FromRgb(32, 32, 32));
            SetBrush(resources, "StatusTextBrush", Color.FromRgb(200, 200, 200));
            SetBrush(resources, "StatusErrorTextBrush", Color.FromRgb(241, 112, 123));
            SetBrush(resources, "TitleButtonForegroundBrush", Color.FromRgb(212, 212, 212));
            SetBrush(resources, "SoftButtonHoverBackgroundBrush", Color.FromRgb(51, 51, 51));
            SetBrush(resources, "SoftButtonPressedBackgroundBrush", Color.FromRgb(62, 62, 66));
            return;
        }

        var windowBackgroundLight = Color.FromRgb(243, 243, 243);
        var dropAreaBackgroundLight = Color.FromRgb(245, 245, 245);

        SetBrush(resources, "WindowBackgroundBrush", windowBackgroundLight);
        SetBrush(resources, "WindowBorderBrush", MixColor(windowBackgroundLight, Colors.Black, LightBorderMixRatio));
        SetBrush(resources, "DropAreaBackgroundBrush", dropAreaBackgroundLight);
        SetBrush(resources, "DropAreaBorderBrush", MixColor(dropAreaBackgroundLight, Colors.Black, LightBorderMixRatio));
        SetBrush(resources, "DropAreaDashedBorderBrush", MixColor(dropAreaBackgroundLight, Colors.Black, 0.16));
        SetBrush(resources, "MainTextBrush", Color.FromRgb(70, 70, 70));
        SetBrush(resources, "SecondaryTextBrush", Color.FromRgb(50, 50, 50));
        SetBrush(resources, "ButtonBackgroundBrush", Color.FromRgb(250, 250, 250));
        SetBrush(resources, "ButtonHoverBackgroundBrush", Color.FromRgb(234, 244, 255));
        SetBrush(resources, "ButtonPressedBackgroundBrush", Color.FromRgb(207, 230, 255));
        SetBrush(resources, "ButtonBorderBrush", MixColor(dropAreaBackgroundLight, Colors.Black, LightBorderMixRatio));
        SetBrush(resources, "AccentBorderBrush", Color.FromRgb(0, 120, 215));
        SetBrush(resources, "InputBackgroundBrush", Color.FromRgb(255, 255, 255));
        SetBrush(resources, "StatusTextBrush", Color.FromRgb(80, 80, 80));
        SetBrush(resources, "StatusErrorTextBrush", Color.FromRgb(196, 43, 28));
        SetBrush(resources, "TitleButtonForegroundBrush", Color.FromRgb(75, 85, 99));
        SetBrush(resources, "SoftButtonHoverBackgroundBrush", Color.FromRgb(226, 240, 255));
        SetBrush(resources, "SoftButtonPressedBackgroundBrush", Color.FromRgb(203, 227, 255));
    }

    public static void ApplySettings(ResourceDictionary resources, bool isDark)
    {
        if (isDark)
        {
            var windowBackground = Color.FromRgb(32, 32, 32);
            var panelBackground = Color.FromRgb(37, 37, 38);

            SetBrush(resources, "WindowBackgroundBrush", windowBackground);
            SetBrush(resources, "PanelBackgroundBrush", panelBackground);
            SetBrush(resources, "MainTextBrush", Color.FromRgb(212, 212, 212));
            SetBrush(resources, "SecondaryTextBrush", Color.FromRgb(212, 212, 212));
            SetBrush(resources, "ButtonBackgroundBrush", Color.FromRgb(45, 45, 48));
            SetBrush(resources, "ButtonHoverBackgroundBrush", Color.FromRgb(62, 62, 66));
            SetBrush(resources, "ButtonPressedBackgroundBrush", Color.FromRgb(0, 122, 204));
            SetBrush(resources, "ButtonBorderBrush", MixColor(panelBackground, Colors.White, DarkBorderMixRatio));
            SetBrush(resources, "AccentBorderBrush", Color.FromRgb(0, 122, 204));
            SetBrush(resources, "ApplyButtonBackgroundBrush", Color.FromRgb(6, 50, 77));
            SetBrush(resources, "ApplyButtonHoverBackgroundBrush", Color.FromRgb(9, 71, 113));
            SetBrush(resources, "InputBackgroundBrush", Color.FromRgb(32, 32, 32));
            SetBrush(resources, "WindowBorderBrush", MixColor(windowBackground, Colors.White, DarkBorderMixRatio));
            SetBrush(resources, "TitleButtonForegroundBrush", Color.FromRgb(212, 212, 212));
            SetBrush(resources, "SoftButtonHoverBackgroundBrush", Color.FromRgb(51, 51, 51));
            SetBrush(resources, "SoftButtonPressedBackgroundBrush", Color.FromRgb(62, 62, 66));
            SetBrush(resources, "ScrollThumbBrush", Color.FromRgb(104, 104, 104));
            SetBrush(resources, "ScrollThumbHoverBrush", Color.FromRgb(133, 133, 133));
            SetBrush(resources, "ScrollThumbPressedBrush", Color.FromRgb(160, 160, 160));
            SetBrush(resources, "StatusErrorTextBrush", Color.FromRgb(241, 112, 123));
            return;
        }

        var windowBackgroundLight = Color.FromRgb(243, 243, 243);
        var panelBackgroundLight = Color.FromRgb(255, 255, 255);

        SetBrush(resources, "WindowBackgroundBrush", windowBackgroundLight);
        SetBrush(resources, "PanelBackgroundBrush", panelBackgroundLight);
        SetBrush(resources, "MainTextBrush", Color.FromRgb(32, 32, 32));
        SetBrush(resources, "SecondaryTextBrush", Color.FromRgb(55, 55, 55));
        SetBrush(resources, "ButtonBackgroundBrush", Color.FromRgb(250, 250, 250));
        SetBrush(resources, "ButtonHoverBackgroundBrush", Color.FromRgb(234, 244, 255));
        SetBrush(resources, "ButtonPressedBackgroundBrush", Color.FromRgb(207, 230, 255));
        SetBrush(resources, "ButtonBorderBrush", MixColor(panelBackgroundLight, Colors.Black, LightBorderMixRatio));
        SetBrush(resources, "AccentBorderBrush", Color.FromRgb(0, 120, 215));
        SetBrush(resources, "ApplyButtonBackgroundBrush", Color.FromRgb(215, 235, 255));
        SetBrush(resources, "ApplyButtonHoverBackgroundBrush", Color.FromRgb(197, 225, 255));
        SetBrush(resources, "InputBackgroundBrush", Color.FromRgb(255, 255, 255));
        SetBrush(resources, "WindowBorderBrush", MixColor(windowBackgroundLight, Colors.Black, LightBorderMixRatio));
        SetBrush(resources, "TitleButtonForegroundBrush", Color.FromRgb(75, 85, 99));
        SetBrush(resources, "SoftButtonHoverBackgroundBrush", Color.FromRgb(226, 240, 255));
        SetBrush(resources, "SoftButtonPressedBackgroundBrush", Color.FromRgb(203, 227, 255));
        SetBrush(resources, "ScrollThumbBrush", Color.FromRgb(176, 181, 188));
        SetBrush(resources, "ScrollThumbHoverBrush", Color.FromRgb(139, 146, 156));
        SetBrush(resources, "ScrollThumbPressedBrush", Color.FromRgb(107, 114, 128));
        SetBrush(resources, "StatusErrorTextBrush", Color.FromRgb(196, 43, 28));
    }

    public static void ApplyAbout(ResourceDictionary resources, bool isDark)
    {
        ApplyAuxiliary(resources, isDark);
    }

    public static void ApplyLicense(ResourceDictionary resources, bool isDark)
    {
        ApplyAuxiliary(resources, isDark);

        if (isDark)
        {
            var inputBackground = Color.FromRgb(37, 37, 38);

            SetBrush(resources, "InputBackgroundBrush", inputBackground);
            SetBrush(resources, "ButtonBorderBrush", MixColor(inputBackground, Colors.White, 0.06));
            SetBrush(resources, "ScrollThumbBrush", Color.FromRgb(104, 104, 104));
            SetBrush(resources, "ScrollThumbHoverBrush", Color.FromRgb(133, 133, 133));
            SetBrush(resources, "ScrollThumbPressedBrush", Color.FromRgb(160, 160, 160));
            return;
        }

        var inputBackgroundLight = Color.FromRgb(249, 250, 251);

        SetBrush(resources, "InputBackgroundBrush", inputBackgroundLight);
        SetBrush(resources, "ButtonBorderBrush", MixColor(inputBackgroundLight, Colors.Black, 0.08));
        SetBrush(resources, "ScrollThumbBrush", Color.FromRgb(176, 181, 188));
        SetBrush(resources, "ScrollThumbHoverBrush", Color.FromRgb(139, 146, 156));
        SetBrush(resources, "ScrollThumbPressedBrush", Color.FromRgb(107, 114, 128));
    }

    private static void ApplyAuxiliary(ResourceDictionary resources, bool isDark)
    {
        if (isDark)
        {
            var windowBackground = Color.FromRgb(32, 32, 32);

            SetBrush(resources, "WindowBackgroundBrush", windowBackground);
            SetBrush(resources, "MainTextBrush", Color.FromRgb(212, 212, 212));
            SetBrush(resources, "SecondaryTextBrush", Color.FromRgb(170, 170, 170));
            SetBrush(resources, "AccentBrush", Color.FromRgb(0, 122, 204));
            SetBrush(resources, "WindowBorderBrush", MixColor(windowBackground, Colors.White, DarkBorderMixRatio));
            SetBrush(resources, "TitleButtonForegroundBrush", Color.FromRgb(212, 212, 212));
            SetBrush(resources, "SoftButtonHoverBackgroundBrush", Color.FromRgb(51, 51, 51));
            SetBrush(resources, "SoftButtonPressedBackgroundBrush", Color.FromRgb(62, 62, 66));
            SetBrush(resources, "StatusErrorTextBrush", Color.FromRgb(241, 112, 123));
            return;
        }

        var windowBackgroundLight = Color.FromRgb(255, 255, 255);

        SetBrush(resources, "WindowBackgroundBrush", windowBackgroundLight);
        SetBrush(resources, "MainTextBrush", Color.FromRgb(31, 41, 55));
        SetBrush(resources, "SecondaryTextBrush", Color.FromRgb(107, 114, 128));
        SetBrush(resources, "AccentBrush", Color.FromRgb(0, 120, 215));
        SetBrush(resources, "WindowBorderBrush", MixColor(windowBackgroundLight, Colors.Black, LightBorderMixRatio));
        SetBrush(resources, "TitleButtonForegroundBrush", Color.FromRgb(75, 85, 99));
        SetBrush(resources, "SoftButtonHoverBackgroundBrush", Color.FromRgb(234, 243, 255));
        SetBrush(resources, "SoftButtonPressedBackgroundBrush", Color.FromRgb(215, 234, 254));
        SetBrush(resources, "StatusErrorTextBrush", Color.FromRgb(196, 43, 28));
    }

    private static void SetBrush(ResourceDictionary resources, string key, Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        resources[key] = brush;
    }

    private static Color MixColor(Color source, Color target, double ratio)
    {
        ratio = Math.Clamp(ratio, 0.0, 1.0);

        return Color.FromRgb(
            MixChannel(source.R, target.R, ratio),
            MixChannel(source.G, target.G, ratio),
            MixChannel(source.B, target.B, ratio));
    }

    private static byte MixChannel(byte source, byte target, double ratio)
    {
        return (byte)Math.Round(source + (target - source) * ratio);
    }
}
