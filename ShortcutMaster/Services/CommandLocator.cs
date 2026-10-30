namespace ShortcutMaster.Services;

/// <summary>
/// Checks whether a name is already taken by a program on the PATH or by a built-in shell command.
/// </summary>
public static class CommandLocator
{
    private static readonly string[] ShellBuiltIns =
    {
        "alias", "call", "cd", "clear", "cls", "copy", "del", "dir", "echo", "exit", "export",
        "history", "md", "mkdir", "move", "popd", "pushd", "pwd", "rd", "ren", "set", "source", "start", "type"
    };

    /// <summary>
    /// Returns true if typing this name in a terminal already runs something.
    /// </summary>
    public static bool Exists(string name)
    {
        if (ShellBuiltIns.Contains(name, StringComparer.OrdinalIgnoreCase))
            return true;

        foreach (string folder in GetPathFolders())
        {
            foreach (string extension in GetExecutableExtensions())
            {
                if (File.Exists(Path.Combine(folder, name + extension)))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the folders listed in the PATH environment variable.
    /// </summary>
    private static IEnumerable<string> GetPathFolders()
    {
        string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        return path
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(folder => folder.Trim('"'));
    }

    /// <summary>
    /// Returns the file extensions of runnable files: .exe, .cmd and so on on Windows, none elsewhere.
    /// </summary>
    private static string[] GetExecutableExtensions()
    {
        if (!OperatingSystem.IsWindows())
            return new[] { string.Empty };

        string pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD";
        return pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
