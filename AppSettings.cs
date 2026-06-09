using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ImageSquareResizer;

internal sealed class AppSettings
{
    public const int DefaultQuality = 92;
    public const string DefaultTheme = "light";
    public const string DefaultResizeMode = "auto";
    public const bool DefaultSmartMode = true;
    public const bool DefaultManualMode = false;
    public const string DefaultSharpMode = "standard";
    public const int DefaultJpegMode = 1;

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
        "theme"
    };

    public int Quality { get; set; } = DefaultQuality;
    public string Theme { get; set; } = DefaultTheme;
    public string ResizeMode { get; set; } = DefaultResizeMode;
    public bool SmartMode { get; set; } = DefaultSmartMode;
    public bool ManualMode { get; set; } = DefaultManualMode;
    public string SharpMode { get; set; } = DefaultSharpMode;
    public int JpegMode { get; set; } = DefaultJpegMode;

    public bool IsDarkTheme =>
        string.Equals(Theme, "dark", StringComparison.OrdinalIgnoreCase);

    public static string SettingsFilePath =>
        Path.Combine(AppContext.BaseDirectory, SettingsFileName);


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
                    if (int.TryParse(value, out int quality))
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
                    if (int.TryParse(value, out int jpegMode))
                    {
                        settings.JpegMode = NormalizeJpegMode(jpegMode);
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

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["quality"] = Quality.ToString(),
            ["resize_mode"] = ResizeMode,
            ["sharp_mode"] = SharpMode,
            ["jpeg_mode"] = JpegMode.ToString(),
            ["smart_mode"] = SmartMode.ToString().ToLowerInvariant(),
            ["manual_mode"] = ManualMode.ToString().ToLowerInvariant(),
            ["theme"] = Theme
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

        File.WriteAllLines(SettingsFilePath, resultLines);
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
            "theme=" + DefaultTheme + Environment.NewLine;
    }

    public static int NormalizeQuality(int quality)
    {
        if (quality < 1)
        {
            return 1;
        }

        if (quality > 100)
        {
            return 100;
        }

        return quality;
    }

    public static int NormalizeJpegMode(int jpegMode)
    {
        if (jpegMode < 1)
        {
            return 1;
        }

        if (jpegMode > 3)
        {
            return 3;
        }

        return jpegMode;
    }

    public static string NormalizeTheme(string? theme)
    {
        if (string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase))
        {
            return "dark";
        }

        return DefaultTheme;
    }

    public static string NormalizeResizeMode(string? resizeMode)
    {
        if (string.Equals(resizeMode, "music_cover", StringComparison.OrdinalIgnoreCase))
        {
            return "music_cover";
        }

        return DefaultResizeMode;
    }

    public static bool NormalizeSmartMode(string? smartMode)
    {
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
}
