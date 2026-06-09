using System.Reflection;

namespace ImageSquareResizer;

internal static class AppVersion
{
    public const string ProductName = "SquareResizer";

    private const string UnknownVersion = "unknown";

    public static string Current { get; } = ReadAssemblyVersion();

    public static string WindowTitle => $"{ProductName} {Current}";

    private static string ReadAssemblyVersion()
    {
        string? informationalVersion = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            int metadataSeparatorIndex = informationalVersion.IndexOf('+');

            if (metadataSeparatorIndex > 0)
            {
                return informationalVersion[..metadataSeparatorIndex];
            }

            return informationalVersion;
        }

        string? assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

        if (!string.IsNullOrWhiteSpace(assemblyVersion))
        {
            return assemblyVersion;
        }

        return UnknownVersion;
    }
}
