using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ImageSquareResizer;

internal enum WindowsIntegrationStatus
{
    Absent,
    Installed,
    NeedsRepair,
}

internal readonly record struct WindowsIntegrationSnapshot(
    WindowsIntegrationStatus ContextMenu,
    WindowsIntegrationStatus ManualContextMenu,
    WindowsIntegrationStatus SendToShortcut);

internal readonly record struct WindowsIntegrationSelection(
    bool ContextMenu,
    bool ManualContextMenu,
    bool SendToShortcut);

internal static class WindowsIntegrationService
{
    private const string ExecutableName = "SquareResizer.exe";
    private const string ContextMenuCommandName = "SquareResizer";
    private const string ManualContextMenuCommandName = "SquareResizerManual";
    private const string ShortcutName = "SquareResizer.lnk";
    private const uint ShcneAssocChanged = 0x08000000;
    private const uint ShcnfIdList = 0x0000;

    private static readonly string[] SupportedExtensions =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp",
        ".tif",
        ".tiff",
    };

    internal static bool IsManagementAvailable
    {
        get
        {
#if DEBUG
            return false;
#else
            return true;
#endif
        }
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(
        uint eventId,
        uint flags,
        IntPtr item1,
        IntPtr item2);

    internal static WindowsIntegrationSnapshot GetState()
    {
        EnsureManagementAvailable();
        string executablePath = GetExecutablePath();
        return GetState(executablePath);
    }

    internal static WindowsIntegrationSnapshot Apply(WindowsIntegrationSelection selection)
    {
        EnsureManagementAvailable();
        string executablePath = GetExecutablePath();
        WindowsIntegrationSnapshot current = GetState(executablePath);

        try
        {
            ApplyContextMenuSelection(
                selection.ContextMenu,
                current.ContextMenu,
                executablePath,
                ContextMenuCommandName,
                manualMode: false);
            ApplyContextMenuSelection(
                selection.ManualContextMenu,
                current.ManualContextMenu,
                executablePath,
                ManualContextMenuCommandName,
                manualMode: true);
            ApplySendToSelection(selection.SendToShortcut, current.SendToShortcut, executablePath);
        }
        finally
        {
            SHChangeNotify(ShcneAssocChanged, ShcnfIdList, IntPtr.Zero, IntPtr.Zero);
        }

        WindowsIntegrationSnapshot updated = GetState(executablePath);

        if (!MatchesSelection(updated, selection))
        {
            throw new InvalidOperationException("The requested Windows integration state could not be verified.");
        }

        return updated;
    }

    private static WindowsIntegrationSnapshot GetState(string executablePath)
    {
        return new WindowsIntegrationSnapshot(
            GetContextMenuStatus(
                executablePath,
                ContextMenuCommandName,
                manualMode: false),
            GetContextMenuStatus(
                executablePath,
                ManualContextMenuCommandName,
                manualMode: true),
            GetSendToShortcutStatus(executablePath));
    }

    private static WindowsIntegrationStatus GetContextMenuStatus(
        string executablePath,
        string commandName,
        bool manualMode)
    {
        bool foundAny = false;
        bool allCorrect = true;
        string expectedTitle = GetContextMenuTitle(manualMode);
        string expectedCommand = GetCommandLine(executablePath, manualMode);

        foreach (string extension in SupportedExtensions)
        {
            string basePath = GetContextMenuBasePath(extension, commandName);

            using RegistryKey? baseKey = Registry.CurrentUser.OpenSubKey(basePath, writable: false);

            if (baseKey is null)
            {
                allCorrect = false;
                continue;
            }

            foundAny = true;

            string? title = baseKey.GetValue("MUIVerb") as string;
            string? iconPath = baseKey.GetValue("Icon") as string;

            using RegistryKey? commandKey = baseKey.OpenSubKey("command", writable: false);
            string? commandLine = commandKey?.GetValue(null) as string;

            if (!string.Equals(title, expectedTitle, StringComparison.Ordinal) ||
                !PathsEqual(iconPath, executablePath) ||
                !string.Equals(commandLine?.Trim(), expectedCommand, StringComparison.OrdinalIgnoreCase))
            {
                allCorrect = false;
            }
        }

        if (!foundAny)
        {
            return WindowsIntegrationStatus.Absent;
        }

        return allCorrect
            ? WindowsIntegrationStatus.Installed
            : WindowsIntegrationStatus.NeedsRepair;
    }

    private static WindowsIntegrationStatus GetSendToShortcutStatus(string executablePath)
    {
        string shortcutPath = GetSendToShortcutPath();

        if (!File.Exists(shortcutPath))
        {
            return WindowsIntegrationStatus.Absent;
        }

        object? shell = null;
        object? shortcut = null;

        try
        {
            shell = CreateWScriptShell();
            shortcut = ((dynamic)shell).CreateShortcut(shortcutPath);
            dynamic link = shortcut;

            string targetPath = Convert.ToString(link.TargetPath, CultureInfo.InvariantCulture) ?? string.Empty;
            string arguments = Convert.ToString(link.Arguments, CultureInfo.InvariantCulture) ?? string.Empty;
            string workingDirectory = Convert.ToString(link.WorkingDirectory, CultureInfo.InvariantCulture) ?? string.Empty;
            string iconLocation = Convert.ToString(link.IconLocation, CultureInfo.InvariantCulture) ?? string.Empty;
            string description = Convert.ToString(link.Description, CultureInfo.InvariantCulture) ?? string.Empty;
            string expectedWorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty;

            bool isCorrect =
                PathsEqual(targetPath, executablePath) &&
                string.IsNullOrWhiteSpace(arguments) &&
                PathsEqual(workingDirectory, expectedWorkingDirectory) &&
                PathsEqual(GetIconPath(iconLocation), executablePath) &&
                string.Equals(description, GetShortcutDescription(), StringComparison.Ordinal);

            return isCorrect
                ? WindowsIntegrationStatus.Installed
                : WindowsIntegrationStatus.NeedsRepair;
        }
        catch
        {
            return WindowsIntegrationStatus.NeedsRepair;
        }
        finally
        {
            ReleaseComObject(shortcut);
            ReleaseComObject(shell);
        }
    }

    private static void ApplyContextMenuSelection(
        bool shouldBeInstalled,
        WindowsIntegrationStatus currentStatus,
        string executablePath,
        string commandName,
        bool manualMode)
    {
        if (shouldBeInstalled)
        {
            if (currentStatus != WindowsIntegrationStatus.Installed)
            {
                InstallContextMenu(executablePath, commandName, manualMode);
            }

            return;
        }

        if (currentStatus != WindowsIntegrationStatus.Absent)
        {
            RemoveContextMenu(commandName);
        }
    }

    private static void InstallContextMenu(
        string executablePath,
        string commandName,
        bool manualMode)
    {
        string title = GetContextMenuTitle(manualMode);
        string commandLine = GetCommandLine(executablePath, manualMode);

        foreach (string extension in SupportedExtensions)
        {
            string basePath = GetContextMenuBasePath(extension, commandName);

            using RegistryKey baseKey = Registry.CurrentUser.CreateSubKey(basePath, writable: true)
                ?? throw new InvalidOperationException($"Registry key could not be created: {basePath}");
            baseKey.SetValue("MUIVerb", title, RegistryValueKind.String);
            baseKey.SetValue("Icon", executablePath, RegistryValueKind.String);

            using RegistryKey commandKey = baseKey.CreateSubKey("command", writable: true)
                ?? throw new InvalidOperationException($"Registry key could not be created: {basePath}\\command");
            commandKey.SetValue(null, commandLine, RegistryValueKind.String);
        }
    }

    private static void RemoveContextMenu(string commandName)
    {
        foreach (string extension in SupportedExtensions)
        {
            string basePath = GetContextMenuBasePath(extension, commandName);
            Registry.CurrentUser.DeleteSubKeyTree(basePath, throwOnMissingSubKey: false);
        }
    }

    private static void ApplySendToSelection(
        bool shouldBeInstalled,
        WindowsIntegrationStatus currentStatus,
        string executablePath)
    {
        if (shouldBeInstalled)
        {
            if (currentStatus != WindowsIntegrationStatus.Installed)
            {
                CreateSendToShortcut(executablePath);
            }

            return;
        }

        if (currentStatus != WindowsIntegrationStatus.Absent)
        {
            File.Delete(GetSendToShortcutPath());
        }
    }

    private static void CreateSendToShortcut(string executablePath)
    {
        string shortcutPath = GetSendToShortcutPath();
        string sendToFolder = Path.GetDirectoryName(shortcutPath)
            ?? throw new InvalidOperationException("The Windows SendTo folder is unavailable.");
        string workingDirectory = Path.GetDirectoryName(executablePath)
            ?? throw new InvalidOperationException("The SquareResizer folder is unavailable.");

        Directory.CreateDirectory(sendToFolder);

        object? shell = null;
        object? shortcut = null;

        try
        {
            shell = CreateWScriptShell();
            shortcut = ((dynamic)shell).CreateShortcut(shortcutPath);
            dynamic link = shortcut;
            link.TargetPath = executablePath;
            link.Arguments = string.Empty;
            link.WorkingDirectory = workingDirectory;
            link.IconLocation = $"{executablePath},0";
            link.Description = GetShortcutDescription();
            link.Save();
        }
        finally
        {
            ReleaseComObject(shortcut);
            ReleaseComObject(shell);
        }
    }

    private static object CreateWScriptShell()
    {
        Type shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is unavailable.");

        return Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Windows Script Host could not be started.");
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            _ = Marshal.FinalReleaseComObject(value);
        }
    }

    private static bool MatchesSelection(
        WindowsIntegrationSnapshot snapshot,
        WindowsIntegrationSelection selection)
    {
        return MatchesSelection(snapshot.ContextMenu, selection.ContextMenu) &&
               MatchesSelection(snapshot.ManualContextMenu, selection.ManualContextMenu) &&
               MatchesSelection(snapshot.SendToShortcut, selection.SendToShortcut);
    }

    private static bool MatchesSelection(WindowsIntegrationStatus status, bool shouldBeInstalled)
    {
        return shouldBeInstalled
            ? status == WindowsIntegrationStatus.Installed
            : status == WindowsIntegrationStatus.Absent;
    }

    private static string GetExecutablePath()
    {
        string? executablePath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(executablePath) ||
            !string.Equals(Path.GetFileName(executablePath), ExecutableName, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(executablePath))
        {
            throw new InvalidOperationException("The current SquareResizer executable could not be located.");
        }

        return Path.GetFullPath(executablePath);
    }

    private static string GetContextMenuBasePath(string extension, string commandName)
    {
        return $"Software\\Classes\\SystemFileAssociations\\{extension}\\shell\\{commandName}";
    }

    private static string GetCommandLine(string executablePath, bool manualMode)
    {
        return manualMode
            ? $"\"{executablePath}\" --manual \"%1\""
            : $"\"{executablePath}\" \"%1\"";
    }

    private static string GetContextMenuTitle(bool manualMode)
    {
        if (IsRussianWindowsUi())
        {
            return manualMode
                ? "Открыть в ручном режиме SquareResizer"
                : "Преобразовать с SquareResizer";
        }

        return manualMode
            ? "Open in SquareResizer manual mode"
            : "Convert with SquareResizer";
    }

    private static string GetShortcutDescription()
    {
        return IsRussianWindowsUi()
            ? "Открыть изображение с помощью SquareResizer"
            : "Open image with SquareResizer";
    }

    private static bool IsRussianWindowsUi()
    {
        return string.Equals(
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
            "ru",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSendToShortcutPath()
    {
        string sendToFolder = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);

        if (string.IsNullOrWhiteSpace(sendToFolder))
        {
            throw new InvalidOperationException("The Windows SendTo folder could not be located.");
        }

        return Path.Combine(sendToFolder, ShortcutName);
    }

    private static string GetIconPath(string iconLocation)
    {
        string value = iconLocation.Trim();
        int separatorIndex = value.LastIndexOf(',');

        if (separatorIndex > 0 &&
            int.TryParse(value[(separatorIndex + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            value = value[..separatorIndex];
        }

        return value.Trim().Trim('"');
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            string normalizedLeft = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(left.Trim().Trim('"')));
            string normalizedRight = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(right.Trim().Trim('"')));

            return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void EnsureManagementAvailable()
    {
        if (!IsManagementAvailable)
        {
            throw new InvalidOperationException("Windows integration management is disabled in debug builds.");
        }
    }
}
