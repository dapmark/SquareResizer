using System;
using System.IO;

namespace ImageSquareResizer;

internal sealed class AppSettings
{
    public const int DefaultQuality = 92;
    public const string DefaultTheme = "light";
    public const string DefaultResizeMode = "auto";
    public const bool DefaultSmartMode = true;
    public const bool DefaultManualMode = false;
    public const string DefaultSharpMode = "standard";

    private const string SettingsFileName = "settings.txt";

    public int Quality { get; set; } = DefaultQuality;
    public string Theme { get; set; } = DefaultTheme;
    public string ResizeMode { get; set; } = DefaultResizeMode;
    public bool SmartMode { get; set; } = DefaultSmartMode;
    public bool ManualMode { get; set; } = DefaultManualMode;
    public string SharpMode { get; set; } = DefaultSharpMode;

    public bool IsDarkTheme =>
        string.Equals(Theme, "dark", StringComparison.OrdinalIgnoreCase);

    public static string SettingsFilePath =>
        Path.Combine(AppContext.BaseDirectory, SettingsFileName);

    public static AppSettings Load()
    {
        var settings = new AppSettings();

        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                settings.Save();
                return settings;
            }

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
            return settings;
        }
    }

    public void Save()
    {
        Quality = NormalizeQuality(Quality);
        Theme = NormalizeTheme(Theme);
        ResizeMode = NormalizeResizeMode(ResizeMode);
        SharpMode = NormalizeSharpMode(SharpMode);

        string content =
            "# SquareResizer settings" + Environment.NewLine +
            "#" + Environment.NewLine +
            "# quality:" + Environment.NewLine +
            "#   число от 1 до 100" + Environment.NewLine +
            "#   применяется для JPG" + Environment.NewLine +
            "#" + Environment.NewLine +
            "# resize_mode:" + Environment.NewLine +
            "#   auto        = ближайший квадрат" + Environment.NewLine +
            "#   music_cover = стандартные размеры музыкальных обложек" + Environment.NewLine +
            "#" + Environment.NewLine +
            "# sharp_mode:" + Environment.NewLine +
            "#   standard  = стандартная резкость" + Environment.NewLine +
            "#   increased = повышенная резкость" + Environment.NewLine +
            "#   high      = высокая резкость" + Environment.NewLine +
            "#   maximum   = максимальная резкость" + Environment.NewLine +
            "#" + Environment.NewLine +
            "# smart_mode:" + Environment.NewLine +
            "#   true  = если возможно, дополнить фон; иначе сжать/растянуть до квадрата" + Environment.NewLine +
            "#   false = обычное сжатие/растяжение изображения до квадрата" + Environment.NewLine +
            "#" + Environment.NewLine +
            "# manual_mode:" + Environment.NewLine +
            "#   true  = ручное кадрирование перед сохранением" + Environment.NewLine +
            "#   false = обычная автоматическая обработка" + Environment.NewLine +
            "#" + Environment.NewLine +
            "# theme:" + Environment.NewLine +
            "#   light = светлая тема" + Environment.NewLine +
            "#   dark  = тёмная тема" + Environment.NewLine +
            "#" + Environment.NewLine +
            "# Если значение resize_mode некорректное или отсутствует, используется auto." + Environment.NewLine +
            "# Если значение sharp_mode некорректное или отсутствует, используется standard." + Environment.NewLine +
            "# Если значение smart_mode некорректное или отсутствует, используется true." + Environment.NewLine +
            "# Если значение manual_mode некорректное или отсутствует, используется false." + Environment.NewLine +
            "# Если значение theme некорректное или отсутствует, используется light." + Environment.NewLine +
            Environment.NewLine +
            "quality=" + Quality + Environment.NewLine +
            "resize_mode=" + ResizeMode + Environment.NewLine +
            "sharp_mode=" + SharpMode + Environment.NewLine +
            "smart_mode=" + SmartMode.ToString().ToLowerInvariant() + Environment.NewLine +
            "manual_mode=" + ManualMode.ToString().ToLowerInvariant() + Environment.NewLine +
            "theme=" + Theme + Environment.NewLine;

        File.WriteAllText(SettingsFilePath, content);
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