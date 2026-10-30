namespace ShortcutMaster.Services;

/// <summary>
/// Central place for all file paths the tool uses inside the user's personal data folder.
/// </summary>
public static class AppPaths
{
    public static string DataFolder => Path.Combine(GetFolder(Environment.SpecialFolder.ApplicationData), "ShortcutMaster");

    public static string ShortcutsFile => Path.Combine(DataFolder, "shortcuts.json");

    public static string PowerShellScript => Path.Combine(DataFolder, "aliases.ps1");

    public static string BashScript => Path.Combine(DataFolder, "aliases.sh");

    public static string CmdMacroFile => Path.Combine(DataFolder, "aliases.doskey");

    /// <summary>
    /// Returns a system folder path even if that folder does not exist yet (for example ~/.config on a new Linux user).
    /// </summary>
    public static string GetFolder(Environment.SpecialFolder folder)
    {
        return Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
    }
}
