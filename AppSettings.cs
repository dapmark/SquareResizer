using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ImageSquareResizer;

internal sealed class AppSettings
{
    public const int DefaultQuality = 95;
    public const string DefaultTheme = "dark";
    public const string DefaultResizeMode = "music_cover";
    public const bool DefaultSmartMode = true;
    public const bool DefaultManualMode = false;
    public const string DefaultSharpMode = "standard";
    public const int DefaultJpegMode = 1;
    public const string DefaultLanguage = "en";
    public const double DefaultSmartPaddingPercent = 4.0;
    public const int DefaultSmartPaddingMaxPx = 32;
    public const int DefaultAutoSizeStep = 100;

    private const string SettingsFileName = "settings.txt";
    private const string EmbeddedDefaultSettingsResourceName = "SquareResizer.SettingsDefault";

    private static readonly string[] SettingKeyOrder =
    {
        "quality",
        "resize_mode",
        "sharp_mode",
        "jpeg_mode",
        "smart_mode",
        "manual_mode",
        "smart_padding_percent",
        "smart_padding_max_px",
        "auto_size_step",
        "theme",
        "language"
    };

    public int Quality { get; set; } = DefaultQuality;
    public string Theme { get; set; } = DefaultTheme;
    public string ResizeMode { get; set; } = DefaultResizeMode;
    public bool SmartMode { get; set; } = DefaultSmartMode;
    public bool ManualMode { get; set; } = DefaultManualMode;
    public string SharpMode { get; set; } = DefaultSharpMode;
    public int JpegMode { get; set; } = DefaultJpegMode;
    public string Language { get; set; } = DefaultLanguage;
    public double SmartPaddingPercent { get; set; } = DefaultSmartPaddingPercent;
    public int SmartPaddingMaxPx { get; set; } = DefaultSmartPaddingMaxPx;
    public int AutoSizeStep { get; set; } = DefaultAutoSizeStep;

    public bool IsDarkTheme =>
        string.Equals(Theme, "dark", StringComparison.OrdinalIgnoreCase);

    public static string SettingsFilePath =>
        Path.Combine(AppContext.BaseDirectory, SettingsFileName);

    public AppSettings Clone()
    {
        return new AppSettings
        {
            Quality = Quality,
            Theme = Theme,
            ResizeMode = ResizeMode,
            SmartMode = SmartMode,
            ManualMode = ManualMode,
            SharpMode = SharpMode,
            JpegMode = JpegMode,
            Language = Language,
            SmartPaddingPercent = SmartPaddingPercent,
            SmartPaddingMaxPx = SmartPaddingMaxPx,
            AutoSizeStep = AutoSizeStep
        };
    }

    public void CopyFrom(AppSettings other)
    {
        Quality = other.Quality;
        Theme = other.Theme;
        ResizeMode = other.ResizeMode;
        SmartMode = other.SmartMode;
        ManualMode = other.ManualMode;
        SharpMode = other.SharpMode;
        JpegMode = other.JpegMode;
        Language = other.Language;
        SmartPaddingPercent = other.SmartPaddingPercent;
        SmartPaddingMaxPx = other.SmartPaddingMaxPx;
        AutoSizeStep = other.AutoSizeStep;
    }

    public static AppSettings Load()
    {
        var settings = new AppSettings();

        try
        {
            EnsureSettingsFileExists();

            string[] lines = File.ReadAllLines(SettingsFilePath);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                int separatorIndex = line.IndexOf('=');

                if (separatorIndex <= 0)
                {
                    continue;
                }

                string key = line[..separatorIndex].Trim();
                string value = line[(separatorIndex + 1)..].Trim();

                if (key.Equals("quality", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int quality))
                    {
                        settings.Quality = NormalizeQuality(quality);
                    }

                    continue;
                }

                if (key.Equals("theme", StringComparison.OrdinalIgnoreCase))
                {
                    settings.Theme = NormalizeTheme(value);
                    continue;
                }

                if (key.Equals("language", StringComparison.OrdinalIgnoreCase))
                {
                    settings.Language = NormalizeLanguage(value);
                    continue;
                }

                if (key.Equals("resize_mode", StringComparison.OrdinalIgnoreCase))
                {
                    settings.ResizeMode = NormalizeResizeMode(value);
                    continue;
                }

                if (key.Equals("smart_mode", StringComparison.OrdinalIgnoreCase))
                {
                    settings.SmartMode = NormalizeSmartMode(value);
                    continue;
                }

                if (key.Equals("manual_mode", StringComparison.OrdinalIgnoreCase))
                {
                    settings.ManualMode = NormalizeManualMode(value);
                    continue;
                }

                if (key.Equals("sharp_mode", StringComparison.OrdinalIgnoreCase))
                {
                    settings.SharpMode = NormalizeSharpMode(value);
                    continue;
                }

                if (key.Equals("jpeg_mode", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int jpegMode))
                    {
                        settings.JpegMode = NormalizeJpegMode(jpegMode);
                    }

                    continue;
                }

                if (key.Equals("smart_padding_percent", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseDouble(value, out double smartPaddingPercent))
                    {
                        settings.SmartPaddingPercent = NormalizeSmartPaddingPercent(smartPaddingPercent);
                    }

                    continue;
                }

                if (key.Equals("smart_padding_max_px", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int smartPaddingMaxPx))
                    {
                        settings.SmartPaddingMaxPx = NormalizeSmartPaddingMaxPx(smartPaddingMaxPx);
                    }

                    continue;
                }

                if (key.Equals("auto_size_step", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int autoSizeStep))
                    {
                        settings.AutoSizeStep = NormalizeAutoSizeStep(autoSizeStep);
                    }
                }
            }

            settings.Save();
            return settings;
        }
        catch
        {
            settings.Quality = DefaultQuality;
            settings.Theme = DefaultTheme;
            settings.ResizeMode = DefaultResizeMode;
            settings.SmartMode = DefaultSmartMode;
            settings.ManualMode = DefaultManualMode;
            settings.SharpMode = DefaultSharpMode;
            settings.JpegMode = DefaultJpegMode;
            settings.Language = DefaultLanguage;
            settings.SmartPaddingPercent = DefaultSmartPaddingPercent;
            settings.SmartPaddingMaxPx = DefaultSmartPaddingMaxPx;
            settings.AutoSizeStep = DefaultAutoSizeStep;
            return settings;
        }
    }

    public void Save()
    {
        Quality = NormalizeQuality(Quality);
        Theme = NormalizeTheme(Theme);
        ResizeMode = NormalizeResizeMode(ResizeMode);
        SharpMode = NormalizeSharpMode(SharpMode);
        JpegMode = NormalizeJpegMode(JpegMode);
        Language = NormalizeLanguage(Language);
        SmartPaddingPercent = NormalizeSmartPaddingPercent(SmartPaddingPercent);
        SmartPaddingMaxPx = NormalizeSmartPaddingMaxPx(SmartPaddingMaxPx);
        AutoSizeStep = NormalizeAutoSizeStep(AutoSizeStep);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["quality"] = Quality.ToString(CultureInfo.InvariantCulture),
            ["resize_mode"] = ResizeMode,
            ["sharp_mode"] = SharpMode,
            ["jpeg_mode"] = JpegMode.ToString(CultureInfo.InvariantCulture),
            ["smart_mode"] = SmartMode.ToString().ToLowerInvariant(),
            ["manual_mode"] = ManualMode.ToString().ToLowerInvariant(),
            ["smart_padding_percent"] = FormatDouble(SmartPaddingPercent),
            ["smart_padding_max_px"] = SmartPaddingMaxPx.ToString(CultureInfo.InvariantCulture),
            ["auto_size_step"] = AutoSizeStep.ToString(CultureInfo.InvariantCulture),
            ["theme"] = Theme,
            ["language"] = Language
        };

        string[] sourceLines = LoadSettingsTemplateLines();
        var resultLines = new List<string>();
        var writtenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in sourceLines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                resultLines.Add(rawLine);
                continue;
            }

            int separatorIndex = rawLine.IndexOf('=');

            if (separatorIndex <= 0)
            {
                resultLines.Add(rawLine);
                continue;
            }

            string key = rawLine[..separatorIndex].Trim();

            if (values.TryGetValue(key, out string? value))
            {
                resultLines.Add(key + "=" + value);
                writtenKeys.Add(key);
                continue;
            }

            resultLines.Add(rawLine);
        }

        bool appendedAnyMissingKey = false;

        foreach (string key in SettingKeyOrder)
        {
            if (writtenKeys.Contains(key))
            {
                continue;
            }

            if (!appendedAnyMissingKey)
            {
                if (resultLines.Count > 0 && !string.IsNullOrWhiteSpace(resultLines[^1]))
                {
                    resultLines.Add(string.Empty);
                }

                appendedAnyMissingKey = true;
            }

            resultLines.Add(key + "=" + values[key]);
        }

        File.WriteAllLines(SettingsFilePath, resultLines, Encoding.UTF8);
    }

    private static void EnsureSettingsFileExists()
    {
        if (File.Exists(SettingsFilePath))
        {
            return;
        }

        string defaultSettingsContent = LoadEmbeddedDefaultSettingsContent() ?? BuildFallbackSettingsContent();
        File.WriteAllText(SettingsFilePath, defaultSettingsContent, Encoding.UTF8);
    }

    private static string[] LoadSettingsTemplateLines()
    {
        if (File.Exists(SettingsFilePath))
        {
            return File.ReadAllLines(SettingsFilePath);
        }

        string templateContent = LoadEmbeddedDefaultSettingsContent() ?? BuildFallbackSettingsContent();

        return templateContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');
    }

    private static string? LoadEmbeddedDefaultSettingsContent()
    {
        using Stream? stream = typeof(AppSettings).Assembly.GetManifestResourceStream(EmbeddedDefaultSettingsResourceName);

        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string BuildFallbackSettingsContent()
    {
        return
            "# SquareResizer settings" + Environment.NewLine +
            Environment.NewLine +
            "quality=" + DefaultQuality + Environment.NewLine +
            "resize_mode=" + DefaultResizeMode + Environment.NewLine +
            "sharp_mode=" + DefaultSharpMode + Environment.NewLine +
            "jpeg_mode=" + DefaultJpegMode + Environment.NewLine +
            "smart_mode=" + DefaultSmartMode.ToString().ToLowerInvariant() + Environment.NewLine +
            "manual_mode=" + DefaultManualMode.ToString().ToLowerInvariant() + Environment.NewLine +
            "smart_padding_percent=" + FormatDouble(DefaultSmartPaddingPercent) + Environment.NewLine +
            "smart_padding_max_px=" + DefaultSmartPaddingMaxPx + Environment.NewLine +
            "auto_size_step=" + DefaultAutoSizeStep + Environment.NewLine +
            "theme=" + DefaultTheme + Environment.NewLine +
            "language=" + DefaultLanguage + Environment.NewLine;
    }

    public static int NormalizeQuality(int quality)
    {
        return Math.Clamp(quality, 1, 100);
    }

    public static int NormalizeJpegMode(int jpegMode)
    {
        return Math.Clamp(jpegMode, 1, 3);
    }

    public static string NormalizeLanguage(string? language)
    {
        if (string.Equals(language, "ru", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(language, "russian", StringComparison.OrdinalIgnoreCase))
        {
            return "ru";
        }

        if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(language, "english", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        return DefaultLanguage;
    }

    public static string NormalizeTheme(string? theme)
    {
        if (string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase))
        {
            return "light";
        }

        if (string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase))
        {
            return "dark";
        }

        return DefaultTheme;
    }

    public static string NormalizeResizeMode(string? resizeMode)
    {
        if (string.Equals(resizeMode, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return "auto";
        }

        if (string.Equals(resizeMode, "music_cover", StringComparison.OrdinalIgnoreCase))
        {
            return "music_cover";
        }

        return DefaultResizeMode;
    }

    public static bool NormalizeSmartMode(string? smartMode)
    {
        if (string.Equals(smartMode, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(smartMode, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(smartMode, "on", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(smartMode, "yes", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(smartMode, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(smartMode, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(smartMode, "off", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(smartMode, "no", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return DefaultSmartMode;
    }

    public static bool NormalizeManualMode(string? manualMode)
    {
        if (string.Equals(manualMode, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(manualMode, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(manualMode, "on", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(manualMode, "yes", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(manualMode, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(manualMode, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(manualMode, "off", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(manualMode, "no", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return DefaultManualMode;
    }

    public static string NormalizeSharpMode(string? sharpMode)
    {
        if (string.Equals(sharpMode, "increased", StringComparison.OrdinalIgnoreCase))
        {
            return "increased";
        }

        if (string.Equals(sharpMode, "high", StringComparison.OrdinalIgnoreCase))
        {
            return "high";
        }

        if (string.Equals(sharpMode, "maximum", StringComparison.OrdinalIgnoreCase))
        {
            return "maximum";
        }

        return DefaultSharpMode;
    }

    public static double NormalizeSmartPaddingPercent(double smartPaddingPercent)
    {
        if (double.IsNaN(smartPaddingPercent) || double.IsInfinity(smartPaddingPercent))
        {
            return DefaultSmartPaddingPercent;
        }

        return Math.Clamp(smartPaddingPercent, 0.0, 20.0);
    }

    public static int NormalizeSmartPaddingMaxPx(int smartPaddingMaxPx)
    {
        return Math.Clamp(smartPaddingMaxPx, 0, 300);
    }


    public static int NormalizeAutoSizeStep(int autoSizeStep)
    {
        int[] allowedSteps = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 60, 70, 80, 90, 100, 200 };

        return allowedSteps.Contains(autoSizeStep)
            ? autoSizeStep
            : DefaultAutoSizeStep;
    }

    public static bool TryParseDouble(string value, out double result)
    {
        value = value.Trim().Replace(',', '.');
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    public static string FormatDouble(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
