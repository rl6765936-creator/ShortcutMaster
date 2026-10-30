using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;
using ShortcutMaster.Services;

namespace ShortcutMaster.Shell;

/// <summary>
/// Connects the alias files to each terminal once: PowerShell and Bash/Zsh profiles, and the CMD AutoRun setting.
/// </summary>
public static class ShellInstaller
{
    private const string StartMarker = "# >>> ShortcutMaster >>>";
    private const string EndMarker = "# <<< ShortcutMaster <<<";

    private const string CmdRegistryKey = @"Software\Microsoft\Command Processor";
    private const string CmdAutoRunValue = "AutoRun";
    private const string CmdAutoRunCommand =
        "if exist \"%APPDATA%\\ShortcutMaster\\aliases.doskey\" doskey /macrofile=\"%APPDATA%\\ShortcutMaster\\aliases.doskey\"";

    /// <summary>
    /// Adds the loading line to every supported shell and returns a list of what was changed.
    /// </summary>
    public static List<string> Install()
    {
        var changed = new List<string>();

        foreach (string profile in GetPowerShellProfiles(onlyExistingShells: true))
        {
            WriteBlock(profile, GetPowerShellLoadLine(), useBomForNewFile: true);
            changed.Add($"PowerShell  {profile}");
        }

        foreach (string rcFile in GetBashRcFiles(onlyExistingShells: true))
        {
            WriteBlock(rcFile, GetBashLoadLine(), useBomForNewFile: false);
            changed.Add($"Bash/Zsh    {rcFile}");
        }

        if (OperatingSystem.IsWindows())
        {
            InstallCmd();
            changed.Add($@"CMD         HKEY_CURRENT_USER\{CmdRegistryKey} -> {CmdAutoRunValue}");
        }

        return changed;
    }

    /// <summary>
    /// Removes the loading line from every shell and returns a list of what was changed.
    /// </summary>
    public static List<string> Uninstall()
    {
        var changed = new List<string>();
        IEnumerable<string> files = GetPowerShellProfiles(onlyExistingShells: false)
            .Concat(GetBashRcFiles(onlyExistingShells: false));

        foreach (string file in files)
        {
            if (RemoveBlockFromFile(file))
                changed.Add(file);
        }

        if (OperatingSystem.IsWindows() && UninstallCmd())
            changed.Add($@"HKEY_CURRENT_USER\{CmdRegistryKey} -> {CmdAutoRunValue}");

        return changed;
    }

    /// <summary>
    /// Returns true if at least one shell is already set up to load the aliases.
    /// </summary>
    public static bool IsInstalled()
    {
        IEnumerable<string> files = GetPowerShellProfiles(onlyExistingShells: false)
            .Concat(GetBashRcFiles(onlyExistingShells: false));

        if (files.Any(file => File.Exists(file) && File.ReadAllText(file).Contains(StartMarker)))
            return true;

        return OperatingSystem.IsWindows() && IsCmdInstalled();
    }

    /// <summary>
    /// Returns the PowerShell profile files: Windows PowerShell and PowerShell 7 on Windows, pwsh elsewhere.
    /// </summary>
    private static List<string> GetPowerShellProfiles(bool onlyExistingShells)
    {
        if (OperatingSystem.IsWindows())
        {
            string documents = AppPaths.GetFolder(Environment.SpecialFolder.MyDocuments);
            return new List<string>
            {
                Path.Combine(documents, "WindowsPowerShell", "profile.ps1"),
                Path.Combine(documents, "PowerShell", "profile.ps1")
            };
        }

        string configFolder = AppPaths.GetFolder(Environment.SpecialFolder.ApplicationData);
        string profile = Path.Combine(configFolder, "powershell", "profile.ps1");

        if (onlyExistingShells && !CommandLocator.Exists("pwsh"))
            return new List<string>();

        return new List<string> { profile };
    }

    /// <summary>
    /// Returns the Bash and Zsh startup files: .bashrc always (also used by Git Bash), .zshrc where Zsh exists.
    /// </summary>
    private static List<string> GetBashRcFiles(bool onlyExistingShells)
    {
        string home = AppPaths.GetFolder(Environment.SpecialFolder.UserProfile);
        string bashrc = Path.Combine(home, ".bashrc");
        string zshrc = Path.Combine(home, ".zshrc");
        var files = new List<string> { bashrc };

        if (OperatingSystem.IsWindows())
            return files;

        if (!onlyExistingShells || CommandLocator.Exists("zsh") || File.Exists(zshrc))
            files.Add(zshrc);

        return files;
    }

    /// <summary>
    /// Builds the line that PowerShell runs at startup to load aliases.ps1.
    /// </summary>
    private static string GetPowerShellLoadLine()
    {
        string path = OperatingSystem.IsWindows()
            ? "\"$env:APPDATA\\ShortcutMaster\\aliases.ps1\""
            : "'" + AppPaths.PowerShellScript.Replace("'", "''") + "'";

        return $"if (Test-Path {path}) {{ . {path} }}";
    }

    /// <summary>
    /// Builds the line that Bash/Zsh run at startup to load aliases.sh.
    /// </summary>
    private static string GetBashLoadLine()
    {
        string path = "'" + AliasScriptWriter.ToBashPath(AppPaths.BashScript).Replace("'", "'\\''") + "'";
        return $"[ -f {path} ] && . {path}";
    }

    /// <summary>
    /// Adds (or replaces) the ShortcutMaster block at the end of a startup file, keeping its encoding.
    /// </summary>
    private static void WriteBlock(string filePath, string line, bool useBomForNewFile)
    {
        (string text, Encoding encoding) = ReadText(filePath, useBomForNewFile);
        string newLine = text.Contains("\r\n") ? "\r\n" : "\n";
        string kept = RemoveBlock(text).TrimEnd('\r', '\n');
        string block = StartMarker + newLine + line + newLine + EndMarker + newLine;
        string result = kept.Length == 0 ? block : kept + newLine + newLine + block;

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, result, encoding);
    }

    /// <summary>
    /// Removes the ShortcutMaster block from a startup file. Returns true if the file was changed.
    /// </summary>
    private static bool RemoveBlockFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        (string text, Encoding encoding) = ReadText(filePath, useBomForNewFile: false);

        if (!text.Contains(StartMarker))
            return false;

        File.WriteAllText(filePath, RemoveBlock(text), encoding);
        return true;
    }

    /// <summary>
    /// Returns the text without the ShortcutMaster block (from the start marker to the end marker).
    /// </summary>
    private static string RemoveBlock(string text)
    {
        int start = text.IndexOf(StartMarker, StringComparison.Ordinal);

        if (start < 0)
            return text;

        int end = text.IndexOf(EndMarker, start, StringComparison.Ordinal);

        if (end < 0)
            return text;

        end += EndMarker.Length;

        while (end < text.Length && (text[end] == '\r' || text[end] == '\n'))
            end++;

        return text.Remove(start, end - start);
    }

    /// <summary>
    /// Reads a text file and detects its encoding (UTF-16, UTF-8 with or without BOM).
    /// A missing file is returned as empty text with the requested default encoding.
    /// </summary>
    private static (string Text, Encoding Encoding) ReadText(string filePath, bool useBomForNewFile)
    {
        if (!File.Exists(filePath))
            return (string.Empty, new UTF8Encoding(useBomForNewFile));

        byte[] bytes = File.ReadAllBytes(filePath);

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return (Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), Encoding.Unicode);

        bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        int skip = hasBom ? 3 : 0;

        return (Encoding.UTF8.GetString(bytes, skip, bytes.Length - skip), new UTF8Encoding(hasBom));
    }

    /// <summary>
    /// Adds the doskey loading command to CMD's AutoRun setting, keeping anything already there.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void InstallCmd()
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(CmdRegistryKey);
        string current = RemoveCmdCommand(ReadAutoRun(key));
        string updated = current.Length == 0 ? CmdAutoRunCommand : current + " & " + CmdAutoRunCommand;

        key.SetValue(CmdAutoRunValue, updated, RegistryValueKind.ExpandString);
    }

    /// <summary>
    /// Removes the doskey loading command from CMD's AutoRun setting. Returns true if it was there.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static bool UninstallCmd()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(CmdRegistryKey, writable: true);

        if (key == null)
            return false;

        string current = ReadAutoRun(key);

        if (!current.Contains(CmdAutoRunCommand))
            return false;

        string remaining = RemoveCmdCommand(current);

        if (remaining.Length == 0)
            key.DeleteValue(CmdAutoRunValue, throwOnMissingValue: false);
        else
            key.SetValue(CmdAutoRunValue, remaining, RegistryValueKind.ExpandString);

        return true;
    }

    /// <summary>
    /// Returns true if CMD's AutoRun setting already loads the aliases.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static bool IsCmdInstalled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(CmdRegistryKey);
        return key != null && ReadAutoRun(key).Contains(CmdAutoRunCommand);
    }

    /// <summary>
    /// Reads the AutoRun value as written, without expanding %VARIABLES%.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static string ReadAutoRun(RegistryKey key)
    {
        object? value = key.GetValue(CmdAutoRunValue, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
        return value as string ?? string.Empty;
    }

    /// <summary>
    /// Returns the AutoRun text without our command and the "&amp;" that joined it to other commands.
    /// </summary>
    private static string RemoveCmdCommand(string autoRun)
    {
        return autoRun
            .Replace(" & " + CmdAutoRunCommand, string.Empty)
            .Replace(CmdAutoRunCommand + " & ", string.Empty)
            .Replace(CmdAutoRunCommand, string.Empty)
            .Trim();
    }
}
