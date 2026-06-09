using System;
using System.IO;
using System.Reflection;

namespace ImageSquareResizer;

internal static class AppVersion
{
    public const string ProductName = "SquareResizer";

    private const string VersionFileName = "version.txt";
    private const string UnknownVersion = "unknown";

    public static string Current { get; } = ReadVersion();

    public static string WindowTitle => $"{ProductName} {Current}";

    private static string ReadVersion()
    {
        try
        {
            string versionPath = Path.Combine(AppContext.BaseDirectory, VersionFileName);

            if (File.Exists(versionPath))
            {
                string version = File.ReadAllText(versionPath).Trim();

                if (!string.IsNullOrWhiteSpace(version))
                {
                    return version;
                }
            }
        }
        catch
        {
            // Если внешний version.txt недоступен, ниже используем метаданные сборки.
        }

        string? informationalVersion = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        return UnknownVersion;
    }
}
