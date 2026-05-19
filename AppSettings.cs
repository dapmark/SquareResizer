using System;
using System.IO;

namespace ImageSquareResizer;

internal sealed class AppSettings
{
    public const int DefaultQuality = 92;
    public const string DefaultTheme = "light";

    private const string SettingsFileName = "settings.txt";

    public int Quality { get; set; } = DefaultQuality;
    public string Theme { get; set; } = DefaultTheme;

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
                }
            }

            settings.Save();
            return settings;
        }
        catch
        {
            settings.Quality = DefaultQuality;
            settings.Theme = DefaultTheme;
            return settings;
        }
    }

    public void Save()
    {
        Quality = NormalizeQuality(Quality);
        Theme = NormalizeTheme(Theme);

        string content =
            "# DapLine SquareResizer settings" + Environment.NewLine +
            "#" + Environment.NewLine +
            "# quality:" + Environment.NewLine +
            "#   число от 1 до 100" + Environment.NewLine +
            "#   применяется для JPG и WEBP" + Environment.NewLine +
            "#" + Environment.NewLine +
            "# theme:" + Environment.NewLine +
            "#   light = светлая тема" + Environment.NewLine +
            "#   dark  = тёмная тема" + Environment.NewLine +
            "#" + Environment.NewLine +
            "# Если значение theme некорректное или отсутствует, используется light." + Environment.NewLine +
            Environment.NewLine +
            "quality=" + Quality + Environment.NewLine +
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
}
